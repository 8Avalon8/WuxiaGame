using GLib;
using System;
using System.Collections.Generic;

namespace HSFrameWork.KCP.Common
{
    public partial class KCPLib : IDisposable
    {
        // KCP Segment Definition
        private class Segment
        {
            public void Clear()
            {
                FirstSendTime = uint.MaxValue;
                //conv = 0;
                cmd = 0;
                frg = 0;
                RecvQueueFreeSlots = 0;
                ts = 0;
                SendSN = 0;
                una = 0;
                resendts = 0;
                rto = 0;
                fastack = 0;
                xmit = 0;
                AppDataSeq = 0;
                LastRTO = 0;
                SendReason = SendReasonEnum.NONE;
            }

            internal uint FirstSendTime = uint.MaxValue;
            //internal UInt32 conv = 0;
            internal byte cmd = 0;
            internal UInt32 frg = 0;
            /// <summary>
            /// recived_window_unused
            /// </summary>
            internal UInt32 RecvQueueFreeSlots = 0;
            /// <summary>
            /// 最近一次发送的时间
            /// </summary>
            internal UInt32 ts = 0;
            /// <summary>
            /// 在发送端设置，按照Segment来递增，创建后只读。
            /// </summary>
            internal UInt32 SendSN = 0;
            /// <summary>
            /// 当前还未确认的数据包的编号
            /// </summary>
            internal UInt32 una = 0;
            /// <summary>
            /// 下一次重发的时间
            /// </summary>
            internal UInt32 resendts = 0;
            internal UInt32 rto = 0;
            internal UInt32 fastack = 0;
            /// <summary>
            /// 总发送次数。
            /// </summary>
            internal UInt32 xmit = 0;
            internal ArraySegment<byte> data;

            /// <summary>
            /// 应用调用Send时的Sequence，创建后只读
            /// </summary>
            internal ushort AppDataSeq = 0;

            /// <summary>
            /// 上次发送的时候的RTO，仅仅开发调试使用。
            /// </summary>
            internal UInt32 LastRTO = 0;
            /// <summary>
            /// 发送原因，内部调试使用。
            /// </summary>
            internal SendReasonEnum SendReason;
            internal enum SendReasonEnum
            {
                NONE,
                FIRST,
                LOST,
                FASTACK
            }

            private KCPLib _Lib;
            public Segment(KCPLib lib)
            {
                _Lib = lib;
            }

            // encode a segment into buffer
            private int EncodeHeader(byte[] ptr, int offset)
            {
                var offset_ = offset;

                //offset += ikcp_encode32u(ptr, offset, conv);
                offset += ikcp_encode8u(ptr, offset, (byte)cmd);
                offset += ikcp_encode8u(ptr, offset, (byte)frg);
                offset += ikcp_encode16u(ptr, offset, (UInt16)RecvQueueFreeSlots);
                offset += ikcp_encode32u(ptr, offset, ts);
                offset += ikcp_encode32u(ptr, offset, SendSN);
                offset += ikcp_encode32u(ptr, offset, una);
                offset += ikcp_encode32u(ptr, offset, (UInt32)data.Count);

                return offset - offset_;
            }

            /// <summary>
            /// 几乎就是等于发送出去。
            /// </summary>
            public void Output()
            {
                if (FirstSendTime == uint.MaxValue)
                {
                    FirstSendTime = _Lib.Current;
                }

                var helper = _Lib.OutputHelper;
                helper.Prepare(IKCP_OVERHEAD + data.Count);
                helper.Offset += EncodeHeader(helper.Buffer, helper.Offset);

                if (data.Count > 0)
                {
                    Array.Copy(data.Array, data.Offset, helper.Buffer, helper.Offset, data.Count);
                    helper.Offset += data.Count;

                    _Lib.KCPHeaderWithPayload.Send.LastSize = IKCP_OVERHEAD;
                    if (cmd == IKCP_CMD_REPUSH)
                        _Lib.KCPPayloadRetrans.Send.LastSize = data.Count;
                }
                else
                {
                    _Lib.KCPHeaderWithoutPayload.Send.LastSize = IKCP_OVERHEAD;
                }

                var logger = _Lib.GetLogger();
                if (logger != null)
                {
                    if (data.Count > 0)
                        logger.Trace("{0}{1}[{2:D3}     {3}{4:X4}* {5}{6:X8}] XMIT={7} {8} LastRTO={9}, RTO={10}",
                            _Lib.ServerSide ? "      << LIBSF  S" : "LIBCF >>        C", //0
                            _Lib.DisplayName, data.Count, //1,2
                            _Lib.ServerSide ? "appS" : "appC", AppDataSeq, //3,4
                            _Lib.ServerSide ? "snS" : "snC", SendSN, //5,6
                            xmit, SendReason, //7, 8
                            LastRTO, rto); //9,10
                    else
                        logger.Trace("{0}{1}[{2}                {3}{4:X8}]",
                            _Lib.ServerSide ? "      << LIBSF  S" : "LIBCF >>        C",
                            _Lib.DisplayName, CmdToString(cmd),
                            _Lib.ServerSide ? "snC" : "snS", SendSN);
                }
            }
        }

        private class CSegPool : Helper
        {
            private readonly SimpleObjectPool<Segment> _Pool;

            public CSegPool()
            {
                _Pool = new SimpleObjectPool<Segment>(128, () => new Segment(Context), seg => seg.Clear());
            }

            public Segment PopSegment(int size)
            {
                var seg = _Pool.Spawn();
                seg.data = new ArraySegment<byte>(BufferAllocInner(size), 0, size);
                return seg;
            }

            /// <summary>
            /// 同时会释放内部的缓冲区
            /// </summary>
            public void PushSegment(Segment seg)
            {
                BufferFreeInner(seg.data.Array);
                _Pool.Despawn(seg);
            }
        }

        private class CCogWinHelper : Helper
        {
            /// <summary> 本地拥塞窗口，不会超过_rmt_wnd_free </summary>
            public UInt32 Cogwnd { get; private set; }

            /// <summary> 可发送的最大数据量。仅仅用于更新_cogWnd。 </summary>
            private UInt32 _incr;
            /// <summary>
            /// 拥塞窗口先行增长的阈值。
            /// 当拥塞窗口增长到此阈值以后，就减慢增长速度，缓慢增长。
            /// </summary>
            private UInt32 _ssthresh = IKCP_THRESH_INIT;

            public void UpdateCogWinInInput(uint old_snd_una)
            {
                //重新计算拥塞窗口
                if (_itimediff(Context.SendHelper.SendUNA, old_snd_una) > 0 && Cogwnd < Context.InputHelper.RemoteReceiveQueueFreeSlots)
                {
                    if (Cogwnd < _ssthresh)
                    {//线性增长
                        Cogwnd++;
                        _incr += MSS;
                    }
                    else
                    {
                        if (_incr < MSS)
                        {
                            _incr = MSS;
                        }
                        _incr += (MSS * MSS) / _incr + (MSS / 16);
                        if ((Cogwnd + 1) * MSS <= _incr)
                            Cogwnd++;
                    }

                    if (Cogwnd > Context.InputHelper.RemoteReceiveQueueFreeSlots)
                    {
                        Cogwnd = Context.InputHelper.RemoteReceiveQueueFreeSlots;
                        _incr = Context.InputHelper.RemoteReceiveQueueFreeSlots * MSS;
                    }
                }
            }

            public void UpdateCogWinInFlush(bool quickResendHappend, bool lostHappened, uint resent)
            {
                // update ssthresh
                if (quickResendHappend)
                {
                    _ssthresh = (Context.SendHelper.SendSNNext - Context.SendHelper.SendUNA) / 2; //下一个要分配的包 - 第一个未确认的包
                                                                                                  //当发生快速重传的时候，会将慢启动阈值调整为当前活动发送窗口的一半

                    if (_ssthresh < IKCP_THRESH_MIN)
                        _ssthresh = IKCP_THRESH_MIN;
                    Cogwnd = _ssthresh + resent; //resent有可能是0XFFFFFFFF。
                    _incr = Cogwnd * MSS;
                }

                if (lostHappened)
                {
                    _ssthresh = Cogwnd / 2; //丢包了。慢启动阈值需要减半
                    if (_ssthresh < IKCP_THRESH_MIN)
                        _ssthresh = IKCP_THRESH_MIN;
                    Cogwnd = 1;
                    _incr = MSS;
                }

                if (Cogwnd < 1)
                {
                    Cogwnd = 1;
                    _incr = MSS;
                }
            }
        }

        /// <summary>
        /// 是TCP RTO的标准计算方式，微微修正。
        /// http://sgros.blogspot.com/2012/02/calculating-tcp-rto.html
        /// </summary>
        private class CRTOHelper : Helper
        {
            /// <summary>ack接收延迟计算出来的重传超时时间</summary>
            public UInt32 RTO { get; private set; }

            private const int RTTARRAY_SIZE = 1024;
            public readonly Int32[] RTTArray = new Int32[RTTARRAY_SIZE];
            public int LastRtt { get { return RTTArray[LastRTTIndex % RTTARRAY_SIZE]; } }
            public void GetRttVerboseInfo(out int min, out int max, out int avg, out int last, out int smoothed, out int var)
            {
                min = int.MaxValue;
                max = 0;
                int sum = 0;
                int i;
                for (i = 0; i < Math.Min(LastRTTIndex, RTTARRAY_SIZE); i++)
                {
                    int current = RTTArray[i];
                    if (current < min) min = current;
                    if (current > min) max = current;
                    sum += current;
                }

                avg = i != 0 ? sum / i : 0;
                last = LastRtt;
                smoothed = _Smoothed;
                var = _var;
            }
            public int LastRTTIndex { get; private set; }

            /// <summary>构造时设置。最小重传超时时间，仅仅用于计算_rx_rto</summary>
            public uint RX_MinRTO = IKCP_RTO_MIN;
            public CRTOHelper()
            {
                RTO = IKCP_RTO_DEF;
            }

            /// <summary>RTT移动方差</summary>
            private int _var;
            public int SRTT { get { return _Smoothed; } }
            /// <summary>RTT移动平均值</summary>
            private int _Smoothed;
            /// <summary>
            ///  根据接收的RTT重新计算 _rx_rto
            ///  原名：update_ack
            /// </summary>
            public void Update(Int32 rtt)
            {
                RTTArray[(++LastRTTIndex) % RTTARRAY_SIZE] = rtt;
                if (0 == _Smoothed)
                {
                    _Smoothed = rtt;
                    //_rttvar = rtt / 2;
                    _var = rtt / 10; //GG
                }
                else
                {
                    int delta = Math.Abs(rtt - _Smoothed);
                    _var = (3 * _var + delta) / 4; //方差以3/4保留历史
                    _Smoothed = (7 * _Smoothed + rtt) / 8; //均值以7/8保留历史
                    if (_Smoothed < 1) //这里是KCP原始作者的改进，为了不让 _rttvar复位
                        _Smoothed = 1;
                }

                var rto = (uint)_Smoothed + _imax_(Context.FlushInterval, 4 * (uint)_var, 10);
                //KCP优化 20180807 发现重传频率很高，查看日志发现几乎重传后几毫秒就收到了ACK。再仔细看发现var=1，故此加上10ms。
                RTO = _ibound_(RX_MinRTO, rto, IKCP_RTO_MAX);

                var logger = Context.GetLogger();
                if (logger != null)
                    logger.Trace("rtt={0}, Smoothed={1}, var={2}, RTO={3}", rtt, _Smoothed, _var, RTO);
            }
        }

        private class CProbehelper : Helper
        {
            public UInt32 Mask { get; set; }

            /// <summary> 探查窗口需要等待的时间 </summary>
            private UInt32 _probe_wait;
            /// <summary> 下次探查窗口的时间戳 </summary>
            public UInt32 NextSendTime { get; private set; }

            public CProbehelper()
            {
                NextSendTime = UInt32.MaxValue;
            }

            public void TryOutputProbe(Segment seg)
            {
                //GGTODO:如果探查窗口UDP包没有被收到，会如何？
                //STEP2 probe window size (if remote window size equals zero)
                if (0 == Context.InputHelper.RemoteReceiveQueueFreeSlots)  //远端接收窗口大小为0的时候
                {
                    if (0 == _probe_wait) //探查窗口需要等待的时间为0
                    {
                        _probe_wait = IKCP_PROBE_INIT;  //设置探查窗口需要等待的时间
                        NextSendTime = Context.Current + _probe_wait;
                        //下次探查窗口的时间戳 = 当前时间 + 探查窗口等待时间间隔
                    }
                    else
                    {
                        if (_itimediff(Context.Current, NextSendTime) >= 0) //当前时间 > 下一次探查窗口的时间
                        {
                            if (_probe_wait < IKCP_PROBE_INIT)
                                _probe_wait = IKCP_PROBE_INIT;
                            _probe_wait += _probe_wait / 2;  //等待时间变为之前的1.5倍
                            if (_probe_wait > IKCP_PROBE_LIMIT)
                                _probe_wait = IKCP_PROBE_LIMIT;
                            NextSendTime = Context.Current + _probe_wait;
                            Mask |= IKCP_MASK_ASK_REMOTE;
                        }
                    }
                }
                else
                {
                    NextSendTime = UInt32.MaxValue;
                    _probe_wait = 0;
                }

                // flush window probing commands
                if ((Mask & IKCP_MASK_ASK_REMOTE) != 0)
                {
                    seg.cmd = IKCP_CMD_WASK;
                    seg.Output();
                }

                // flush window probing commands
                //原始C#没有这段代码。而C有。
                if ((Mask & IKCP_MASK_ANSWER_REMOTE) != 0)
                {
                    seg.cmd = IKCP_CMD_WINS;
                    seg.Output();
                }

                Mask = 0;
            }
        }

        private class CCmdHelper : Helper
        {
            public UInt32 NextSendTime
            {
                get
                {
                    return Math.Min(_NextSendTime, Context.Probehelper.NextSendTime);
                }
            }

            private UInt32 _NextSendTime = UInt32.MaxValue;

            private List<uint> _AckList = new List<uint>(1024);
            public void AddAck(uint sn, uint ts)
            {
                if (_NextSendTime > Context.Current)
                    _NextSendTime = Context.Current;
                _AckList.Add(sn);
                _AckList.Add(ts);
            }

            public int WaitSnd { get { return _AckList.Count / 2; } }

            private Segment PopCmdSegment()
            {
                var seg = Context.SegPool.PopSegment(0);
                //seg.conv = Context.Conv;
                seg.RecvQueueFreeSlots = Context.InputHelper.ReceiveQueueFreeSlots;
                seg.una = Context.InputHelper.ReceiveNext;
                return seg;
            }

            private void TryOutputAcks(Segment segACK)
            {
                segACK.cmd = IKCP_CMD_ACK;
                for (var i = 0; i < _AckList.Count;)
                {
                    segACK.SendSN = _AckList[i++];
                    segACK.ts = _AckList[i++];
                    segACK.Output();
                }
                _AckList.Clear();
                _NextSendTime = UInt32.MaxValue;
            }

            public void TryOutputAllCmds()
            {
                var sharedSeg = PopCmdSegment();
                TryOutputAcks(sharedSeg);
                Context.Probehelper.TryOutputProbe(sharedSeg);
                Context.SegPool.PushSegment(sharedSeg);
            }
        }

        private class COutputHelper : IDisposable
        {
            public readonly byte[] Buffer;
            public int Offset;
            private Action<byte[], int> _OutputAction;
            public COutputHelper(Action<byte[], int> output)
            {
                _OutputAction = output;
                Buffer = BufferAllocInner((int)((MTU + IKCP_OVERHEAD) * 3));
            }

            public void Prepare(int size)
            {
                if (Offset + size > MTU)
                {
                    _OutputAction(Buffer, Offset);
                    Offset = 0;
                }
            }

            public void Flush()
            {
                if (Offset > 0)
                {
                    _OutputAction(Buffer, Offset);
                    Offset = 0;
                }
            }

            private bool _Disposed = false;
            public void Dispose()
            {
                if (!_Disposed)
                {
                    _Disposed = true;
                    BufferFreeInner(Buffer);
                }
            }
        }

        // encode 8 bits unsigned int
        public static int ikcp_encode8u(byte[] p, int offset, byte c)
        {
            p[0 + offset] = c;
            return 1;
        }

        // decode 8 bits unsigned int
        public static int ikcp_decode8u(byte[] p, int offset, ref byte c)
        {
            c = p[0 + offset];
            return 1;
        }

        /* encode 16 bits unsigned int (lsb) */
        public static int ikcp_encode16u(byte[] p, int offset, UInt16 w)
        {
            p[0 + offset] = (byte)(w >> 0);
            p[1 + offset] = (byte)(w >> 8);
            return 2;
        }

        /* decode 16 bits unsigned int (lsb) */
        public static int ikcp_decode16u(byte[] p, int offset, ref UInt16 c)
        {
            UInt16 result = 0;
            result |= (UInt16)p[0 + offset];
            result |= (UInt16)(p[1 + offset] << 8);
            c = result;
            return 2;
        }

        /* encode 32 bits unsigned int (lsb) */
        public static int ikcp_encode32u(byte[] p, int offset, UInt32 l)
        {
            p[0 + offset] = (byte)(l >> 0);
            p[1 + offset] = (byte)(l >> 8);
            p[2 + offset] = (byte)(l >> 16);
            p[3 + offset] = (byte)(l >> 24);
            return 4;
        }

        /* decode 32 bits unsigned int (lsb) */
        public static int ikcp_decode32u(byte[] p, int offset, ref UInt32 c)
        {
            UInt32 result = 0;
            result |= (UInt32)p[0 + offset];
            result |= (UInt32)(p[1 + offset] << 8);
            result |= (UInt32)(p[2 + offset] << 16);
            result |= (UInt32)(p[3 + offset] << 24);
            c = result;
            return 4;
        }

        static UInt32 _imin_(UInt32 a, UInt32 b)
        {
            return a <= b ? a : b;
        }

        static UInt32 _imin_(UInt32 a, UInt32 b, UInt32 c)
        {
            var x = a <= b ? a : b;
            return x <= c ? x : c;
        }

        static UInt32 _imax_(UInt32 a, UInt32 b)
        {
            return a >= b ? a : b;
        }

        static UInt32 _imax_(UInt32 a, UInt32 b, UInt32 c)
        {
            var x = a >= b ? a : b;
            return x > c ? x : c;
        }

        static UInt32 _ibound_(UInt32 lower, UInt32 middle, UInt32 upper)
        {
            return _imin_(_imax_(lower, middle), upper);
        }

        static Int32 _itimediff(UInt32 later, UInt32 earlier)
        {
            return ((Int32)(later - earlier));
        }

    }
}
