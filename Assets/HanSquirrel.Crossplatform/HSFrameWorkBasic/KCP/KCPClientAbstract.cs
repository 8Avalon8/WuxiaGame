using System;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using HSFrameWork.KCP.Common;
using HSFrameWork.Common;
using GLib;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using HSFrameWork.Net;
using HSFrameWork.Common.Inner;

namespace HSFrameWork.KCP.Client
{
    public partial class KCPClientFactory
    {
        /// <summary>
        /// >=Net3.5
        /// </summary>
        public abstract class KCPClientAbstract : IPlayerLinkClient
        {
            public KCPClientAbstract(uint displayName, bool traceMe, int sendLogMaxSize, object state, RecvDataHandler recvData)
            {
                DebugInfoKCP = new PlayerLinkDebugInfoKCP();
                DebugInfo.CreatedTime = DateTime.UtcNow;

                DisplayName = displayName;
                State = state;
                RecvData += recvData;
                FakePackageCount = 0; //仅仅为了100%代码测试，下同。
                FakeSize = 0;

                _TraceMe = traceMe;
                _SendLogMaxSize = sendLogMaxSize;
            }

            protected readonly bool _TraceMe;
            protected readonly int _SendLogMaxSize;
            public uint DisplayName { get; protected set; }
            public string PK { get; private set; }
            public byte LocalSessionId { get; private set; }
            public ushort RemoteSessionId { get; private set; }
            public int TrashPackageCount { get; private set; }
            public int TrashSize { get; private set; }
            public int FakePackageCount { get; private set; }
            public int FakeSize { get; private set; }
            public PlayerLinkDebugInfo DebugInfo { get { return DebugInfoKCP; } }
            public PlayerLinkDebugInfoKCP DebugInfoKCP { get; private set; }

            public string VerboseStatus
            {
                get
                {
                    return "{0}, Trash={1}, WaitSnd=[{2}], LoopNo={3}".f(DebugInfo.VerboseStatus, TrashPackageCount, m_Kcp == null ? "NA" : m_Kcp.WaitSndEx().ToString(), _LoopNo);
                }
            }
            private int _RawReceivedCount;

            protected UdpClient m_UdpClient { get; set; }
            protected IPEndPoint mIPEndPoint;
            protected IPEndPoint mSvrEndPoint;
            protected KCPLibEx m_Kcp { get; set; }
            public object Secret { get { return m_Kcp; } }
            public object State { get; set; }

            public bool SuspendFlush
            {
                set
                {
                    DoSendToKCP_APPTHREAD(value ? KCPSigals.SuspendFlushSignal : KCPSigals.ResumeFlushSignal, false);
                }
            }

            public PlayerLinkStatus Status { get; protected set; }
            public IEnumerable<PlayerLinkStatus> StatusHistory { get { lock (_StatusHistory) return _StatusHistory.ToArray(); } }
            private List<PlayerLinkStatus> _StatusHistory = new List<PlayerLinkStatus>();
            public event Action<IPlayerLink, PlayerLinkStatus> Event;
            public event RecvDataHandler RecvData;
            public event Action<IPlayerLinkClient, byte[], int, int> RefusedEvent;


            /// <summary>
            /// 返回true说明已经处理了。false表示是普通的数据，该函数没有处理。
            /// 如果需要退出则抛出退出异常。
            /// </summary>
            protected void TryProcessSignal_MT(SmartBufferEx<KCPDataDirection> sb)
            {
                /*
                status == PlayerLinkStatus.InConnect || status == PlayerLinkStatus.Connected ||
                status == PlayerLinkStatus.ConnectRefused || status == PlayerLinkStatus.ConnectAbandoned ||
                status == PlayerLinkStatus.Closing1 || status == PlayerLinkStatus.Closing2
                 */
                if (sb == KCPSigals.SuspendFlushSignal)
                {
                    m_Kcp.SuspendFlush = true;
                }
                else if (sb == KCPSigals.ResumeFlushSignal)
                {
                    m_Kcp.SuspendFlush = false;
                }
                else if (sb == KCPSigals.PoliteCloseSignal)
                {
                    if (Status.IsClosable())
                    {
                        /*
                         * status == PlayerLinkStatus.InConnect || status == PlayerLinkStatus.Connected ||
                           status == PlayerLinkStatus.ConnectRefused || status == PlayerLinkStatus.ConnectAbandoned
                         */
                        _Logger0.Debug("KCPC{0} Do CloseGracefully", DisplayName);
                        InvokeEvent_MT(PlayerLinkStatus.Closing1, false);
                        SendClose_MT();
                    }
                    else
                    {
                        Mini.ThrowIfFalse(Status == PlayerLinkStatus.Closing2 || Status == PlayerLinkStatus.Closing1,
                            "George编程错误。TryProcessSignal_MT中Status状态错乱");
                    }
                }
                else if (sb == KCPSigals.ReceiveFailSignal)
                {
                    throw new ExitException(PlayerLinkStatus.ReceiveFail, "接收异常", null);
                    //GGTODO 接收异常存下来
                }
                else
                {
                    Mini.ThrowIfFalse(sb == KCPSigals.RuteCloseSignal, "George程序编写错误。TryProcessSignal");
                    _Logger0.Debug("KCPC{0} CloseForcefully", DisplayName);
                    throw new ExitException(PlayerLinkStatus.Disconnected, "被强制终止", null);
                }
            }

            void IPlayerLink.CloseGracefully()
            {
                _Logger0.Debug("KCPC{0} CloseGracefully called.", DisplayName);
                lock (_LockStartTag)
                {
                    if (!_Started)
                    {
                        _StoppedBeforeStarted = true;
                        return;
                    }
                }
                DoSendToKCP_APPTHREAD(KCPSigals.PoliteCloseSignal, false);
            }

            void IPlayerLink.CloseForcefully()
            {
                _Logger0.Debug("KCPC{0} CloseForcefully called.", DisplayName);
                lock (_LockStartTag)
                {
                    if (!_Started)
                    {
                        _StoppedBeforeStarted = true;
                        return;
                    }
                }
                DoSendToKCP_APPTHREAD(KCPSigals.RuteCloseSignal, false);
            }

            /// <summary>
            /// KCPLib的发送实现函数
            /// </summary>
            protected void KCPSendDataHandler_MT(byte[] buf, int size)
            {
                if (_Logger2.IsTraceEnabled && NeedTrace)
                    DataPackUtils.TraceKCPDownBuffer(">>", buf, 0, size, "UDPCF", null, "",
                        "C", DisplayName, "C", (format, args) => _Logger2.Trace(format, args));

                try
                {
                    if (KCPGlobalOptions.SimuUdpSendExcetionClient)
                    {
                        KCPGlobalOptions.SimuUdpSendExcetionClient = false;
                        throw new Exception("千树万树梨花开");
                    }
                    using (var sb = DataPackUtils.PackKCPDownData2UDPData(KCPLibEx.SharedBytePool, _Random, true,
                                                                    RemoteSessionId, LocalSessionId, buf, 0, size))
                    {
                        DebugInfoKCP.UdpHeader.Send.LastSize = DataPackUtils.SESSION_HEAD_SIZE;
                        DebugInfoKCP.RAWSendBandwidth.LastSize = sb.Size;
                        m_UdpClient.Send(sb.Data, sb.Size);
                    }
                }
                catch (Exception e)
                {
                    _Logger0.Error(e, "Udp发送异常");
                    throw new ExitException(PlayerLinkStatus.SendFail, "Udp发送异常", e);
                }
            }

#if HSFRAMEWORK_NET_ABOVE_4_5
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
            protected void CloseUDP()
            {
                var udpclient = m_UdpClient;
                m_UdpClient = null;
                if (udpclient != null)
                {
                    try
                    {
                        using (udpclient)
                            udpclient.Close();
                    }
                    catch { }
                }
            }

            /// <summary>
            /// 是否已经进入Inconnect状态。
            /// </summary>
            private volatile bool _Started = false;
            /// <summary>
            /// 没开始之前就已经停止了。
            /// </summary>
            private volatile bool _StoppedBeforeStarted = false;
            private object _LockStartTag = new object();

            protected double _Closing2Time = 0;
            protected void InvokeEvent_MT(PlayerLinkStatus newStat, bool closeUDP)
            {
                if (newStat == PlayerLinkStatus.Closing2)
                {
                    _Closing2Time = m_Kcp.IClock;
                }

                Status = newStat;
                lock (_StatusHistory)
                    _StatusHistory.Add(newStat);

                if (closeUDP)
                    CloseUDP();
                if (Event != null)
                {
                    try
                    {
                        Event.Invoke(this, newStat);
                    }
                    catch (Exception ex)
                    {
                        Event = null; //防止再次出现异常。
                        if (newStat.IsDead())
                        {
                            _Logger0.Error(ex, "调用Event [{0}] 回调出现异常。", newStat);
                            Status = PlayerLinkStatus.CallbackException;
                            lock (_StatusHistory)
                                _StatusHistory.Add(PlayerLinkStatus.CallbackException);
                            ExitMessage = "调用Event [{0}] 回调出现异常。原始退出消息：[{1}]".f(newStat, ExitMessage);
                            return;
                        }
                        else
                        {
                            _Logger0.Error(ex, "调用Event [{0}] 回调出现异常，终止连接。", newStat);
                            throw new ExitException(PlayerLinkStatus.CallbackException, "调用Event回调出现异常。", ex);
                        }
                    }

                    if (newStat.IsDownAlive() && Status.IsDead())
                    {
                        //表示在Event处理的时候已经重入并且出现异常，然后Link关闭了。
                        //具体事例比如：ClientSyncImpl里面： Connected里面Send，Send里面异常了。
                        throw new ExitException(PlayerLinkStatus.NULL, null);
                    }
                }
            }

            protected String ExitMessage { get; private set; }
            protected abstract String TypeDisplay { get; }
            protected abstract void OnKCPLibDirty();
            protected class ExitException : Exception
            {
                public PlayerLinkStatus Status;
                public ExitException(PlayerLinkStatus status, string message) : this(status, message, null) { }
                public ExitException(PlayerLinkStatus status, string message, Exception innerException)
                    : base(message, innerException)
                {
                    Status = status;
                }
            }

            /// <summary>
            /// 仅仅处理从InConnect和Connected到结束状态。多线程安全。自动防止结束重入。
            /// </summary>
#if HSFRAMEWORK_NET_ABOVE_4_5
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
            protected void InvokeExitEvent_MT(PlayerLinkStatus newStatus, string message)
            {
                if (Status.IsDownAlive() || Status == PlayerLinkStatus.NULL)
                {
                    ExitMessage = message;
                    InvokeEvent_MT(newStatus, true);
                    return;
                }

                _Logger0.Warn("InvokeExitEvent_MT重入 C{0} {1} {2} omitted.", DisplayName, newStatus, message);
            }

            protected byte[] _HandShakeData;
            protected void Setup(byte[] serverEndPoint, byte localSessionId, byte[] handShakeData, Func<byte[], int, int, bool> hv)
            {
                _Logger0.Debug("▼ {0} KCPC{1} 开始运行", TypeDisplay, DisplayName);
                lock (_LockStartTag)
                {
                    if (_StoppedBeforeStarted)
                    {
                        throw new ExitException(PlayerLinkStatus.StoppedBeforeStarted, "在没有启动连接前就被外界关闭了。");
                    }
                    else
                    {
                        _Started = true;
                    }
                }

                LocalSessionId = localSessionId;
                _HandShakeData = DataPackUtils.PackHandShakeData(handShakeData, DisplayName, _TraceMe, _SendLogMaxSize);
                HandShakeValidator = hv;

                var endPoint = SimpleServerEndPoint.Create(serverEndPoint, 0, serverEndPoint.Length);
                IPAddress svrIPAddress;
                if (!IPAddress.TryParse(endPoint.InternetIP, out svrIPAddress))
                {
                    _Logger0.Debug("▼ {0} KCPC{1} 解析地址 [{2}]", TypeDisplay, DisplayName, endPoint.InternetIP);

                    IPHostEntry hostEntry = Dns.GetHostEntry(endPoint.InternetIP);
                    if (hostEntry.AddressList.Length <= 0)
                    {
                        throw new Exception("无法解析DNS地址 {0}".f(endPoint.InternetIP));
                    }
                    svrIPAddress = hostEntry.AddressList[0];
                }
                _Logger0.Debug("▼ {0} KCPC{1} 远端地址为[{2}]", TypeDisplay, DisplayName, svrIPAddress);

                mIPEndPoint = mSvrEndPoint = new IPEndPoint(svrIPAddress, endPoint.InternetPort);
                m_UdpClient = new UdpClient(svrIPAddress.ToString(), endPoint.InternetPort);
                m_UdpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 5000);
                //Receive a time-out. This option applies only to synchronous methods; it has no effect on 
                //asynchronous methods such as the BeginSend method.
                KCPGlobalOptions.DisableUDP_CONN_RESET(m_UdpClient);
#if HSFRAMEWORK_NET_ABOVE_4_5
                try
                {
                    m_UdpClient.AllowNatTraversal(true); //GG net4以上才有？
                }
                catch
                {
                    //在UNity下会异常。底层不支持。
                }
#endif
                m_UdpClient.Connect(mSvrEndPoint);

                Status = PlayerLinkStatus.NULL;
                //init_kcp((UInt32)new Random((int)DateTime.Now.Ticks).Next(1, Int32.MaxValue));

                m_Kcp = new KCPLibEx(true, 0XBBBBBBBB, _SendLogMaxSize, KCPSendDataHandler_MT, OnKCPLibDirty);
                m_Kcp.DisplayName = DisplayName;
                m_Kcp.ServerSide = false;
                m_Kcp.GetLogger = () =>
                {
                    return _LoggerX.IsTraceEnabled && NeedTrace ?
                           _LoggerX : null;
                };

                // fast mode.
                m_Kcp.NoDelay(1, 0, 2, 1);
                m_Kcp.WndSize(128, 128);
            }

            private Random _Random = new Random();

            /// <summary>
            /// 直接或者简介操作m_kcp来发送数据。最终发送完成后需要销毁sb
            /// </summary>
            protected abstract bool DoSendToKCP_APPTHREAD(SmartBufferEx<KCPDataDirection> sb, bool backGround);

            /// <summary>
            /// 内部调用的KCP发送。
            /// </summary>
            protected abstract void DoSendToKCP_MT(SmartBufferEx<KCPDataDirection> sb, bool backGround);

            private int _AppDataSeq = 0;
            public bool Send(byte[] data, int offset, int size, bool backGround, bool zip = false)
            {
                if (data == null || offset < 0 || size + offset > data.Length)
                    throw new ArgumentOutOfRangeException("data/offset/size不正确");

                if (Status != PlayerLinkStatus.Connected)
                {
                    _Logger0.Warn("C{0}不在连接状态，不可以发送数据。[{1}]", DisplayName, Status);
                    return false;
                }

                var sb = KCPLibEx.SharedBytePool.ConvertAppData2KCPUpData(KCPUpDataTypeTag.APP, data, offset, size, zip, m_Kcp.ZipLog, (ushort)Interlocked.Increment(ref _AppDataSeq));
                LogSend(sb, "APPCF");
                return DoSendToKCP_APPTHREAD(sb, backGround);
            }

            private void LogSend(SmartBufferEx<KCPDataDirection> sb, string title)
            {
                var size = sb.Size - DataPackUtils.KCPUPDATA_PACK_ADDED_SIZE;
                if (Logger1Tracable())
                    _Logger1.Trace("{0} →→        C{1}[{2:D3}     appC{3}-{4}]", title, DisplayName, size,
                        sb.Data.ToHex(DataPackUtils.APP_DATA_SEQ_OFFSET, DataPackUtils.APP_DATA_SEQ_SIZE),
                        sb.Data.ToHex(DataPackUtils.APP_DATA_OFFSET, Math.Min(256, size)));
            }

            protected void SendHandShake1_MT()
            {
                var sb = KCPLibEx.SharedBytePool.ConvertAppData2KCPUpData(KCPUpDataTypeTag.HANDSHAKE1, _HandShakeData, 0, _HandShakeData.Length, false, null, (ushort)Interlocked.Increment(ref _AppDataSeq));
                LogSend(sb, "NK1CF");
                DoSendToKCP_MT(sb, false);
            }

            private void SendHandShake3_MT()
            {
                var sb = KCPLibEx.SharedBytePool.ConvertAppData2KCPUpData(KCPUpDataTypeTag.HANDSHAKE3, DataPackUtils.DummyBuff, 0, DataPackUtils.DummyBuff.Length, false, null, (ushort)Interlocked.Increment(ref _AppDataSeq));
                LogSend(sb, "NK3CF");
                DoSendToKCP_MT(sb, false);
            }

#if false
            protected static byte[] M_CloseCmd;
            static KCPClientAbstract()
            {
                using (var sb = KCPLibEx.SharedBytePool.ConvertAppData2KCPUpData(KCPUpDataTypeTag.CLOSE, DataPackUtils.DummyBuff, 0, DataPackUtils.DummyBuff.Length))
                    M_CloseCmd = sb.GetCommonBytes();
            }
#endif
            protected void SendClose_MT()
            {
                var sb = KCPLibEx.SharedBytePool.ConvertAppData2KCPUpData(KCPUpDataTypeTag.CLOSE, DataPackUtils.DummyBuff, 0, DataPackUtils.DummyBuff.Length, false, null, (ushort)Interlocked.Increment(ref _AppDataSeq));
                LogSend(sb, "CLOSE");
                DoSendToKCP_MT(sb, false);
            }

            private Func<byte[], int, int, bool> HandShakeValidator;

            /// <summary>
            /// 最后收到数据的时刻。
            /// 当超过UdpLibConfig.MaxTimeNoData时间没有收到客户端的数据，则可以认为是死链接
            /// </summary>
            protected volatile int _LoopNo = -1;
            protected TimeSpan m_LastRecvTimestamp;
            /// <summary>
            /// 仅仅在主任务里面调用，多线程不安全
            /// </summary>
            protected void ProcessReceivedUDP_MT(byte[] buf, bool simuFake = true)
            {
                _RawReceivedCount++;

                if (simuFake && KCPGlobalOptions.SimuUdpTrashPacketClient)
                {
                    //因为仅仅是在模拟测试的时候用，因此不去管分配效率的问题。
                    byte[] fakeBuf = new byte[buf.Length];
                    _Random.NextBytes(fakeBuf);
                    ProcessReceivedUDP_MT(fakeBuf, false);
                }

                UDPDataTypeTag udpDataType;
                byte localSessionID; ushort remoteSessionId;
                int offset, size;
                if (DataPackUtils.UnPackUDPData2KCPDownData(buf, out udpDataType, out remoteSessionId, out localSessionID, out offset, out size) &&
                    localSessionID == LocalSessionId && udpDataType == UDPDataTypeTag.KCP)
                { //解密
                    DebugInfoKCP.UdpHeader.Recv.LastSize = DataPackUtils.SESSION_HEAD_SIZE;
                    DebugInfoKCP.RAWRecvBandwidth.LastSize = buf.Length;
                    m_LastRecvTimestamp = SharedG.Clock.Elapsed;
                    ProcessKCPDownData_MT(buf, offset, size, remoteSessionId);
                }
                else
                {
                    TrashSize += buf.Length;
                    TrashPackageCount++;
                }
            }

            private volatile int _MaxReceivedAppDataSeq = 0;
            private void ProcessKCPDownData_MT(byte[] buf, int offset, int size, ushort remoteSessionId)
            {
                int appDataSeq = 0;
                if (_Logger2.IsTraceEnabled && NeedTrace)
                {
                    appDataSeq = DataPackUtils.TraceKCPDownBuffer("<<", buf, offset, size, "UDPCR", null, "",
                        "C", DisplayName, "S", (format, args) => _Logger2.Trace(format, args));

                    if (_MaxReceivedAppDataSeq < appDataSeq)
                        _MaxReceivedAppDataSeq = appDataSeq;
                }
                //这个函数的第三个参数并非有效数据的长度，而是有效数据从buf[0]开始的长度。
                m_Kcp.Input(buf, offset, size + offset);

                int kcpUpDataSize;
                while ((kcpUpDataSize = m_Kcp.PeekSize()) > 0)
                {
                    using (var sb = KCPLibEx.SharedBytePool.CreateSB(kcpUpDataSize))
                    {
                        var buffer = sb.Data;
                        if (m_Kcp.Recv(buffer) > 0)
                        {
                            ProcessKCPUpData_MT(buffer, 0, kcpUpDataSize, remoteSessionId);
                        }
                    }
                }
            }

            private void ProcessKCPUpData_MT(byte[] buffer, int kcpUpDataOffset, int kcpUpDataSize, ushort remoteSessionId)
            {
                KCPUpDataTypeTag kcpUpDataType;
                int appDataOffset;
                int appDataLength;

                if (KCPGlobalOptions.SimuKCPUpDataFormatErrorClient)
                {
                    KCPGlobalOptions.SimuKCPUpDataFormatErrorClient = false;
                    _Random.NextBytes(buffer);
                }

                if (!DataPackUtils.TryUnPackKCPUpData2AppData(buffer, kcpUpDataOffset, kcpUpDataSize, out kcpUpDataType, out appDataOffset, out appDataLength))
                {
                    throw new ExitException(PlayerLinkStatus.DataError, "KCP输出的数据格式不对，无法UnPackKCPUpDownData。", null);
                }

                if (KCPGlobalOptions.SimuKCPUpDataTypeErrorClient)
                {
                    KCPGlobalOptions.SimuKCPUpDataTypeErrorClient = false;
                    kcpUpDataType = KCPUpDataTypeTag.MAX;
                }

                DebugInfoKCP.AppData.Recv.LastSize = appDataLength;
                DebugInfoKCP.KCPUpHeader.Recv.LastSize = DataPackUtils.KCPUPDATA_PACK_ADDED_SIZE;

                if (kcpUpDataType == KCPUpDataTypeTag.HANDSHAKE2)
                {
                    LogAppReceive("NK2CR", buffer, appDataOffset, appDataLength, false);
                    ProcessHandShake2Data_MT(buffer, appDataOffset, appDataLength, remoteSessionId);
                }
                else if (kcpUpDataType == KCPUpDataTypeTag.CLOSE)
                {
                    LogAppReceive("CLOSE", buffer, appDataOffset, appDataLength, false);
                    if (Status.IsClosable())
                    {
                        InvokeEvent_MT(PlayerLinkStatus.Closing2, false);
                        SendClose_MT();
                    }
                    else if (Status == PlayerLinkStatus.Closing1)
                    {
                        InvokeEvent_MT(PlayerLinkStatus.Closing2, false);
                    }
                }
                else if (kcpUpDataType == KCPUpDataTypeTag.APP || kcpUpDataType == KCPUpDataTypeTag.APPZIPPED)
                {
                    LogAppReceive("APPCR", buffer, appDataOffset, appDataLength, true);

                    if (RecvData != null && Status == PlayerLinkStatus.Connected)
                    {
                        SmartBuffer sb = null;
                        try
                        {
                            if (kcpUpDataType == KCPUpDataTypeTag.APPZIPPED)
                            {
                                sb = DataPackUtils.UnzipAppData(KCPLibEx.SharedBytePool, buffer, appDataOffset, appDataLength);
                                if (sb == null)
                                    throw new ExitException(PlayerLinkStatus.DataError, "KCP输出的APPZIP数据解压错误", null);
                                buffer = sb.Data;
                                appDataOffset = 0;
                                appDataLength = sb.Size;
                            }
                            RecvData(this, buffer, appDataOffset, appDataLength);
                        }
                        catch (Exception ex)
                        {
                            _Logger0.Error(ex, "调用RecvData过程出现异常。RecvData实现者BUG。");
                            throw new ExitException(PlayerLinkStatus.CallbackException, "调用RecvData过程出现异常。RecvData实现者BUG。");
                        }
                        finally
                        {
                            if (sb != null) sb.Dispose();
                        }

                        if (Status.IsDead())
                        {   //RecvData处理过程中内部出现异常，已经退出了。
                            throw new ExitException(PlayerLinkStatus.NULL, null);
                        }
                    }
                }
                else
                {
                    throw new ExitException(PlayerLinkStatus.DataError, "KCP输出的数据格式不对：kcpUpDataType不对。");
                }
            }

            private void LogAppReceive(string title, byte[] buffer, int appDataOffset, int appDataLength, bool zipped)
            {
                if (Logger1Tracable())
                {
                    ushort length = zipped ? BinOp.DecodeUShort(buffer, appDataOffset) : (ushort)0;
                    _Logger1.Trace("{0} ←←        C{1}[{2:D3}     appS{3}-{4}]{5}", title, DisplayName, appDataLength,
                            buffer.ToHex(appDataOffset - DataPackUtils.APP_DATA_SEQ_SIZE, DataPackUtils.APP_DATA_SEQ_SIZE),
                            buffer.ToHex(appDataOffset, Math.Min(256, appDataLength)),
                            zipped ? " Zipped[{0:D3}]".f(length) : "");
                }
            }

            private void ProcessHandShake2Data_MT(byte[] buf, int offset, int size, ushort remoteSessionId)
            {
                RemoteSessionId = remoteSessionId;
                PK = RemoteSessionId.ToString();

                if (Status != PlayerLinkStatus.InConnect)
                {
                    //在SendHandShake1之后，在HandShake2没有返回之前，调用 GraceFullCLOSE 才会出现这种情况。
                    if (Logger0Tracable())
                        _Logger0.Trace("KCPC{0} HANDSHAKE RET, omitted. Status=[{1}]", DisplayName, Status);
                    return;
                }

                //GGTODO: 第一次的握手消息回来的总是很慢。需要调试看看为何。
                offset++;
                size--;
                //GG: 服务端会返回HS拒绝消息。
                if (buf[offset - 1] == 0)
                    ProcessHandShakeRejected_MT(buf, offset, size);
                else
                    ProcessHandShakeAccepted_MT(buf, offset, size);
            }

            private void ProcessHandShakeRejected_MT(byte[] buf, int offset, int size)
            {
                if (Logger0Tracable())
                    _Logger0.Trace("KCPC{0} HANDSHAKE Refused, PlayerLinkStatus.ConnectRefused.", DisplayName);
                InvokeEvent_MT(PlayerLinkStatus.ConnectRefused, false);

                if (RefusedEvent != null)
                {
                    try
                    {
                        RefusedEvent.Invoke(this, buf, offset, size);
                    }
                    catch (Exception ex)
                    {
                        _Logger0.Error(ex, "调用RefusedEvent出现异常，退出Link。");
                        throw new ExitException(PlayerLinkStatus.CallbackException, "调用RefusedEvent出现异常，退出Link。");
                    }
                }
                DoSendToKCP_MT(KCPSigals.PoliteCloseSignal, false); //处理完接收数据后就会马上处理发送数据。
            }

            private void ProcessHandShakeAccepted_MT(byte[] buf, int offset, int size)
            {
                bool acceptedLocal;
                try
                {
                    acceptedLocal = HandShakeValidator == null || HandShakeValidator(buf, offset, size);
                }
                catch (Exception ex)
                {
                    _Logger0.Error(ex, "调用 HandShakeValidator 出现异常。");
                    throw new ExitException(PlayerLinkStatus.CallbackException, "调用 HandShakeValidator 出现异常。", ex);
                }

                if (acceptedLocal)
                {
                    if (Logger0Tracable())
                        _Logger0.Debug("KCPC{0} HANDSHAKE RET OK, 连接成功.", DisplayName);
                    SendHandShake3_MT();
                    m_Kcp.FlushInterval = KCPLib.IKCP_INTERVAL_DEFAULT;
                    DebugInfo.ConnectedTime = DateTime.UtcNow;
                    InvokeEvent_MT(PlayerLinkStatus.Connected, false);
                }
                else
                {
                    if (Logger0Tracable())
                        _Logger0.Debug("KCPC{0} HANDSHAKE RET OK, But PlayerLinkStatus.ConnectAbandoned.", DisplayName);
                    InvokeEvent_MT(PlayerLinkStatus.ConnectAbandoned, false);
                    DoSendToKCP_MT(KCPSigals.PoliteCloseSignal, false);
                }
            }

            protected int CalcUpdateTimeout()
            {
                return Utils.CalcTimeOut(m_NextUpdateTime, m_Kcp.IClock, 0,
                                            (int)KCPGlobalOptions.MaxTimeNoData.TotalMilliseconds, Status);
            }

            protected UInt32 m_NextUpdateTime;
            /// <summary>
            /// 如果判断当前需要update则去updata，仅仅在主任务里面调用
            /// </summary>
            protected void SafeUpdata_MT(long wakeUpTime)
            {
                bool updated = m_Kcp.Update();
                var old = m_NextUpdateTime;
                m_NextUpdateTime = m_Kcp.Check();

                if (Logger3Tracable())
                    _Logger3.Trace("\t\t\t\t\tKCPC{0} LOOP{10} {8}: WaitSnd={1}/{9}, Sleep={6}, LastRTT={11}/{12}, Current={2} NextFlush={3} LastFlush={4} NextWakeup={5} SLoad={7}",
                        DisplayName, m_Kcp.WaitSnd(),  //0,1
                        m_Kcp.Current, m_Kcp.NextFlush == UInt32.MaxValue ? "∞" : m_Kcp.NextFlush.ToString(), //2,3,4,5
                        m_Kcp.LastFlush, m_NextUpdateTime, //4,5
                        m_NextUpdateTime == uint.MaxValue ? "∞" : (m_NextUpdateTime - m_Kcp.Current).ToString(),  //6
                        SharedG.Clock.Elapsed.Ticks - wakeUpTime,//7
                        updated ? "Flushed" : (old == m_NextUpdateTime ? "Skipped" : "Updated"),//8
                        m_Kcp.CmdToSend(),//9
                        _LoopNo, //10
                        m_Kcp.LastRtt, //11
                        m_Kcp.RX_RTO //12
                        );
            }

            protected static readonly IHSLogger _Logger0 = HSLogManager.GetLogger("KCPCL0"); //Main Control
            protected static readonly IHSLogger _Logger1 = HSLogManager.GetLogger("KCPCL1"); //APP DATA
            protected static readonly IHSLogger _Logger2 = HSLogManager.GetLogger("KCPCL2"); //KCP DATA
            protected static readonly IHSLogger _Logger3 = HSLogManager.GetLogger("KCPCL3"); //LOOP CONTROL
            protected static readonly IHSLogger _LoggerX = HSLogManager.GetLogger("KCPCLX"); //KCPLib

            private bool NeedTrace
            {
                get
                {
                    return _TraceMe || (NeedTraceFunc != null && NeedTraceFunc(this));
                }
            }

#if HSFRAMEWORK_NET_ABOVE_4_5
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
            protected bool Logger0Tracable()
            {
                return _Logger0.IsTraceEnabled && NeedTrace;
            }

#if HSFRAMEWORK_NET_ABOVE_4_5
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
            protected bool Logger1Tracable()
            {
                return _Logger1.IsTraceEnabled && NeedTrace;
            }

#if HSFRAMEWORK_NET_ABOVE_4_5
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
            protected bool Logger3Tracable()
            {
                return _Logger3.IsTraceEnabled && NeedTrace;
            }

        }
    }
}
