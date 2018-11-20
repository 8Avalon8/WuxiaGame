
using GLib;
using HSFrameWork.Common;
using HSFrameWork.Common.Inner;
using ProtoBuf;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
#if HSFRAMEWORK_NET_ABOVE_4_5
using System.Runtime.CompilerServices;
#endif
using System.Text;
using System.Threading;

namespace HSFrameWork.Net
{

    public interface IPackageCounter
    {
        /// <summary>
        /// 垃圾数据包个数
        /// </summary>
        int TrashPackageCount { get; }

        /// <summary>
        /// 垃圾数据长度
        /// </summary>
        int TrashSize { get; }

        /// <summary>
        /// 数据包的来源不明
        /// </summary>
        int FakePackageCount { get; }
        int FakeSize { get; }
    }

    public static class PlayerLinkStatusExt
    {
        /// <summary>
        /// InConnect Connected ConnectAbandoned 是否会接收和发送UDP数据。
        /// </summary>
#if HSFRAMEWORK_NET_ABOVE_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool IsClosable(this PlayerLinkStatus status)
        {
            return status == PlayerLinkStatus.InConnect || status == PlayerLinkStatus.Connected ||
                status == PlayerLinkStatus.ConnectRefused || status == PlayerLinkStatus.ConnectAbandoned;
        }

        /// <summary>
        /// 是否会接收和发送UDP数据。
        /// </summary>
#if HSFRAMEWORK_NET_ABOVE_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool IsDownAlive(this PlayerLinkStatus status)
        {
            return status == PlayerLinkStatus.InConnect || status == PlayerLinkStatus.Connected ||
                status == PlayerLinkStatus.ConnectRefused || status == PlayerLinkStatus.ConnectAbandoned ||
                status == PlayerLinkStatus.Closing1 || status == PlayerLinkStatus.Closing2;
        }

        /// <summary>
        /// 是否会让应用接收和发送数据。
        /// </summary>
#if HSFRAMEWORK_NET_ABOVE_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool IsUpAlive(this PlayerLinkStatus status)
        {
            return status == PlayerLinkStatus.Connected;
        }

#if HSFRAMEWORK_NET_ABOVE_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool IsDead(this PlayerLinkStatus status)
        {
            return status != PlayerLinkStatus.NULL && !IsDownAlive(status);
        }
    }

    public enum PlayerLinkStatus
    {
        NULL,
        StoppedBeforeStarted,
        InConnect, //C,S
        ConnectRefused, //C 服务器直接拒绝
        ConnectAbandoned, //C 服务器返回的握手回应参数客户端不认可。
        Connected, //C,S
        SendFail, //C,S
        ReceiveFail, //C,S
        DataError, //c,s
        TimedOut,//C,S
        InnerException,//C,S
        CallbackException,//C,S 给上层的回调出现异常。
        Closing1,//C,S
        Closing2,//C,S
        Disconnected, //C,S
    }

    public class BandWidthUsage
    {
        public int CountAll { get; private set; }
        public int SizeAll { get; private set; }
        public DateTime FirstTime { get; private set; }

        private Queue<Tuple<DateTime, int>> _SizeQueue = new Queue<Tuple<DateTime, int>>(128);
        public int CountLastSecond { get { return _SizeQueue.Count; } }
        public int SizeLastSecond { get { return _SizeLastSecond; } }
        private int _SizeLastSecond;

        public DateTime LastTimeUTC { get { return _LastTime; } }
        private DateTime _LastTime;

        private static TimeSpan OneSecond = TimeSpan.FromSeconds(1);
        public int LastSize
        {
            get
            {
                return _LastSize;
            }
            set
            {
                CountAll++;
                SizeAll += value;

                _LastSize = value;
                _LastTime = DateTime.UtcNow;
                if (FirstTime == null)
                    FirstTime = _LastTime;
                _SizeLastSecond += value;
                _SizeQueue.Enqueue(Tuple.Create(_LastTime, value));

                while (true)
                {
                    var begin = _SizeQueue.Peek();
                    if ((_LastTime - begin.Item1) > OneSecond)
                    {
                        _SizeQueue.Dequeue();
                        _SizeLastSecond -= begin.Item2;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        private int _LastSize;
    }

    public class DataSizeCounter
    {
        public int Count { get { return _Count; } }
        public int Size { get { return _Size; } }
        public int LastSize
        {
            set
            {
                Interlocked.Add(ref _Size, value);
                Interlocked.Increment(ref _Count);
            }
        }
        private int _Count;
        private int _Size;
    }

    public class SRDataSizeCounter
    {
        public DataSizeCounter Send { get; private set; }
        public DataSizeCounter Recv { get; private set; }

        public SRDataSizeCounter()
        {
            Send = new DataSizeCounter();
            Recv = new DataSizeCounter();
        }
    }

    public class PlayerLinkDebugInfo
    {
        public object State { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime ConnectedTime { get; set; }
        public DateTime LastWakeupTime { get; set; }

        public int RunningSeconds { get { return (LastSRTime - CreatedTime).Seconds; } }
        public DateTime LastSRTime
        {
            get
            {
                return RAWSendBandwidth.LastTimeUTC > RAWRecvBandwidth.LastTimeUTC ?
                    RAWSendBandwidth.LastTimeUTC : RAWRecvBandwidth.LastTimeUTC;
            }
        }

        public readonly BandWidthUsage RAWSendBandwidth = new BandWidthUsage();
        public readonly BandWidthUsage RAWRecvBandwidth = new BandWidthUsage();

        /// <summary> 应用包发送和接收次数和大小记录 </summary>
        public readonly SRDataSizeCounter AppData = new SRDataSizeCounter();

        public string VerboseStatus
        {
            get
            {
                return "LastReceive=[{0}/{1}] LastSend=[{2}/{3}] AppSent={4} AppRecieve={5} RawSent={6} RawReceived={7}".f(
                    RAWRecvBandwidth.LastTimeUTC.ToString("mm:ss.fff"), RAWRecvBandwidth.LastSize,
                    RAWSendBandwidth.LastTimeUTC.ToString("mm:ss.fff"), RAWSendBandwidth.LastSize, AppData.Send.Count, AppData.Recv.Count,
                    RAWSendBandwidth.CountAll, RAWRecvBandwidth.CountAll);
            }
        }
    }

    public delegate void RecvDataHandler(IPlayerLink playerLink, byte[] data, int offset, int size);
    public interface IPlayerLink
    {
        /// <summary>
        /// 仅仅用于辅助框架开发者和使用者开发调试，逻辑上无实际用途
        /// </summary>
        uint DisplayName { get; }
        string PK { get; }
        string VerboseStatus { get; }
        PlayerLinkStatus Status { get; }
        IEnumerable<PlayerLinkStatus> StatusHistory { get; }
        /// <summary>
        /// 试图和通讯对方通过关闭协议来安全关闭。比如
        /// http://www.tcpipguide.com/free/t_TCPConnectionTermination-2.htm
        /// </summary>
        void CloseGracefully();

        /// <summary>
        /// 立即强制关闭
        /// </summary>
        void CloseForcefully();

        bool SuspendFlush { set; }

        /// <summary>
        /// 发送数据。如果希望立刻在当前线程发送，则backGound为false。可能有些实现只能支持一种方式。
        /// 一般情况下，同步发送效率最高，因为线程或者任务切换带来的损耗更大。
        /// 如果连接不在Connected状态，则返回false。
        /// </summary>
        bool Send(byte[] data, int offset, int size, bool backGround, bool zip = false);

        /// <summary>
        /// 收到数据事件
        /// 数据来自KCPServer.BytePool，调用完毕将立即回收。
        /// 如果有需要，请自行Copy。
        /// </summary>
        event RecvDataHandler RecvData;
        event Action<IPlayerLink, PlayerLinkStatus> Event;

        /// <summary>
        /// 一般在实现类构造函数中赋值并不再修改。
        /// </summary>
        Object State { get; }
        /// <summary>
        /// NOZUONODIE
        /// </summary>
        object Secret { get; }

        PlayerLinkDebugInfo DebugInfo { get; }
    }

    public interface IPlayerLinkClient : IPlayerLink, IPackageCounter
    {
        //uint LocalSessionId { get; }
        //uint RemoteSessionId { get; }
        event Action<IPlayerLinkClient, byte[], int, int> RefusedEvent;
    }

    public interface IPlayerLinkClientSync : IPlayerLinkClient
    {
        /// <summary>
        /// 连接到服务端。
        /// </summary>
        void Connect(byte[] serverEndPoint, byte localSessionId, byte[] handShakeData,
            Func<byte[], int, int, bool> hv, Action<IPlayerLinkClient, PlayerLinkStatus> onThreadExit);
    }

    [ProtoContract]
    public class SimpleServerEndPoint
    {
        [ProtoMember(1)]
        public string InternetIP;
        [ProtoMember(2)]
        public ushort InternetPort;

        public SimpleServerEndPoint() { }
        public SimpleServerEndPoint(string ip, ushort port)
        {
            InternetIP = ip;
            InternetPort = port;
        }

        public static SimpleServerEndPoint Create(byte[] data, int offset, int size)
        {
            return DirectProtoBufTools.Deserialize<SimpleServerEndPoint>(data, offset, size);
        }

        public static byte[] GetBytes(string ip, ushort port)
        {
            return DirectProtoBufTools.Serialize(new SimpleServerEndPoint(ip, port));
        }

        public byte[] GetBytes()
        {
            return DirectProtoBufTools.Serialize(this);
        }
    }

    public static class PlayerLinkExtention
    {
        /// <summary>
        /// 自动使用ArrayPool
        /// </summary>
        public static bool Send(this IPlayerLink client, string s, bool backGround)
        {
            using (var sb = ArrayPool<byte>.Shared.CreateSB(Encoding.UTF8.GetMaxByteCount(s.Length)))
            {
                return client.Send(sb.Data, 0, Encoding.UTF8.GetBytes(s, 0, s.Length, sb.Data, 0), backGround);
            }
        }

        /// <summary>
        /// 函数不会修改data，返回后不会再使用 data
        /// </summary>
        public static bool Send(this IPlayerLink client, byte[] data, bool backGround)
        {
            return client.Send(data, 0, data.Length, backGround);
        }
    }

}
