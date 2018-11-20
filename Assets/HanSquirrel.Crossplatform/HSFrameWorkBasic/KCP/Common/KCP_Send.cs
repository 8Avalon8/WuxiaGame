using GLib;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HSFrameWork.KCP.Common
{
    public partial class KCPLib : IDisposable
    {
        public class SendLog
        {
            public uint SN { get; set; }
            public uint FirstTime { get; set; }
            public uint TransmitCount { get; set; }
            public uint LastTime { get; set; }
            public uint RemoveTime { get; set; }
            public uint AppRTT { get { return RemoveTime - FirstTime; } }
        }


        private class CSendHelper : Helper, IDisposable
        {
            private int _SendLogMaxSize;
            public CSendHelper(int sendLogMaxSize)
            {
                _SendLogMaxSize = sendLogMaxSize;
                if (_SendLogMaxSize > 0)
                {
                    SendSegLog = new C5.CircularQueue<SendLog>(sendLogMaxSize / 24);
                }
            }
            public readonly C5.CircularQueue<SendLog> SendSegLog;

            /// <summary> 构造时创建。bool，是否快速重传。 </summary>
            public bool NoDelayOption;
            /// <summary> 构造时设置。快速重传门限。若设置为2（则2次ACK跨越将会直接重传）。 </summary>
            public Int32 FastResend;
            /// <summary> 发送窗口的上限。在构造的时候设置。缺省为32. </summary>
            public UInt32 SndWinUpLimit = IKCP_WND_SND; //SET

            /// <summary> 第一个未确认是否收到的包 </summary>
            public UInt32 SendUNA { get; private set; }
            /// <summary> 下一个待分配的包的序号 </summary>
            public UInt32 SendSNNext { get; private set; }

            /// <summary> 发送队列，上层应用数据分片后加入发送队列 </summary>
            private LinkedList<Segment> _snd_queue = new LinkedList<Segment>();
            /// <summary> 发送缓冲区，已发送尚未确认的包 </summary>
            private LinkedList<Segment> _snd_buf = new LinkedList<Segment>();

            public int WaitSnd()
            {
                return _snd_buf.Count + _snd_queue.Count;
            }

            /// <summary>
            /// 根据_snd_buf和_snd_nxt确定 _snduna
            /// </summary>
            private void ResetSendUNA()
            {
                if (_snd_buf.Count > 0)
                    SendUNA = _snd_buf.First.Value.SendSN;
                else
                    SendUNA = SendSNNext;
            }

            private void RemoveNode(LinkedListNode<Segment> node)
            {
                var seg = node.Value;
                if (SendSegLog != null)
                {
                    SendLog sendLog = null;
                    while (SendSegLog.Count >= _SendLogMaxSize)
                    {//最多只可能等于，不可能大于。只是为了保险这么写。
                        sendLog = SendSegLog.Dequeue();
                        //复用sendLog，否则会new很多很多次。
                    }
                    if(sendLog == null)
                    {
                        sendLog = new SendLog();
                    }

                    sendLog.SN = seg.SendSN;
                    sendLog.FirstTime = seg.FirstSendTime;
                    sendLog.TransmitCount = seg.xmit;
                    sendLog.LastTime = seg.ts;
                    sendLog.RemoveTime = Context.Current;

                    SendSegLog.Enqueue(sendLog);
                }
                _snd_buf.Remove(node);
                _NextFlushTimeDirty = true;
                Context.SegPool.PushSegment(seg);
            }

            /// <summary>
            /// 根据从远端收到的ACK消息中的sn来删除本地发送队列中对应的数据包。
            /// </summary>
            public void RemoveSendSegmentByRemoteAckSN(UInt32 snAcked)
            {
                if (_itimediff(snAcked, SendUNA) < 0 || _itimediff(snAcked, SendSNNext) >= 0)
                    return;

                var node = _snd_buf.First;
                while (node != null)
                {
                    var seg = node.Value;
                    var next = node.Next;
                    if (snAcked == seg.SendSN)
                    {
                        RemoveNode(node);
                        break;
                    }
                    else
                    {
                        seg.fastack++;
                    }
                    node = next;
                }
                ResetSendUNA();
            }
            /// <summary>
            /// una =unacknowledge 待接收消息序号（接收滑动窗口左端）
            /// 从snd_buf中删除比远端的una早的数据包。
            /// </summary>
            public void RemoveSendSegmentsFromSendBuffer(UInt32 unaRemote)
            {
                var node = _snd_buf.First;
                while (node != null)
                {
                    var seg = node.Value;
                    var next = node.Next;
                    if (_itimediff(unaRemote, seg.SendSN) > 0)
                    {
                        RemoveNode(node);
                    }
                    else
                    {
                        break;
                    }
                    node = next;
                }
                ResetSendUNA();
            }
            /// <summary>
            /// 仅仅往snd_Queue里面添加数据。appDataSeq是应用调用Send的序列号。
            /// </summary>
            public int Send(byte[] buffer, int index, int bufsize, ushort appDataSeq)
            {
                if (0 == bufsize)
                    throw new ArgumentException("KCPLib.Send(bufsize不可以为0)");

                var count = 0;

                if (bufsize < MSS)
                    count = 1;
                else
                    count = (int)(bufsize + MSS - 1) / (int)MSS;

                if (255 < count)
                    throw new ArgumentException("KCPLib.Send(bufsize不可以超过{0})".f(255 * MSS));

                if (0 == count)
                    count = 1;

                var offset = index;

                for (var i = 0; i < count; i++)
                {
                    var size = Math.Min((int)MSS, bufsize);
                    var seg = Context.SegPool.PopSegment(size);

                    Array.Copy(buffer, offset, seg.data.Array, seg.data.Offset, size);

                    offset += size;
                    bufsize -= size; //原始C#代码里面没有此行代码。原始C代码里面有。
                    seg.frg = (UInt32)(count - i - 1);
                    seg.AppDataSeq = appDataSeq;
                    seg.ts = seg.resendts = Context.Current;
                    _snd_queue.AddLast(seg);
                }

                _NextFlushTimeDirty = true;
                return 0;
            }

            /// <summary> 总共发出的数据包个数 </summary>
            private UInt32 _xmit_dbg;
            private bool _SendBufferFull = false;
            private void TryMoveSendSegFromQueueToBuf()
            {
                var sndWinLimit = _imin_(SndWinUpLimit, Context.InputHelper.RemoteReceiveQueueFreeSlots, Context.DisableCogWin ? uint.MaxValue : Context.CogWinHeler.Cogwnd); //最重要

                //将发送队列（snd_queue)中的数据添加到发送缓存(snd_bu)
                // move data from snd_queue to snd_buf
                _SendBufferFull = false;
                var node = _snd_queue.First;
                while (node != null)
                {
                    //[_snd_una, ..., _snd_nxt)
                    if (_itimediff(SendSNNext, SendUNA + sndWinLimit) >= 0)
                    {
                        _SendBufferFull = true;
                        break;
                    }
                    // 这里是所有拥塞控制、发送窗口控制、接收窗口控制 的中枢所在。

                    Segment segToSend = node.Value;
                    var next = node.Next;

                    _snd_queue.Remove(node);
                    //segToSend.conv = Context.Conv;
                    segToSend.cmd = IKCP_CMD_PUSH;
                    segToSend.RecvQueueFreeSlots = Context.InputHelper.ReceiveQueueFreeSlots;
                    segToSend.ts = Context.Current;
                    segToSend.SendSN = SendSNNext++;
                    segToSend.una = Context.InputHelper.ReceiveNext;
                    segToSend.resendts = Context.Current;
                    segToSend.rto = Context.RTOHelper.RTO;
                    segToSend.fastack = 0;
                    segToSend.xmit = 0;
                    _snd_buf.AddLast(node);
                    node = next;

                    _NextFlushTime = Context.Current;
                }
            }

            /// <summary>
            /// 0表示正常，-1表示DEADLINK，目前没有什么用途
            /// </summary>_buffer
            int _state;

            private void TryOutputSendBuffer()
            {
                // calculate resent
                var resent = FastResend > 0 ? (UInt32)FastResend : uint.MaxValue;

                bool quickResendHappend = false;
                bool lostHappened = false;
                // flush data segments
                foreach (var segment in _snd_buf)
                {
                    var needsend = false;
                    var debug = _itimediff(Context.Current, segment.resendts);
                    if (0 == segment.xmit)
                    {
                        needsend = true;
                        segment.SendReason = Segment.SendReasonEnum.FIRST;
                        segment.rto = Context.RTOHelper.RTO;
                        segment.resendts = Context.Current + segment.rto +
                            (NoDelayOption ? 0 : Context.RTOHelper.RTO >> 3);
                    }
                    else if (_itimediff(Context.Current, segment.resendts) >= 0)
                    {
                        needsend = true;
                        segment.SendReason = Segment.SendReasonEnum.LOST;
                        segment.cmd = IKCP_CMD_REPUSH;
                        _xmit_dbg++;
                        segment.LastRTO = segment.rto;
                        segment.rto += NoDelayOption ? Context.RTOHelper.RTO / 2 : Context.RTOHelper.RTO;
                        segment.resendts = Context.Current + segment.rto;
                        lostHappened = true;
                    }
                    else if (segment.fastack >= resent)
                    {
                        needsend = true;
                        segment.SendReason = Segment.SendReasonEnum.FASTACK;
                        segment.cmd = IKCP_CMD_REPUSH;
                        segment.fastack = 0;
                        segment.resendts = Context.Current + segment.rto;
                        quickResendHappend = true;
                    }

                    if (needsend)
                    {
                        _NextFlushTimeDirty = true;
                        segment.xmit++;
                        segment.ts = Context.Current;
                        segment.RecvQueueFreeSlots = Context.InputHelper.ReceiveQueueFreeSlots;
                        segment.una = Context.InputHelper.ReceiveNext;
                        segment.Output();

                        if (segment.xmit >= IKCP_DEADLINK)
                        {   //目前没有什么意义。
                            _state = -1;
                        }
                    }
                }

                Context.CogWinHeler.UpdateCogWinInFlush(quickResendHappend, lostHappened, resent);
            }

            public void TryFlush()
            {
                TryMoveSendSegFromQueueToBuf();
                TryOutputSendBuffer();
            }

            /// <summary> 下一次应该Flush的时刻 </summary>
            public UInt32 NextFlushTime
            {
                get
                {
                    if (_NextFlushTimeDirty)
                    {
                        _NextFlushTime = UInt32.MaxValue;
                        foreach (var seg in _snd_buf)
                        {
                            if (seg.resendts < _NextFlushTime)
                                _NextFlushTime = seg.resendts;
                        }
                        if (!_SendBufferFull)
                        {
                            foreach (var seg in _snd_queue)
                            {
                                if (seg.resendts < _NextFlushTime)
                                    _NextFlushTime = seg.resendts;
                            }
                        }
                    }

                    return _NextFlushTime;
                }
            }
            private UInt32 _NextFlushTime = UInt32.MaxValue;
            private bool _NextFlushTimeDirty = false;
            public void Dispose()
            {
                foreach (var v in _snd_buf)
                {
                    Context.SegPool.PushSegment(v);
                }
                _snd_buf.Clear();
                foreach (var v in _snd_queue)
                {
                    Context.SegPool.PushSegment(v);
                }
                _snd_queue.Clear();
            }
        }
    }
}