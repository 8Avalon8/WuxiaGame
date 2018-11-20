using System;
using HSFrameWork.Common;
using HSFrameWork.KCP.Common;
using System.Threading;
using GLib;
using HSFrameWork.Net;
using HSFrameWork.Common.Inner;

#if HSFRAMEWORK_NET_ABOVE_4_5
using System.Runtime.CompilerServices;
#endif

namespace HSFrameWork.KCP.Client
{
    public partial class KCPClientFactory
    {
        /* 线程关系：
        * 主线程：负责定期 Update KCP。
        * UDP接收回调必定是某个系统线程。
        * Send可能来自主线程、UDP接收回调、或者其他线程。
        * 给应用的数据回调必定来自于上面的某个线程。
        * 
        * 问题1：UDP接收回调直接操作KCP或者是把数据放入消息队列，让主线程处理？
        * 问题2：Send直接操作KCP还是把数据放入消息队列，让主线程处理？
        * 
        * http://jonskeet.uk/csharp/threads/waithandles.shtml
        * WaitHandle的损耗要大于Lock，因此我们选择使用Lock，让UDP回调、Send和主线程都直接操作KCP。
        * 这样可以保证Send和Receive的及时性。
        */

        public class KCPClientSyncImpl : KCPClientAbstract, IPlayerLinkClientSync
        {
            protected override String TypeDisplay { get { return "同步"; } }

            private object _LockObj = new object();

            /// <summary>
            /// 目前是否在主任务工作。
            /// </summary>
            private volatile bool _LockedInMT = false;
            public KCPClientSyncImpl(uint displayName, bool traceMe, int sendLogMaxSize, object state, RecvDataHandler recvData)
                : base(displayName, traceMe, sendLogMaxSize, state, recvData)
            {
            }

            public void Connect(byte[] serverEndPoint, byte localSessionId, byte[] handShakeData,
                                Func<byte[], int, int, bool> hv,
                                Action<IPlayerLinkClient, PlayerLinkStatus> onThreadExit)
            {
                new Thread(() =>
                {
                    if (Logger0Tracable())
                        _Logger0.Trace("▼ C{0} updated thread started. ", DisplayName);
                    try
                    {
                        Setup(serverEndPoint, localSessionId, handShakeData, hv);
                        UpdateThread_MT();
                        Mini.ThrowIfFalse(Status.IsDead(), "George编程错误。KCPClientASyncImpl.MainTask 退出然而Status没有正确设置。");
                    }
                    catch (Exception e)
                    {
                        lock (_LockObj)
                        {
                            if (Status.IsDownAlive() || Status == PlayerLinkStatus.NULL)
                                ProcessException_MT(e, "KCPClientSyncImpl.UpdateThread出现异常。");
                        }
                    }

                    CloseUDP();
                    if (m_Kcp != null)
                        m_Kcp.Dispose();

                    if (Logger0Tracable())
                        _Logger0.Trace("▲ C{0} updated thread stopped. ", DisplayName);

                    if (Status != PlayerLinkStatus.Disconnected)
                        _Logger0.Warn("▲ {0} KCPC{1} 结束运行：[{2}] {3} {4}", TypeDisplay, DisplayName, Status, ExitMessage, VerboseStatus);
                    else
                        _Logger0.Debug("▲ {0} KCPC{1} 结束运行：[{2}] {3} {4}", TypeDisplay, DisplayName, Status, ExitMessage, VerboseStatus);

                    _Logger0.Debug(KCPLibEx.SharedBytePool.DebugString);

                    if (onThreadExit != null)
                    {
                        onThreadExit(this, Status);
                    }
                }).Start();
            }

            private void ReceiveCallback_SYSThread(IAsyncResult ar)
            {
                var udpClient = m_UdpClient;
                if (udpClient == null)
                    return;

                byte[] data;
                try
                {
                    if (KCPGlobalOptions.SimuUdpReceiveExceptionClient)
                    {
                        KCPGlobalOptions.SimuUdpReceiveExceptionClient = false;
                        throw new Exception("冰岛");
                    }
                    data = udpClient.EndReceive(ar, ref mIPEndPoint);
                    udpClient.BeginReceive(ReceiveCallback_SYSThread, this);
                }
                catch (Exception e)
                {
                    //可能发生情况：在接收回调执行期间Updata线程执行CloseForcefully将UDP关闭了。
                    //这样在BeginReceive就会出现异常。
                    if (m_UdpClient != null)
                    {
                        _Logger0.Warn(e, "KCPC{0} UDP接收异常", DisplayName);
                        DoSendToKCP_APPTHREAD(KCPSigals.ReceiveFailSignal, false);
                    }
                    return;
                }

                //GGTODO: 何时是null？
                if (null != data)
                {
                    lock (_LockObj)
                    {
                        //可能发生情况：在BeginReceive和lock之间Update线程将连接关闭。
                        if (Status.IsDownAlive())
                        {
                            try
                            {
                                _LockedInMT = true;
                                m_Kcp.SuspendFlush = true;
                                ProcessReceivedUDP_MT(data);
                                m_Kcp.SuspendFlush = false;
                            }
                            catch (Exception ex)
                            {
                                ProcessException_MT(ex, "ReceiveCallback_SYSThread 出现异常。");
                            }
                            finally
                            {
                                _LockedInMT = false;
                            }
                        }
                        else
                        {
                        }
                    }
                }
            }

            private bool _DisableOnDirty = false;
            protected override void OnKCPLibDirty()
            {
                if (!_DisableOnDirty)
                    _UpdateThreadWakeUpEvent.Set();
            }

            private TimeSpan _HandShakeSendTime;
            private AutoResetEvent _UpdateThreadWakeUpEvent = new AutoResetEvent(false);
            private void UpdateThread_MT()
            {
                m_LastRecvTimestamp = _HandShakeSendTime = SharedG.Clock.Elapsed;
                lock (_LockObj)
                {
                    InvokeEvent_MT(PlayerLinkStatus.InConnect, false);
                    try
                    {
                        if (KCPGlobalOptions.SimuUdpReceiveExceptionClient)
                        {
                            KCPGlobalOptions.SimuUdpReceiveExceptionClient = false;
                            throw new Exception("天涯明月刀");
                        }
                        m_UdpClient.BeginReceive(ReceiveCallback_SYSThread, this);
                    }
                    catch (Exception e)
                    {
                        _Logger0.Error(e, "BeginReceive异常");
                        lock (_LockObj)
                            InvokeExitEvent_MT(PlayerLinkStatus.ReceiveFail, "BeginReceive异常");
                        return;
                    }
                    SendHandShake1_MT();
                }

                //握手成功了。
                while (true)
                {
                    bool forcedWakeup = false;
                    int timeOut = CalcUpdateTimeout();
                    long sleepTime = 0;
                    if (timeOut > 0)
                    {
                        if (Logger3Tracable())
                        {
                            sleepTime = SharedG.LClock;
                            _Logger3.Trace("\t\t\t\t\tKCPC{0} ZZZZ {2}MS", DisplayName, _LoopNo, timeOut);
                        }
                        forcedWakeup = _UpdateThreadWakeUpEvent.WaitOne(timeOut);
                    }

                    var wakeUpTime = SharedG.Clock.Elapsed.Ticks;
                    _LoopNo++;

                    if (forcedWakeup)
                    {
                        if (Logger3Tracable()) _Logger3.Trace("\t\t\t\t\tKCPC{0} LOOP{1} C被唤醒", DisplayName, _LoopNo);
                    }
                    else
                    {
                        if (Status == PlayerLinkStatus.Closing2 && m_Kcp.Idle())
                        {
                            InvokeExitEvent_MT(PlayerLinkStatus.Disconnected, "正常退出");
                            return;
                        }

                        if (Logger3Tracable()) _Logger3.Trace("\t\t\t\t\tKCPC{0} LOOP{1} C醒来 {2}MS", DisplayName, _LoopNo, timeOut <= 0 ? 0 : SharedG.LClock - sleepTime);
                    }

                    lock (_LockObj)
                    {
                        _LockedInMT = true;
                        try
                        {
                            if (!Status.IsDownAlive())
                                return;

                            //DebugInfo.LastWakeupTime = DateTime.UtcNow;
                            if (SharedG.Clock.Elapsed - m_LastRecvTimestamp > KCPGlobalOptions.MaxTimeNoData)
                            {
                                InvokeExitEvent_MT(PlayerLinkStatus.TimedOut, "超时"); //表示超时
                                return;
                            }

                            SafeUpdata_MT(wakeUpTime);
                        }
                        catch (Exception e)
                        {
                            ProcessException_MT(e, "UpdateThread_MT 出现异常");
                        }
                        finally
                        {
                            _LockedInMT = false;
                        }
                    }
                }
            }

            public bool SimuSendInnerException = false;
            private void DoSendInner_Locked(SmartBufferEx<KCPDataDirection> sb)
            {
                Mini.ThrowIfFalse(Status.IsDownAlive(), "George编程错误。DoSendInner_Locked在状态不对的时候进入。");

                if (sb.State == KCPDataDirection.SIGNAL)
                {
                    TryProcessSignal_MT(sb);
                    return;
                }

                using (sb)
                {
                    if (SimuSendInnerException)
                        throw new Exception("仙履奇缘");

                    DebugInfoKCP.AppData.Send.LastSize = sb.Size - DataPackUtils.KCPUPDATA_PACK_ADDED_SIZE;
                    DebugInfoKCP.KCPUpHeader.Send.LastSize = DataPackUtils.KCPUPDATA_PACK_ADDED_SIZE;
                    m_Kcp.Send(sb.Data, sb.Offset, sb.Size, BinOp.DecodeUShort(sb.Data, DataPackUtils.APP_DATA_SEQ_OFFSET));
                }
            }

#if HSFRAMEWORK_NET_ABOVE_4_5
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
            protected override bool DoSendToKCP_APPTHREAD(SmartBufferEx<KCPDataDirection> sb, bool backGround)
            {
                lock (_LockObj)
                {
                    if (Status == PlayerLinkStatus.Connected || (sb.State == KCPDataDirection.SIGNAL && Status.IsDownAlive()))
                        return DoSendToKCPInner_AppThread_Locked(sb);
                    else
                        return false;
                }
            }

            private bool DoSendToKCPInner_AppThread_Locked(SmartBufferEx<KCPDataDirection> sb)
            {
                try
                {
                    DoSendInner_Locked(sb);
                    return true;
                }
                catch (Exception ex)
                {
                    ProcessException_MT(ex, "DoSendToKCP_SENDTHREAD 出现异常");
                    return false;
                }
            }

            private void ProcessException_MT(Exception e, string message)
            {
                ExitException exitException = e as ExitException;
                if (exitException != null)
                {
                    //如果是NULL，则表示已经在其他线程处理过结束消息。
                    if (exitException.Status != PlayerLinkStatus.NULL)
                        InvokeExitEvent_MT(exitException.Status, exitException.Message);
                }
                else
                {
                    _Logger0.Error(e, message);
                    InvokeExitEvent_MT(PlayerLinkStatus.InnerException, message);
                }
                _UpdateThreadWakeUpEvent.Set(); //如果是在非UpdateThread处理的，则需要激活让其退出。
            }

            protected override void DoSendToKCP_MT(SmartBufferEx<KCPDataDirection> sb, bool backGround)
            {
                DoSendInner_Locked(sb);
            }
        }
    }
}
