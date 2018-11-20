//GG 20181025 开始重构。如哪位大佬修改代码，请添加临时注释以便GG维护。

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using HSFrameWork.Common;
using GLib;
using System.Diagnostics;

namespace HSFrameWork.Scut.Inner
{
    /// <summary>
    /// GG维护 20181025
    /// </summary>
    public class SocketConnect
    {
        #region 公开接口
        public SocketConnect(string host, int port, IActionClientSettings settings)
        {
            _Host = host;
            _Port = port;
            _Settings = settings;
            if (_SocketOpts.SocketOptionsDesc.Visible())
                ActionClient.Logger.Warn("本机使用特殊的Socket连接属性：{0}", _SocketOpts.SocketOptionsDesc);
        }

        public string HostPort { get { return _Host + ":" + _Port; } }

        public void SayFinal()
        {
            string status = "";
            if (_SendList.Count > 0)
                status += "待发送 [{0}]".f(_SendList.Count);
            if (_EchoQueue.Count + _PushQueue.Count > 0)
                status += "待回调 [{0}]".f(_EchoQueue.Count + _PushQueue.Count);

            if (status == "")
                ActionClient.Logger.Info("SocketConnect [{0}] 被正常丢弃。", HostPort);
            else
                ActionClient.Logger.Warn("SocketConnect [{0}] 被强制丢弃。尚有：{1}。", HostPort, status);
        }

        /// <summary>
        /// 是否已经被关闭。被关闭后就无法再次被使用。
        /// </summary>
        public bool Closed { get; private set; }

        public bool IsConnected()
        {
            return _Socket != null;
        }

        public void Send(byte[] data, ActionExecInfo execInfo)
        {
            Mini.ThrowIfTrue(Closed, "GG程序编写错误：SocketConnect被关闭后了不能调用Send()。");
            Mini.ThrowNullIf(data, "上层程序编写错误：SocketConnect.Send(data不能为NULL)");

            int timeout = CalcTimeOut(execInfo.Action);
            if (ActionClient.LoggerUp.IsTraceEnabled)
                ActionClient.LoggerUp.Trace("MSG#{0} >> Action#{1} [{2}] {3} Bytes. [{4}]",
                    execInfo.Action.MsgId, execInfo.Action.ActionId, GetActionName(execInfo.Action.ActionId), data.Length,
                    GetSimpleTimeoutString(timeout));
            _SendList.Add(execInfo);

            Send(data, timeout);
        }

        /// <summary>
        /// 关闭连接，如果keepCurrentPacket，则当前正在发送的那些数据包都会以错误返回值被Dequeue取得。
        /// </summary>
        public void Close(bool keepCurrentPacket = false)
        {
            if (Closed)
                return;

            ActionClient.LoggerDown.Info("SocketConnect被上层主动关闭。发送缓冲区[{0}]，接收缓冲区[{1}]，推送缓冲区[{2}]。{3}",
                _SendList.Count, _EchoQueue.Count, _PushQueue.Count,
                Busy ? (keepCurrentPacket ? "保留现有缓冲区。" : "抛弃现有缓冲区。") : "");
            CloseInner(keepCurrentPacket, ErrorCode.UserCanceled, "上层主动关闭");
        }

        /// <summary>
        /// 取出回返消息包
        /// </summary>
        /// <returns></returns>
        public ActionExecInfo Dequeue()
        {
            if (_EchoQueue.Count == 0)
                return null;
            else
                return _EchoQueue.Dequeue();
        }

        public ActionExecInfo DequeuePush()
        {
            if (_PushQueue.Count == 0)
                return null;
            else
                return _PushQueue.Dequeue();
        }

        public void Update()
        {
            if (Closed)
                return;

            ProcessNetReaderQueue();
            //首先处理已经收到的数据。因为后面三个如果先处理的话中间遇到错误，则可能会把好包也作为错误回调。
            //所以要第一个调用。
            ProcessSocketClose();
            ProcessTimeOut();
            //如果前面关闭了Socket的话，队列清空也就没有超时了。所以要 第三调用
            ProcessHeartBeat();
            //如果前面三个出现错误关掉Socket的话，心跳就不需要发送了。所以要最后调用。
        }

        #endregion

        #region 连接和关闭
        private bool Busy
        {
            get
            {
                return _SendList.Count + _EchoQueue.Count + _PushQueue.Count > 0;
            }
        }

        /// <summary>
        /// 打开连接。
        /// </summary>
        private void OpenIf()
        {
            if (_Socket != null)
                return;

            if (!_SocketOpts.InternetReachable)
                throw new Exception("当前没有网络连接");

            ActionClient.LoggerUp.Info("Connecting [{0}:{1}]", _Host, _Port);
            IPAddress[] ipAddressArr = Dns.GetHostAddresses(_Host);
            IPEndPoint endPoint = new IPEndPoint(ipAddressArr[0], _Port);
            _Socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                _Socket.Connect(_Host, _Port);
            }
            catch (Exception e)
            {
                _Socket = null;
                throw new Exception("SocketConnect连接 [{0}:{1}] 失败。".f(_Host, _Port), e);
            }

            _HeartbeatSW.Reset();
            _HeartbeatSW.Start();

            new Thread(RT_ReceiveThreadMain).Start(_Socket);
        }

        private void CloseInner(bool keepCurrentPacket, ErrorCode errorCode, string errorString)
        {
            if (Closed)
                return;

            Closed = true;

            ActionClient.Logger.Info("Closing Socket {0}:{1}.", _Host, _Port);
            try
            {
                var socket = _Socket;
                _Socket = null; //因_Socket会在接收线程中存在。
                socket.Close();
            }
            catch { }

            _HeartbeatSW.Reset();
            _HeartbeatSW.Stop();

            if (keepCurrentPacket)
            {
                if (_SendList.Count > 0)
                {
                    ActionClient.Logger.Warn("现有的 [{0}] 个上行数据包被设置为错误 [{1}]。", _SendList.Count, errorCode);
                    foreach (ActionExecInfo info in _SendList)
                    {
                        info.ErrorCode = errorCode;
                        info.ErrorMsg = errorString;
                        _EchoQueue.Enqueue(info);
                    }
                }
            }
            else
            {
                if (_SendList.Count > 0)
                    ActionClient.Logger.Warn("现有的 [{0}] 个上行数据包被抛弃。", _SendList.Count);
                if (_EchoQueue.Count > 0)
                    ActionClient.Logger.Warn("现有的 [{0}] 个返回数据包被抛弃。", _EchoQueue.Count);
                if (_PushQueue.Count > 0)
                    ActionClient.Logger.Warn("现有的 [{0}] 个推送数据包被抛弃。", _PushQueue.Count);

                _EchoQueue.Clear();
                _PushQueue.Clear();
            }

            _SendList.Clear();
        }

        #endregion

        #region 发送
        /// <summary>
        /// 发送数据。如果发送失败，则CloseInner
        /// </summary>
        private void Send(byte[] data, int timeoutMS)
        {
            try
            {
                OpenIf();
            }
            catch (Exception ex)
            {
                ActionClient.LoggerUp.Error(ex, "数据发送异常");
                CloseInner(true, ErrorCode.ConnectError, "连接错误");
                return;
            }

            try
            {
                _Socket.SendTimeout = timeoutMS;
                if (_Socket.Send(data) != data.Length)
                    throw new Exception("Send()返回的长度不等于数据的长度");
            }
            catch (Exception ex)
            {
                ActionClient.LoggerUp.Error(ex, "数据发送错误");
                CloseInner(true, ErrorCode.SendError, "发送错误");
            }
        }
        #endregion

        #region 接收
        /// <summary>
        /// 接收数据的线程,GG 20181025整理
        /// </summary>
        private void RT_ReceiveThreadMain(object socket)
        {
            try
            {
                RT_ReceiveThreadInner(socket as Socket);
            }
            catch (Exception ex)
            {
                if (_Socket == socket)
                    ActionClient.Logger.Warn(ex, "接收线程出现异常，退出。");
                else
                    ActionClient.Logger.Info("接收线程正常退出。");
            }

            lock (_SocketTobeClosed)
                _SocketTobeClosed.Add(socket as Socket);
        }

        private void RT_ReceiveThreadInner(Socket socket)
        {
            while (true)
            {
                if (_Socket != socket)
                {
                    ActionClient.Logger.Info("RT_ReceiveThreadInner：当前接收线程正常退出。");
                    return; //当前接收线程已经失效了。
                }

                RT_ReadNextPacket();
            }
        }

        /// <summary>
        /// GG 20181025 读下一个数据包，仅仅在接收线程中执行。会抛出各种异常。
        /// </summary>
        private void RT_ReadNextPacket()
        {
            byte[] data = RT_ReadFull(BitConverter.ToInt32(RT_ReadFull(4), 0));
            int netLen = data.Length;

            if (data[0] == 'j' && data[1] == 'm')
            {
                data = TDES.ScutInstance.Decrypt(data, 2);
            }

            //判断流是否有Gzip压缩
            if (data[0] == 0x1f && data[1] == 0x8b && data[2] == 0x08 && data[3] == 0x00)
            {
                data = NetReader.Decompression(data);
            }

            NetReader reader = new NetReader(data);

            lock (_NetReaderQueue)
                _NetReaderQueue.Enqueue(reader);

            TraceOrErrorReaderDebugInfo(reader, netLen + 4, data.Length + 4);
        }

        private void TraceOrErrorReaderDebugInfo(NetReader reader, int netLen, int dataLen)
        {
            if (reader.StatusCode == 0 && !ActionClient.LoggerDown.IsTraceEnabled)
                return;

            string msg = reader.StatusCode == 0 ?
                "Success" : "Status [{0}] Desc [{1}]".f(reader.StatusCode, reader.Description);
            msg = "MSG#{0} << Action#{1} [{2}], Len[{3}], Net[{4}], {5}".f(
                        reader.ClientMsgId, reader.ActionId, GetActionName(reader.ActionId),
                        dataLen, netLen, msg);
            if (reader.StatusCode == 0)
                ActionClient.LoggerDown.Trace(msg);
            else
                ActionClient.LoggerDown.Error(msg);
        }
        /// <summary>
        /// GG 20181025 读完整的数据包，仅仅在接收线程中执行。
        /// </summary>
        private byte[] RT_ReadFull(int datalen)
        {
            byte[] data = new byte[datalen];
            int startIndex = 0;
            int recnum = 0;
            do
            {
                int rev = _Socket.Receive(data, startIndex, datalen - recnum, SocketFlags.None);
                if (rev == 0)
                    throw new Exception("对方已经关闭Socket");
                recnum += rev;
                startIndex += rev;
            } while (recnum != datalen);
            return data;
        }

        #endregion

        #region 刷新
        /// <summary>
        /// 如果出错，则会CloseInner
        /// </summary>
        private void ProcessNetReaderQueue()
        {
            List<NetReader> readers = null;
            lock (_NetReaderQueue)
            {
                if (_NetReaderQueue.Count == 0)
                    return;

                readers = new List<NetReader>(_NetReaderQueue);
                _NetReaderQueue.Clear();
            }

            foreach (var reader in readers)
            {
                if (!MovePacketFromSendListToQueue(reader))
                    break;
                //只要一个包解析出现错误，就会抛弃其他所有接受的。否则逻辑写起来比较复杂容易出错。
            }
        }

        /// <summary>
        /// GG 20181025 找到对应的发送数据。如果处理出错，则返回false。
        /// </summary>
        private bool MovePacketFromSendListToQueue(NetReader reader)
        {
            //find pack in send queue.
            foreach (ActionExecInfo execInfo in _SendList)
            {
                if (execInfo.Action.MsgId == reader.ClientMsgId)
                {
                    if (execInfo.Action.ActionId != reader.ActionId)
                    {
                        ActionClient.LoggerDown.Fatal("GG服务端编程错误。Msg#{0} execInfo.Action.ActionId[#{1}] != reader.ActionId[#{2}]，关闭当前连接。",
                            reader.ClientMsgId, execInfo.Action.ActionId, reader.ActionId);
                        CloseInner(true, ErrorCode.Client_FoundServerLogicError, "客户端内部错误：ActionID和MsgId不匹配");
                        return false;
                    }
                    else
                    {
                        execInfo.Reader = reader;
                        execInfo.ErrorCode = (ErrorCode)reader.StatusCode;
                        execInfo.ErrorMsg = reader.Description;
                        _SendList.Remove(execInfo);
                        _EchoQueue.Enqueue(execInfo);
                        return true;
                    }
                }
            }

            return AddToServerPushQueue(reader);
        }

        /// <summary>
        /// GG 20181025 没有对应的发送数据包。说明是服务端主动推送的。如果处理出错，则返回false。
        /// </summary>
        private bool AddToServerPushQueue(NetReader reader)
        {
            if (reader.ActionId == 1) return true; //心跳包

            try
            {
                ActionExecInfo execInfo = new ActionExecInfo()
                {
                    Action = ActionFactory.Create(reader.ActionId, null, null, _Settings),
                    ErrorCode = (ErrorCode)reader.StatusCode,
                    ErrorMsg = reader.Description,
                    Reader = reader
                };
                _PushQueue.Enqueue(execInfo);
                return true;
            }
            catch (Exception ex)
            {
                ActionClient.LoggerDown.Error(ex, "无法创建服务端发出的Action#{0}", reader.ActionId);
                CloseInner(true, ErrorCode.ClientServerMismatch, "服务端发出数据客户端不支持。");
                return false;
            }
        }

        /// <summary>
        /// 会调用CloseInner
        /// </summary>
        private void ProcessSocketClose()
        {
            lock (_SocketTobeClosed)
            {
                if (_SocketTobeClosed.Count > 0)
                {
                    foreach (var s in _SocketTobeClosed)
                    {
                        if (s == _Socket)
                            CloseInner(true, ErrorCode.RecvError, "接收出错");
                        else
                            Mini.EatException(() => s.Close());
                    }
                    _SocketTobeClosed.Clear();
                }
            }
        }

        /// <summary>
        /// 如果出错，会CloseInner
        /// </summary>
        private void ProcessHeartBeat()
        {
            if (_SocketOpts.NoHeartBeat || _Socket == null)
                return;

            if (_HeartbeatSW.ElapsedMilliseconds >= _Settings.HeartBeatIntervalMS)
            {
                int msgId;
                var data = NetWriter.BuildHearbeatPackage(out msgId);
                int timeout = CalcTimeout(_Settings.HeartBeatTimeoutMS);
                ActionClient.LoggerUp.Trace("MSG #{0} >> Action [心跳包], {1}Bytes. [{2}]", msgId, data.Length, GetSimpleTimeoutString(timeout));
                Send(data, timeout);
                _HeartbeatSW.Reset();
                _HeartbeatSW.Start();
            }
        }

        /// <summary>
        /// 只要有一个超时，就整体超时
        /// </summary>
        private void ProcessTimeOut()
        {
            foreach (ActionExecInfo execInfo in _SendList)
            {
                if (DateTime.UtcNow.Subtract(execInfo.SendTimeUTC).TotalMilliseconds >= CalcTimeOut(execInfo.Action))
                {
                    CloseInner(true, ErrorCode.TimedoutError, "接收超时");
                    return;
                }
            }
        }
        #endregion

        #region 仅仅在主线程访问
        private readonly string _Host;
        private readonly int _Port;
        private readonly IRecvHeadDecoder _Formater;
        private readonly List<ActionExecInfo> _SendList = new List<ActionExecInfo>();
        private readonly Queue<ActionExecInfo> _EchoQueue = new Queue<ActionExecInfo>();
        private readonly Queue<ActionExecInfo> _PushQueue = new Queue<ActionExecInfo>();

        private string GetSimpleTimeoutString(int timeoutMS)
        {
            if (timeoutMS < 1000)
                return timeoutMS.ToString() + "ms";
            if (timeoutMS < 60 * 1000)
                return (timeoutMS / 1000).ToString() + "S";
            if (timeoutMS < 3600 * 1000)
                return (timeoutMS / 1000 / 60).ToString() + "M";
            return "∞";
        }

        private int CalcTimeout(int orgTimeoutMS)
        {
            return _SocketOpts.NoSocketTimeOut ? 24 * 3600 * 1000 : orgTimeoutMS;
        }

        private int CalcTimeOut(GameAction action)
        {
            if (_SocketOpts.NoSocketTimeOut)
                return 24 * 3600 * 1000;
            else if (action.TimeoutMS == GameAction.TIMEOUTMS_NOT_SET)
                return _Settings.DefaultSocketTimeoutMS;
            else
                return action.TimeoutMS;
        }

        private readonly IActionClientSettings _Settings;
        private readonly Stopwatch _HeartbeatSW = new Stopwatch();
        #endregion

        #region 接收线程和主线程同时会使用的私有变量
        private Socket _Socket;
        private List<Socket> _SocketTobeClosed = new List<Socket>();
        private readonly Queue<NetReader> _NetReaderQueue = new Queue<NetReader>();

        private static Dictionary<int, string> _ActionNameDict = Container.TryResolveNamed<Dictionary<int, string>>("ActionNameDict");

        private static string GetActionName(int actionId)
        {
            string name;
            if (_ActionNameDict != null && _ActionNameDict.TryGetValue(actionId, out name))
            {
                return name;
            }
            else
            {
                return "";
            }
        }
        private static IScutSocketOptions _SocketOpts = Container.Resolve<IScutSocketOptions>();
        #endregion
    }
}
