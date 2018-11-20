using GLib;
using System;
using System.Collections.Generic;

namespace HSFrameWork.KCP.Common
{
    public partial class KCPLib : IDisposable
    {
        private class CInputHelper : Helper, IDisposable
        {
            public CInputHelper()
            {
                RemoteReceiveQueueFreeSlots = IKCP_WND_RCV_DEFAULT;
            }

            /// <summary> 接收窗口的上限。在构造的时候设置。缺省为32. </summary>
            public UInt32 RcvWinUpLimit = IKCP_WND_RCV_DEFAULT;

            /// <summary>
            /// 这里比较奇怪地用了 RcvWinUpLimit - _rcv_que.count。
            /// 刚刚开始以为是BUG，后来看了作者的文章。是故意使然。
            /// 整个协议在发送端是把无法发出的都放在snd_queue里面，不会去报错。
            /// 如果对方没有接收而造成了其rec_queue和rec_buffer都满了，则不会去发送了。
            /// 在发送的时候，并不去管对方的rec_buff是否满。
            /// 因此如果RcvWinUpLimit设置比Snd要小，则会出现意外。
            /// </summary>
            public UInt32 ReceiveQueueFreeSlots //ikcp_wnd_unused
            {
                get
                {
                    if (_rcv_queue.Count < RcvWinUpLimit) //RecvQueue
                        return RcvWinUpLimit - (UInt32)_rcv_queue.Count; //RecvQueue
                    return 0;
                }
            }

            /// <summary> 远端可用接收窗口，来源于远端的 ReceiveQueueFreeSlots </summary>
            public UInt32 RemoteReceiveQueueFreeSlots { get; private set; }

            /// <summary> 待接收消息序号 </summary>
            public UInt32 ReceiveNext { get; private set; }

            /// <summary> 接收缓冲区，缓冲底层接收的数据，组装连续以后拷贝到接收队列 </summary>
            private LinkedList<Segment> _rcv_buf = new LinkedList<Segment>();
            /// <summary> 接收队列，接收的连续数据包，上层应用可以直接使用，不能超过接收窗口大小 </summary>
            private LinkedList<Segment> _rcv_queue = new LinkedList<Segment>();

            /// <summary>
            /// 只读：check the size of next message in the recv queue
            /// GGTODO: 将Recv和Peek去掉，修改为直接在TryAddSeg里面回调。
            /// </summary>
            public int PeekSize()
            {
                if (0 == _rcv_queue.Count)
                    return -1; //连续队列是空

                var seq0 = _rcv_queue.First.Value;

                if (0 == seq0.frg)
                {   //连续队列第一个数据块是一个独立的消息
                    return seq0.data.Count;
                }

                if (_rcv_queue.Count < seq0.frg + 1)
                    return -1; //连续队列的数据块还不能组成一个完整的消息

                //将第一个完整消息的各个数据块的长度加起来
                int length = 0;
                foreach (var item in _rcv_queue)
                {
                    length += item.data.Count;
                    if (0 == item.frg)
                        break;
                }
                return length;
            }

            /// <summary>
            /// user/upper level recv: returns size, returns below zero for EAGAIN
            /// GGTODO: 将Recv和Peek去掉，修改为直接在TryAddSeg里面回调。
            /// </summary>
            public int Recv(byte[] buffer)
            {
                if (0 == _rcv_queue.Count)
                    return -1;//连续队列是空

                var peekSize = PeekSize();
                if (0 > peekSize)
                    return -2; //没有完整消息可用

                if (peekSize > buffer.Length)
                    throw new ArgumentException("KCPLib.Recv(buffer的长度{0}小于应该的值{1}".f(buffer.Length, peekSize)); //输入的Buffer太小

                //是否目前接收缓冲区已满，造成发送方无法发送。
                var fast_recover = _rcv_queue.Count >= RcvWinUpLimit; //RecvQueue 用于主动发送 [空余接收窗口]

                // STEP1: 将连续队列的第一个消息拼接出来。
                var msgBufOffset = 0;
                var node = _rcv_queue.First;
                while (node != null)
                {   //GGTODO：简化并添加异常判断。
                    var seg = node.Value;
                    Array.Copy(seg.data.Array, seg.data.Offset, buffer, msgBufOffset, seg.data.Count);
                    msgBufOffset += seg.data.Count;
                    uint frg = seg.frg;
                    var next = node.Next;
                    _rcv_queue.Remove(node);
                    Context.SegPool.PushSegment(seg);
                    node = next;
                    if (0 == frg)
                        break;
                }

                //STEP2: 将下一堆连续的数据包移到连续队列。
                node = _rcv_buf.First;
                while (node != null)
                {
                    Segment seg = node.Value;
                    if (seg.SendSN == ReceiveNext && _rcv_queue.Count < RcvWinUpLimit) //RecvQeue，应该无条件将SEG上移
                    {
                        var tmp = node.Next;
                        _rcv_buf.Remove(node);
                        _rcv_queue.AddLast(node);
                        node = tmp;
                        ReceiveNext++;
                    }
                    else
                    {
                        break;
                    }
                }

                //之前的发送缓冲区已满，现在有了空余，因此通知当前的空余接收SLOT数目。
                if (_rcv_queue.Count < RcvWinUpLimit && fast_recover)// RecvQueue主动发送 [接收空余窗口]
                {
                    // ready to send back IKCP_CMD_WINS in ikcp_flush
                    // tell remote my window size
                    Context.Probehelper.Mask |= IKCP_MASK_ANSWER_REMOTE;
                }

                return msgBufOffset;
            }

            /// <summary>
            /// 将Seg加入buf，然后再试图将数据从buf转移到queue
            /// </summary>
            private void tryAddSeg(Segment newseg)
            {
                var newsn = newseg.SendSN;
                if (_itimediff(newsn, ReceiveNext + RcvWinUpLimit) >= 0 || _itimediff(newsn, ReceiveNext) < 0) //往RcvBUffer插入
                {   //接收窗口不足，抛弃之；老数据，抛弃之。
                    //GGTODO: 这个在调用此函数之前已经判断过了。因此不需要。
                    Context.SegPool.PushSegment(newseg);
                    return;
                }

                //STEP1: 将newSeg插入链表。
                var n = _rcv_buf.Count - 1;
                var repeat = false;

                //从后往前，找到的一个比newsn小的，插入到后面。
                LinkedListNode<Segment> p;
                for (p = _rcv_buf.Last; p != null; p = p.Previous)
                {
                    var seg = p.Value;
                    if (seg.SendSN == newsn)
                    {
                        repeat = true;
                        break;
                    }
                    else if (_itimediff(newsn, seg.SendSN) > 0)
                    {
                        break;
                    }
                }

                if (repeat == false)
                {
                    if (p == null)
                    {
                        _rcv_buf.AddFirst(newseg);
                    }
                    else
                    {
                        _rcv_buf.AddAfter(p, newseg);
                        //原始C#版本为AddBefore，是因为对最原始的 IQUEUE_ADD理解错误。
                    }
                }
                else
                {
                    Context.SegPool.PushSegment(newseg);
                }

                //Step2: 将合格的连续Seg推入 rcv_buf
                var node = _rcv_buf.First;
                while (node != null)
                {
                    var seg = node.Value;
                    var tmp = node.Next;
                    if (seg.SendSN == ReceiveNext && _rcv_queue.Count < RcvWinUpLimit) //RecvQueue：推入
                    {
                        _rcv_buf.Remove(node);
                        _rcv_queue.AddLast(node);
                        ReceiveNext++;
                    }
                    else
                    {
                        break;
                    }
                    node = tmp;
                }
            }

            /// <summary>
            /// data[dataOffset, maxoffset) when you received a low level packet (eg. UDP packet), call it
            /// </summary>
            public int Input(byte[] data, int dataOffset, int maxOffset)
            {
                var old_snd_una = Context.SendHelper.SendUNA;
                if (maxOffset < IKCP_OVERHEAD)
                {
                    throw new DataInvalidException("Input的data小于{0}".f(IKCP_OVERHEAD));
                }

                int offset = dataOffset;
                while (offset < maxOffset)
                {
                    //如果是ACK包，则TS是本地的TS。否则是远程的TS
                    UInt32 ts = 0;

                    //如果是ACK包，则sn是本地的TS。否则是远程的sn
                    UInt32 sn = 0;
                    UInt32 length = 0;
                    UInt32 unaRemote = 0;
                    UInt32 conv_ = 0;

                    UInt16 recvQueueFreeSlotsRemote = 0;

                    byte cmd = 0;
                    byte frg = 0;

                    if (maxOffset - offset < IKCP_OVERHEAD)
                    {
                        throw new DataInvalidException("Input的data尾部小于{0}".f(IKCP_OVERHEAD));
                    }

#if false
                    offset += ikcp_decode32u(data, offset, ref conv_);

                    if (Context.Conv != conv_)
                    {
                        throw new DataInvalidException("Input的conv不是{0}".f(conv_));
                    }
#endif
                    offset += ikcp_decode8u(data, offset, ref cmd);
                    offset += ikcp_decode8u(data, offset, ref frg);
                    offset += ikcp_decode16u(data, offset, ref recvQueueFreeSlotsRemote);
                    offset += ikcp_decode32u(data, offset, ref ts);
                    offset += ikcp_decode32u(data, offset, ref sn);
                    offset += ikcp_decode32u(data, offset, ref unaRemote);
                    offset += ikcp_decode32u(data, offset, ref length);

                    if (maxOffset - offset < length)
                        throw new DataInvalidException("Input的包头指示长度大于{0} buff长度 {1}".f(length, maxOffset - offset));

                    switch (cmd)
                    {
                        case IKCP_CMD_PUSH:
                        case IKCP_CMD_REPUSH:
                        case IKCP_CMD_ACK:
                        case IKCP_CMD_WASK:
                        case IKCP_CMD_WINS:
                            break;
                        default:
                            throw new DataInvalidException("Input的包头的CMD不认识{0}".f(cmd));
                    }

                    var logger = Context.GetLogger();
                    if (logger != null)
                    {
                        if (length > 0)
                        {
                            ushort seq = (maxOffset - offset) >= (DataPackUtils.APP_DATA_SEQ_OFFSET + 2) ?
                                BinOp.DecodeUShort(data, offset + DataPackUtils.APP_DATA_SEQ_OFFSET) :
                                ushort.MaxValue;
                            logger.Trace("{0}{1}[{2:D3}     {3}{4:X4}* sn{5}{6:X8}]",
                                Context.ServerSide ? "      >> LIBSR  S" : "LIBCR <<        C",//0
                                Context.DisplayName, length, //1,2
                                Context.ServerSide ? "appC" : "appS", seq, //3,4
                                Context.ServerSide ? "C" : "S", sn); //5,6
                        }
                        else
                            logger.Trace("{0}{1}[{2}                sn{3}{4:X8}]",
                                Context.ServerSide ? "      >> LIBSR  S" : "LIBCR <<        C",
                                Context.DisplayName, CmdToString(cmd),
                                Context.ServerSide ? "S" : "C", sn);
                    }

                    if (length == 0)
                        Context.KCPHeaderWithoutPayload.Recv.LastSize = IKCP_OVERHEAD;
                    else
                    {
                        Context.KCPHeaderWithPayload.Recv.LastSize = IKCP_OVERHEAD;
                        if (cmd == IKCP_CMD_REPUSH)
                            Context.KCPPayloadRetrans.Recv.LastSize = (int)length;
                    }

                    RemoteReceiveQueueFreeSlots = (UInt32)recvQueueFreeSlotsRemote;
                    Context.SendHelper.RemoveSendSegmentsFromSendBuffer(unaRemote);

                    if (IKCP_CMD_ACK == cmd)
                    {
                        //ACK包的TS是对应的发出的那个包的本地TS。
                        if (_itimediff(Context.Current, ts) >= 0)
                        {
                            Context.RTOHelper.Update(_itimediff(Context.Current, ts));
                        }
                        Context.SendHelper.RemoveSendSegmentByRemoteAckSN(sn);
                    }
                    else if (IKCP_CMD_PUSH == cmd || IKCP_CMD_REPUSH == cmd)
                    {
                        if (_itimediff(sn, ReceiveNext + RcvWinUpLimit) < 0) //RecBuf有空间
                        {
                            //在收到后立刻就会生成ACK包。
                            Context.CmdHelper.AddAck(sn, ts);
                            if (_itimediff(sn, ReceiveNext) >= 0)
                            {  //不是无用的老包
                                var segRecved = Context.SegPool.PopSegment((int)length);
                                //segRecved.conv = conv_;
                                segRecved.cmd = cmd;
                                segRecved.frg = (UInt32)frg;
                                segRecved.RecvQueueFreeSlots = (UInt32)recvQueueFreeSlotsRemote; //先解析裸Buffer，再构造收到的Seg
                                segRecved.ts = ts;
                                segRecved.SendSN = sn;
                                segRecved.una = unaRemote;

                                if (length > 0)
                                {
                                    Array.Copy(data, offset, segRecved.data.Array, segRecved.data.Offset, length);
                                }

                                tryAddSeg(segRecved);
                            }
                        }
                    }
                    else if (IKCP_CMD_WASK == cmd)
                    {
                        // ready to send back IKCP_CMD_WINS in Ikcp_flush
                        // tell remote my window size
                        Context.Probehelper.Mask |= IKCP_MASK_ANSWER_REMOTE;
                    }
                    else if (IKCP_CMD_WINS == cmd)
                    {
                        // do nothing
                    }
                    else
                    {
                        throw new DataInvalidException("Input的包头的CMD不认识{0}".f(cmd));
                    }

                    offset += (int)length;
                }

                Context.CogWinHeler.UpdateCogWinInInput(old_snd_una);
                return 0;
            }

            public void Dispose()
            {
                foreach (var v in _rcv_buf)
                {
                    Context.SegPool.PushSegment(v);
                }
                _rcv_buf.Clear();

                foreach (var v in _rcv_queue)
                {
                    Context.SegPool.PushSegment(v);
                }
                _rcv_queue.Clear();
            }
        }
    }
}
