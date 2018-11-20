using GLib;
using HSFrameWork.Common;
using HSFrameWork.Common.Inner;
using HSFrameWork.Net;
using System;
using System.Collections.Generic;

namespace HSFrameWork.KCP.Common
{
    /// <summary>
    /// 支持内存池，发送/接收队列和缓存使用链表代替v1中的数组。
    /// </summary>
#if HSFRAMEWORK_NET_ABOVE_4_5
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    public partial class KCPLib : IDisposable
    {
        //private UInt32 Conv;
        private CInputHelper InputHelper;
        private CSendHelper SendHelper;
        private CCogWinHelper CogWinHeler;
        private CRTOHelper RTOHelper;
        private CProbehelper Probehelper;
        private CCmdHelper CmdHelper;
        private COutputHelper OutputHelper;
        private CSegPool SegPool;

        public KCPLib(UInt32 conv, Action<byte[], int> output, int sendLogMaxSize, Action onDirty)
        {
            //Conv = conv;
            InputHelper = new CInputHelper() { Context = this };
            SendHelper = new CSendHelper(sendLogMaxSize) { Context = this };
            CogWinHeler = new CCogWinHelper() { Context = this };
            RTOHelper = new CRTOHelper() { Context = this };
            Probehelper = new CProbehelper() { Context = this };
            CmdHelper = new CCmdHelper() { Context = this };
            SegPool = new CSegPool() { Context = this };
            OutputHelper = new COutputHelper(output);
            OnDirty = onDirty;
            _ClockBase = SharedG.LClock;
            if (_ClockBase < 0XFFFF) _ClockBase = 0;
            FlushInterval = IKCP_INTERVAL_DEFAULT;
            if (sendLogMaxSize > 0)
                ZipLog = new FixedCircularQueue<ZipLogItem>(sendLogMaxSize / 24);
        }

        private Action OnDirty;
        public Func<IHSLogger> GetLogger;
        public bool ServerSide { get; set; }
        public uint DisplayName { get; set; }
        public static Func<int, byte[]> BufferAlloc
        {
            set
            {
                BufferAllocInner = value != null ? value : s => new byte[s];
            }
        }
        public static Action<byte[]> BufferFree
        {
            set
            {
                BufferFreeInner = value != null ? value : s => { };
            }
        }

        private static Func<int, byte[]> BufferAllocInner = s => new byte[s];
        private static Action<byte[]> BufferFreeInner = s => { };

        /// <summary> 构造时设置，Flush的间隔 </summary>
        public UInt32 FlushInterval;
        /// <summary> 构造时设置。是否禁用拥塞控制。</summary>
        private bool DisableCogWin;
        // fastest: ikcp_nodelay(kcp, 1, 20, 2, 1)
        // nodelay: 0:disable(default), 1:enable
        // interval: internal update timer interval in millisec, default is 100ms
        // resend: <=0:disable fast resend(default), 1+:enable fast resend
        // nc: 0:normal congestion control(default), 1:disable congesti_updatedon control
        public int NoDelay(int nodelay_, int interval_, int resend_, int nc_)
        {
            if (nodelay_ > 0)
            {
                SendHelper.NoDelayOption = nodelay_ != 0;
                RTOHelper.RX_MinRTO = nodelay_ != 0 ? IKCP_RTO_NDL : IKCP_RTO_MIN;
            }

            FlushInterval = (UInt32)interval_;

            if (resend_ >= 0)
                SendHelper.FastResend = resend_;

            if (nc_ >= 0)
                DisableCogWin = nc_ != 0;

            return 0;
        }

        // set maximum window size: sndwnd=32, rcvwnd=32 by default
        public int WndSize(int sndwnd, int rcvwnd)
        {
            if (sndwnd > 0)
                SendHelper.SndWinUpLimit = (UInt32)sndwnd; //SET

            if (rcvwnd > 0)
                InputHelper.RcvWinUpLimit = (UInt32)rcvwnd; //SET
            return 0;
        }

        private long _ClockBase;
        public uint IClock
        {
            get
            {
                return (uint)(SharedG.LClock - _ClockBase);
            }
        }

        public long LClock(uint iclock)
        {
            return _ClockBase + iclock;
        }

        /// <summary>
        /// Cached Time，降低KCP时钟精度为1MS
        /// </summary>
        public UInt32 Current { get; private set; }

        public int Input(byte[] data, int dataOffset, int maxOffset)
        {
            Current = IClock;
            var ret = InputHelper.Input(data, dataOffset, maxOffset);
            AfterChanged();
            return ret;
        }

        public int PeekSize()
        {
            return InputHelper.PeekSize();
        }

        public int Recv(byte[] buffer)
        {
            var ret = InputHelper.Recv(buffer);
            AfterChanged();
            return ret;
        }

        /// <summary>
        /// appDataSeq是应用调用Send的序列号。
        /// </summary>
        public int Send(byte[] buffer, int index, int bufsize, ushort appDataSeq)
        {
            Current = IClock;
            var ret = SendHelper.Send(buffer, index, bufsize, appDataSeq);
            AfterChanged();
            return ret;
        }

        /// <summary>
        /// force：无条件Flush
        /// </summary>
        public bool TryFlush(bool force)
        {
            Current = IClock;
            if (!force && Check() > Current)
                return false;

            CmdHelper.TryOutputAllCmds();
            if (force || _itimediff(Current, SendHelper.NextFlushTime) >= 0)
                SendHelper.TryFlush();
            OutputHelper.Flush();
            LastFlush = Current;
            return true;
        }

        /// <summary>
        /// 上次Flush的时间。
        /// </summary>
        public UInt32 LastFlush { get; private set; }

        /// <summary>
        /// 尚未确定收到的Segment的个数+等待发送的ACK个数
        /// </summary>
        public int WaitSndEx()
        {
            return SendHelper.WaitSnd() + CmdHelper.WaitSnd;
        }

        /// <summary>
        /// 尚未确定收到的Segment的个数
        /// </summary>
        public int WaitSnd()
        {
            return SendHelper.WaitSnd();
        }

        public int CmdToSend()
        {
            return CmdHelper.WaitSnd;
        }

        public UInt32 NextFlush { get { return Math.Min(SendHelper.NextFlushTime, CmdHelper.NextSendTime); } }
        /// <summary>
        /// 下一次需要Flush的时间
        /// </summary>
        public UInt32 Check()
        {
            Current = IClock;
            if (SuspendFlush)
                return UInt32.MaxValue;

            var x = Math.Max(NextFlush, LastFlush);
            return x == UInt32.MaxValue ? UInt32.MaxValue : (x + FlushInterval);
            //如果下次要更新的时间TX大于_LastFlushTime，则在TX+5更新；这样可以让突发性的更新都延迟5ms。
            //如果TX小于_LastFlushTime（不可能）
        }

        public bool Idle()
        {
            return SendHelper.WaitSnd() == 0 && NextFlush == uint.MaxValue;
        }

        public bool Update()
        {
            if (SuspendFlush)
                return false;

            return TryFlush(false);
        }

        private volatile int _SuspendedCount = 0;
        private volatile bool _Dirty = false;
        public bool SuspendFlush
        {
            private get
            {
                return _SuspendedCount != 0;
            }
            set
            {
                _SuspendedCount += value ? 1 : -1;

                if (_SuspendedCount == 0 && _Dirty)
                {
                    _Dirty = false;
                    OnDirty();
                }
            }
        }

        private void AfterChanged()
        {
            if (!SuspendFlush)
            {
                _Dirty = false;
                OnDirty();
            }
            else
                _Dirty = true;
        }

        public UInt32 RX_RTO { get { return RTOHelper.RTO; } }
        public int LastRtt { get { return RTOHelper.LastRtt; } }
        public int SRTT { get { return RTOHelper.SRTT; } }
        public void GetRttVerboseInfo(out int min, out int max, out int avg, out int last, out int smoothed, out int var)
        {
            RTOHelper.GetRttVerboseInfo(out min, out max, out avg, out last, out smoothed, out var);
        }

        public class ZipLogItem
        {
            public ushort Org { get; set; }
            public ushort Zipped { get; set; }
        }
        public readonly FixedCircularQueue<ZipLogItem> ZipLog;

        /// <summary>
        /// NOT ThreadSafe
        /// </summary>
        public IEnumerable<SendLog> SendSegLog { get { return SendHelper.SendSegLog; } }

        /// <summary> KCP实际数据重发统计 </summary>
        public SRDataSizeCounter KCPPayloadRetrans = new SRDataSizeCounter();
        /// <summary> 有数据的KCP包头收发情况 </summary>
        public readonly SRDataSizeCounter KCPHeaderWithPayload = new SRDataSizeCounter();
        /// <summary> 无数据的KCP包头收发情况 </summary>
        public SRDataSizeCounter KCPHeaderWithoutPayload = new SRDataSizeCounter();

        private class Helper
        {
            public KCPLib Context;
        }

        public void Dispose()
        {
            InputHelper.Dispose();
            SendHelper.Dispose();
            OutputHelper.Dispose();
        }

        /// <summary> no delay min rto </summary>
        public const uint IKCP_RTO_NDL = 30;
        /// <summary> normal min rto </summary>
        public const uint IKCP_RTO_MIN = 100;
        /// <summary> 缺省 rto </summary>
        public const uint IKCP_RTO_DEF = 30;
        /// <summary> 最大 rto </summary>
        public const int IKCP_RTO_MAX = 1000;

        public static string CmdToString(byte cmd)
        {
            switch (cmd)
            {
                case IKCP_CMD_PUSH:
                    return "PUSH";
                case IKCP_CMD_REPUSH:
                    return "REPUSH";
                case IKCP_CMD_ACK:
                    return "ACK";
                case IKCP_CMD_WASK:
                    return "WASK";
                case IKCP_CMD_WINS:
                    return "WINS";
                default:
                    return "NA";
            }
        }

        public const byte IKCP_CMD_PUSH = 81; // cmd: push data
        public const byte IKCP_CMD_ACK = 82; // cmd: ack
        public const byte IKCP_CMD_WASK = 83; // cmd: window probe (ask)
        public const byte IKCP_CMD_WINS = 84; // cmd: window size (tell)
        public const byte IKCP_CMD_REPUSH = 85; // cmd: 重发数据
        /// <summary> need to send IKCP_CMD_WASK，要去问远端 </summary>
        public const int IKCP_MASK_ASK_REMOTE = 1; //原来是 IKCP_ASK_SEND
        /// <summary> need to send IKCP_CMD_WINS 要回答远端 </summary>
        public const int IKCP_MASK_ANSWER_REMOTE = 2; //原来是 IKCP_ASK_TELL

        public const int IKCP_WND_SND = 32;
        public const int IKCP_WND_RCV_DEFAULT = 32;
        public const int IKCP_MTU_DEF = 512;//默认MTU 1400
        public const int IKCP_ACK_FAST = 3;
        public static uint IKCP_INTERVAL_DEFAULT = 0;

        /// <summary> 最小的KCP输入包大小。 </summary>
        public const int IKCP_OVERHEAD = 20;
        public const int IKCP_DEADLINK = 20;//原来是10
        public const int IKCP_THRESH_INIT = 2;
        public const int IKCP_THRESH_MIN = 2;
        public const int IKCP_PROBE_INIT = 7000;   // 7 secs to probe window size
        public const int IKCP_PROBE_LIMIT = 120000; // up to 120 secs to probe window


        /// <summary> MaxTrasmitUnit 缺省是512 </summary>
        public const UInt32 MTU = IKCP_MTU_DEF;
        /// <summary> MaxSegmengSize 缺省是512-20 </summary>
        public const UInt32 MSS = IKCP_MTU_DEF - IKCP_OVERHEAD;
    }

#if HSFRAMEWORK_NET_ABOVE_4_5
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    public class DataInvalidException : Exception
    {
        public DataInvalidException(string msg) : base(msg) { }
    }
}
