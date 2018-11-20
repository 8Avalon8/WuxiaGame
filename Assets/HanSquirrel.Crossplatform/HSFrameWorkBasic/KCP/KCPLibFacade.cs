using GLib;
using HSFrameWork.Common;
using HSFrameWork.Net;
using System;
using System.Buffers;
using System.Net.Sockets;

namespace HSFrameWork.KCP.Common
{
    public class PlayerLinkDebugInfoKCP : PlayerLinkDebugInfo
    {
        /// <summary> UDP包头收发情况 </summary>
        public readonly SRDataSizeCounter UdpHeader = new SRDataSizeCounter();

        /// <summary> KCPUP包头收发情况 </summary>
        public readonly SRDataSizeCounter KCPUpHeader = new SRDataSizeCounter();
    }

    public static class KCPGlobalOptions
    {
        /// <summary>
        /// 多久没有收到数据则认为是死链,默认10秒
        /// </summary>
        public static TimeSpan MaxTimeNoData = new TimeSpan(0, 0, 5);

        /// <summary>
        /// 变为Closing2状态和IDLE后多久就退出。
        /// </summary>
        public static int ExitWaitAfterClosing2 = 1000;

        /// <summary>
        /// 最短多久需要收到握手反馈消息。
        /// </summary>
        public static int HandshakeDelay = 2000;

        public static bool EnableKCPEncrypt = true;

        private const int SIO_UDP_CONNRESET = -1744830452;
        public static bool CanDisableUdpConnectReset = false;
        public static void DisableUDP_CONN_RESET(UdpClient udpclient)
        {
            //在unity下面不支持这个函数，因此会异常
            if (CanDisableUdpConnectReset)
                udpclient.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
        }

        /// <summary>
        /// 框架开发者测试异常处理专用。
        /// </summary>
        public static bool ServerDispatchUDPAuto { get; set; }

        public static bool SimuProcessNewSessionExceptionServer { get; set; }
        public static bool SimuUDPThreadExceptionServer { get; set; }
        public static bool SimuDispatchThreadExceptionServer { get; set; }
        public static bool SimuUdpSendExceptionServer { get; set; }
        public static bool SimuUdpSendExcetionClient { get; set; }
        public static bool SimuUdpReceiveExceptionClient { get; set; }
        public static bool SimuKCPLibInputExceptionClient { get; set; }
        //public static bool SimuKCPLibFlushExceptionClient { get; set; }
        public static bool SimuKCPLibUpdateExceptionClient { get; set; }
        public static bool SimuKCPLibInputExceptionServer { get; set; }
        //public static bool SimuKCPLibFlushExceptionServer { get; set; }
        public static bool SimuKCPLibUpdateExceptionServer { get; set; }
        /// <summary>
        /// 框架内部不会自动复位，一直有效。
        /// </summary>
        public static bool SimuUdpTrashPacketClient { get; set; }
        public static bool SimuUdpTrashPacketServer { get; set; }
        public static bool SimuKCPUpDataFormatErrorClient { get; set; }
        public static bool SimuKCPUpDataFormatErrorServer { get; set; }
        public static bool SimuKCPUpDataTypeErrorClient { get; set; }
        public static bool SimuKCPUpDataTypeErrorServer { get; set; }
    }

    public class KCPLibEx : KCPLib
    {
        public static ArrayPool<byte> SharedBytePool { get { return _BytePool; } }
        private static ArrayPool<byte> _BytePool = ArrayPool<byte>.Shared;

        private bool _Client;
        public KCPLibEx(bool client, uint conv_, int sendLogMaxSize, Action<byte[], int> output_, Action onDirty) : base(conv_, output_, sendLogMaxSize, onDirty)
        {
            _Client = client;
        }

        public new int Input(byte[] data, int dataOffset, int dataSize)
        {
            if (KCPGlobalOptions.SimuKCPLibInputExceptionClient && _Client)
            {
                KCPGlobalOptions.SimuKCPLibInputExceptionClient = false;
                throw new Exception("蓝莲花");
            }
            if (KCPGlobalOptions.SimuKCPLibInputExceptionServer && !_Client)
            {
                KCPGlobalOptions.SimuKCPLibInputExceptionServer = false;
                throw new Exception("蓝莲花");
            }
            return base.Input(data, dataOffset, dataSize);
        }

#if false
        public void Flush()
        {
            if (KCPGlobalOptions.SimuKCPLibFlushExceptionClient && _Client)
            {
                KCPGlobalOptions.SimuKCPLibFlushExceptionClient = false;
                throw new Exception("蓝莲花");
            }
            if (KCPGlobalOptions.SimuKCPLibFlushExceptionServer && !_Client)
            {
                KCPGlobalOptions.SimuKCPLibFlushExceptionServer = false;
                throw new Exception("蓝莲花");
            }
            TryFlush(true);
        }
#endif
        public new bool Update()
        {
            if (KCPGlobalOptions.SimuKCPLibUpdateExceptionClient && _Client)
            {
                KCPGlobalOptions.SimuKCPLibUpdateExceptionClient = false;
                throw new Exception("蓝莲花");
            }
            if (KCPGlobalOptions.SimuKCPLibUpdateExceptionServer && !_Client)
            {
                KCPGlobalOptions.SimuKCPLibUpdateExceptionServer = false;
                throw new Exception("蓝莲花");
            }
            return base.Update();
        }

        static KCPLibEx()
        {
            KCPGlobalOptions.ServerDispatchUDPAuto = true;
            KCPLib.BufferAlloc = _BytePool.Rent;
            KCPLib.BufferFree = (buf) =>
            {
                _BytePool.Return(buf);
            };
        }
    }
}