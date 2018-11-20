#if HSFRAMEWORK_NET_ABOVE_4_5
using System;
using System.Net.Sockets;
using HSFrameWork.KCP.Common;
using System.Threading;
using System.Threading.Tasks;
using GLibEx;
using HSFrameWork.Common;
using GLib;
using System.Runtime.CompilerServices;
using HSFrameWork.Net;
using HSFrameWork.Common.Inner;

namespace HSFrameWork.KCP.Client
{
    public partial class KCPClientFactory
    {
        /// <summary>
        /// Windows
        /// </summary>
        private class KCPClientASyncImpl : KCPClientAbstract, IHSNetClientASync
        {
            protected override String TypeDisplay { get { return "异步"; } }

            public KCPClientASyncImpl(uint displayName, bool traceMe, int sendLogMaxSize, object state, RecvDataHandler recvData)
                : base(displayName, traceMe, sendLogMaxSize, state, recvData) { }

            ~KCPClientASyncImpl()
            {
                CleanSendQueue();
            }

            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            private void CleanSendQueue()
            {
                m_SendQueue.Switch();
                while (!m_SendQueue.Empty())
                {
                    m_SendQueue.Dequeue().Dispose();
                }
            }

            #region 父类虚函数
            protected override void DoSendToKCP_MT(SmartBufferEx<KCPDataDirection> sb, bool backGround)
            {
                m_SendQueue.Enqueue(sb, backGround);//在发送过程中出现的错误，会在主Task中截获。
            }

            protected override bool DoSendToKCP_APPTHREAD(SmartBufferEx<KCPDataDirection> sb, bool backGround)
            {
                m_SendQueue.Enqueue(sb, backGround);
                return true; //在发送过程中出现的错误，会在主Task中截获。
                //GGTODO: 如果已经被关闭了，如何销毁。如何和主线程同步。
            }
            #endregion

            #region 接口实现函数
            public Task MainTask { get; private set; }

            public Task RunAsync(byte[] serverEndPoint, byte localSessionId, byte[] handShakeData, Func<byte[], int, int, bool> hv)
            {
                return MainTask = RunAsyncInner(serverEndPoint, localSessionId, handShakeData, hv).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        ExitException exitException;
                        var exps = t.Exception.Flatten().InnerExceptions;
                        if (exps.Count == 1 && (exitException = exps[0] as ExitException) != null)
                        {
                            InvokeExitEvent_MT(exitException.Status, exitException.Message);
                        }
                        else
                        {
                            foreach (var exp in exps)
                                _Logger0.Error(exp, "KCPClientASyncImpl 运行出现异常");
                            InvokeExitEvent_MT(PlayerLinkStatus.InnerException, "KCPClientASyncImpl 出现异常。");
                        }
                    }
                    else
                    {
                        Mini.ThrowIfFalse(Status.IsDead(), "George编程错误。KCPClientASyncImpl.MainTask 退出然而Status没有正确设置。");
                    }

                    CloseUDP();
                    m_Kcp?.Dispose();

                    CleanSendQueue();

                    if (Status != PlayerLinkStatus.Disconnected)
                        _Logger0.Warn("▲ {0} KCPC{1} 结束运行：[{2}] {3} {4}", TypeDisplay, DisplayName, Status, ExitMessage, VerboseStatus);
                    else
                        _Logger0.Debug("▲ {0} KCPC{1} 结束运行：[{2}] {3} {4}", TypeDisplay, DisplayName, Status, ExitMessage, VerboseStatus);

                });
            }
            #endregion

            private bool _DisableOnDirty = false;
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            protected override void OnKCPLibDirty()
            {
                if (!_DisableOnDirty)
                    _UpdateEvent.Set();
                /*
                 * 在SuspendFlush用Signal实现后，所有对KCPLib的操作都在主任务里面了，
                 * 操作前都会_DisableOnDirty，因此_UpdateEvent.Set()永远不会执行到。
                 * 因此这个函数对可以不实现。保留完整性放在这里。
                 */
            }

            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            private void ResetUpdateEventTaskIf()
            {
                if (_UpdateEventTask.IsCompleted)
                    _UpdateEventTask = null;
            }

            private AsyncAutoResetEvent _UpdateEvent = new AsyncAutoResetEvent(false);
            private Task _SendRecvEventTask, _UpdateEventTask;
            /// <summary>
            /// 内部或者外部终止任务时需要。
            /// </summary>
            private SwitchQueue<SmartBufferEx<KCPDataDirection>> m_SendQueue = new SwitchQueue<SmartBufferEx<KCPDataDirection>>(16);
            private async Task RunAsyncInner(byte[] serverEndPoint, byte localSessionId, byte[] handShakeData, Func<byte[], int, int, bool> hv)
            {
                Setup(serverEndPoint, localSessionId, handShakeData, hv);

                m_LastRecvTimestamp = SharedG.Clock.Elapsed;
                Task<UdpReceiveResult> udpTask = null;
                Task[] waitTasks = new Task[4];
                InvokeEvent_MT(PlayerLinkStatus.InConnect, false);

                SendHandShake1_MT();

                TaskUtils.DelayAgent delayAgent = new TaskUtils.DelayAgent();
                while (true)
                {
                    _LoopNo++;

                    ReceiveAsync_MT(ref udpTask);

                    if (_SendRecvEventTask == null)
                        _SendRecvEventTask = m_SendQueue.EnqueueEvent.WaitAsync();

                    if (_UpdateEventTask == null)
                        _UpdateEventTask = _UpdateEvent.WaitAsync();

                    int timeOut = CalcUpdateTimeout();
                    if (timeOut > 0)
                    {
                        waitTasks[0] = udpTask;
                        waitTasks[1] = _SendRecvEventTask;
                        waitTasks[2] = delayAgent.Delay(timeOut);
                        waitTasks[3] = _UpdateEventTask;

                        long sleepTime = 0;
                        if (Logger3Tracable())
                        {
                            sleepTime = SharedG.LClock;
                            _Logger3.Trace("\t\t\t\t\tKCPC{0} ZZZ {2}MS", DisplayName, _LoopNo, timeOut);
                        }

                        var t = await Task.WhenAny(waitTasks);
                        delayAgent.Reset();
                        if (t == waitTasks[0])
                        {
                            if (Logger3Tracable()) _Logger3.Trace("\t\t\t\t\tKCPC{0} LOOP{1} 收数据", DisplayName, _LoopNo);
                        }
                        else if (t == waitTasks[1])
                        {
                            if (Logger3Tracable()) _Logger3.Trace("\t\t\t\t\tKCPC{0} LOOP{1} 发数据", DisplayName, _LoopNo);
                        }
                        else if (t == waitTasks[2])
                        {
                            if (Status == PlayerLinkStatus.Closing2 && m_Kcp.Idle() && (m_Kcp.IClock - _Closing2Time) >= KCPGlobalOptions.ExitWaitAfterClosing2)
                            {
                                InvokeExitEvent_MT(PlayerLinkStatus.Disconnected, "正常退出");
                                return;
                            }

                            if (Logger3Tracable()) _Logger3.Trace("\t\t\t\t\tKCPC{0} LOOP{1} 自然醒 {2}MS", DisplayName, _LoopNo, SharedG.LClock - sleepTime);
                        }
                        else
                        {
                            if (Logger3Tracable()) _Logger3.Trace("\t\t\t\t\tKCPC{0} LOOP{1} 被叫醒刷新 {2}MS", DisplayName, _LoopNo, SharedG.LClock - sleepTime);
                        }
                    }

                    if (!CTaskCore_MT(ref udpTask))
                        return;
                }
            }

            private void ReceiveAsync_MT(ref Task<UdpReceiveResult> udpTask)
            {
                try
                {
                    if (udpTask == null)
                    {
                        if (KCPGlobalOptions.SimuUdpReceiveExceptionClient)
                        {
                            KCPGlobalOptions.SimuUdpReceiveExceptionClient = false;
                            throw new Exception("冰岛");
                        }
                        udpTask = m_UdpClient.ReceiveAsync(); //这里就可能会发生异常；
                    }
                }
                catch (Exception e)
                {
                    _Logger0.Warn(e, "ReceiveAsync发生异常");
                    throw new ExitException(PlayerLinkStatus.ReceiveFail, "ReceiveAsync发生异常", e);
                }
            }

            private long _WakeUpTime;

            private bool CTaskCore_MT(ref Task<UdpReceiveResult> udpTask)
            {
                _WakeUpTime = SharedG.Clock.Elapsed.Ticks;
                DebugInfo.LastWakeupTime = DateTime.UtcNow;

                if (KCPGlobalOptions.SimuUdpReceiveExceptionClient)
                {
                    KCPGlobalOptions.SimuUdpReceiveExceptionClient = false;
                    udpTask = TaskExt.FromException<UdpReceiveResult>(new Exception("冰岛"));
                }

                //_Logger0.Trace("SuspendFlush 1 CTaskCore_MT");
                _DisableOnDirty = true;
                m_Kcp.SuspendFlush = true;
                if (udpTask.Status == TaskStatus.RanToCompletion)
                {   //有数据
                    //if (Logger3Tracable()) _Logger3.Trace("\t\t\t\t\tKCPC{0} LOOP{1} UDP Received", DisplayName, _LoopNo);
                    var data = udpTask.Result.Buffer;

                    udpTask = null;
                    ReceiveAsync_MT(ref udpTask);
                    ProcessReceivedUDP_MT(data);
                }
                else if (udpTask.IsFaulted)
                {
                    _Logger0.Error(udpTask.Exception, "UDPCLient 发生异常");
                    throw new ExitException(PlayerLinkStatus.ReceiveFail, "UDPCLient 发生异常", udpTask.Exception);
                }

                ProcessSendQueue_MT();

                if (SharedG.Clock.Elapsed - m_LastRecvTimestamp > KCPGlobalOptions.MaxTimeNoData)
                {
                    InvokeExitEvent_MT(PlayerLinkStatus.TimedOut, "超时"); //表示超时
                    return false; //表示超时
                }
                m_Kcp.SuspendFlush = false;
                //_Logger0.Trace("SuspendFlush 0 CTaskCore_MT");

                ResetUpdateEventTaskIf();
                _DisableOnDirty = false;
                SafeUpdata_MT(_WakeUpTime);

                return true;
            }

            /// <summary>
            /// 仅仅在主任务里面调用
            /// </summary>
            private void ProcessSendQueue_MT()
            {
                if (_SendRecvEventTask.IsCompleted)
                    _SendRecvEventTask = null;

                m_SendQueue.Switch();
                bool sent = !m_SendQueue.Empty();
                while (!m_SendQueue.Empty())
                {
                    var sb = m_SendQueue.Dequeue();
                    if (sb.State == KCPDataDirection.SIGNAL)
                    {
                        TryProcessSignal_MT(sb);
                        continue;
                    }

                    DebugInfoKCP.AppData.Send.LastSize = sb.Size - DataPackUtils.KCPUPDATA_PACK_ADDED_SIZE;
                    DebugInfoKCP.KCPUpHeader.Send.LastSize = DataPackUtils.KCPUPDATA_PACK_ADDED_SIZE;
                    m_Kcp.Send(sb.Data, sb.Offset, sb.Size, BinOp.DecodeUShort(sb.Data, DataPackUtils.APP_DATA_SEQ_OFFSET));
                    sb.Dispose();
                }
            }
        }
    }
}

#endif
