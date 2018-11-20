using System;

namespace HSFrameWork.KCP.Common
{
    public class KCPRunReport
    {
        public DateTime ConnectedTime { get; set; }
        public int RunningSeconds { get; set; }

        public int RTTMin { get; set; }
        public int RTTMax { get; set; }
        public int RTTAvg { get; set; }

        public float AppSendMB { get; set; }
        public float SendMB { get; set; }
        public float AppRecvMB { get; set; }
        public float RecvMB { get; set; }

        /// <summary> KB/S </summary>
        public float SendSpeed { get; set; }
        /// <summary> KB/S </summary>
        public float RecvSpeed { get; set; }

        /// <summary> APP数据/总发送数据 </summary>
        public float AppSendRate { get; set; }
        /// <summary> APPTAG/总发送数据 </summary>
        public float AppTagSendRate { get; set; }
        /// <summary> KCPCMD/总发送数据 </summary>
        public float CmdSendRate { get; set; }
        /// <summary> KCPPush包头/总发送数据 </summary>
        public float PushSendRate { get; set; }
        /// <summary> 重发的[AppTag+AppData]/总发送数据 </summary>
        public float AppExRetransSendRate { get; set; }
        /// <summary> UDPHead/总发送数据 </summary>
        public float UDPHeadSendRate { get; set; }

        public void RecalcRates()
        {
            if (SendSize > 0)
            {
                AppSendRate = AppSend * 1.0f / SendSize;
                AppTagSendRate = AppTagSendSize * 1.0f / SendSize;
                CmdSendRate = CmdSendSize * 1.0f / SendSize;
                PushSendRate = PushSendSize * 1.0f / SendSize;
                AppExRetransSendRate = AppExRetransSendSize * 1.0f / SendSize;
                UDPHeadSendRate = UDPHeaderSendSize * 1.0f / SendSize;
            }

            if (RecvSize > 0)
            {
                AppRecvRate = AppRecv * 1.0f / RecvSize;
                AppTagRecvRate = AppTagRecvSize * 1.0f / RecvSize;
                CmdRecvRate = CmdRecvSize * 1.0f / RecvSize;
                PushRecvRate = PushRecvSize * 1.0f / RecvSize;
                AppExRetransRecvRate = AppExRetransRecvSize * 1.0f / RecvSize;
                UDPHeadRecvRate = UDPHeaderRecvSize * 1.0f / RecvSize;
            }
        }


        /// <summary> APP数据/总接收数据 </summary>
        public float AppRecvRate { get; set; }
        /// <summary> APPTAG/总接收数据 </summary>
        public float AppTagRecvRate { get; set; }
        /// <summary> KCPCMD/总接收数据 </summary>
        public float CmdRecvRate { get; set; }
        /// <summary> KCPPush包头/总接收数据 </summary>
        public float PushRecvRate { get; set; }
        /// <summary> 重发的[AppTag+AppData]/总接收数据 </summary>
        public float AppExRetransRecvRate { get; set; }
        /// <summary> UDPHead/总接收数据 </summary>
        public float UDPHeadRecvRate { get; set; }

        /// <summary> 应用数据的发送次数 </summary>
        public int AppSendCount { get; set; }
        /// <summary> 应用数据的发送大小 </summary>
        public int AppSend { get; set; }
        /// <summary> UDP数据发送次数 </summary>
        public int SendCount { get; set; }
        /// <summary> UDP数据发送大小 </summary>
        public int SendSize { get; set; }
        /// <summary> App数据头发送次数，应该等于 AppSendCount</summary>
        public int AppTagSendCount { get; set; }
        /// <summary> App数据头发送大小 </summary>
        public int AppTagSendSize { get; set; }
        /// <summary> [AppHead+AppData] 发送大小，等于 AppTagSendSize + AppSend </summary>
        public int AppExSendSize { get; set; }
        /// <summary> 纯粹的KCPCMD（没有数据）发送次数 </summary>
        public int CmdSendCount { get; set; }
        /// <summary> 纯粹的KCPCMD（没有数据）发送大小 </summary>
        public int CmdSendSize { get; set; }
        /// <summary> 有数据的KCP包头发送次数 </summary>
        public int PushSendCount { get; set; }
        /// <summary> 有数据的KCP包头发送大小 </summary>
        public int PushSendSize { get; set; }
        /// <summary> [AppHead+AppData] 重新发送次数 </summary>
        public int AppExRetransCount { get; set; }
        /// <summary> [AppHead+AppData] 重新发送大小 </summary>
        public int AppExRetransSendSize { get; set; }
        /// <summary> UDP包头 发送次数 = SendCount </summary>
        public int UDPHeaderSendCount { get; set; }
        /// <summary> UDP包头 发送大小 </summary>
        public int UDPHeaderSendSize { get; set; }

        /// <summary> 应用数据的接收次数 </summary>
        public int AppRecvCount { get; set; }
        /// <summary> 应用数据的接收大小 </summary>
        public int AppRecv { get; set; }
        /// <summary> UDP数据接收次数 </summary>
        public int RecvCount { get; set; }
        /// <summary> UDP数据接收大小 </summary>
        public int RecvSize { get; set; }
        /// <summary> App数据头接收次数，应该等于 AppRecvCount</summary>
        public int AppTagRecvCount { get; set; }
        /// <summary> App数据头接收大小 </summary>
        public int AppTagRecvSize { get; set; }
        /// <summary> [AppHead+AppData] 接收大小，等于 AppTagRecvSize + AppRecv </summary>
        public int AppExRecvSize { get; set; }
        /// <summary> 纯粹的KCPCMD（没有数据）接收次数 </summary>
        public int CmdRecvCount { get; set; }
        /// <summary> 纯粹的KCPCMD（没有数据）接收大小 </summary>
        public int CmdRecvSize { get; set; }
        /// <summary> 有数据的KCP包头接收次数 </summary>
        public int PushRecvCount { get; set; }
        /// <summary> 有数据的KCP包头接收大小 </summary>
        public int PushRecvSize { get; set; }
        /// <summary> [AppHead+AppData] 重新接收次数 </summary>
        public int AppExReSendRecvCount { get; set; }
        /// <summary> [AppHead+AppData] 重新接收大小 </summary>
        public int AppExRetransRecvSize { get; set; }
        /// <summary> UDP包头 接收次数 = SendCount </summary>
        public int UDPHeaderRecvCount { get; set; }
        /// <summary> UDP包头 接收大小 </summary>
        public int UDPHeaderRecvSize { get; set; }

        public int Xmit1Count { get; set; }
        public int Xmit2Count { get; set; }
        public int Xmit3Count { get; set; }
        public int Xmit4Count { get; set; }
        public int XmitOtherCount { get; set; }
        public int XMitMax { get; set; }
    }

    public static class KCPRunReportFiller
    {

        public static T Fill<T>(this T report, PlayerLinkDebugInfoKCP dbgInfo, KCPLib kcpLib) where T : KCPRunReport
        {
            int min, max, avg, last, smoothed, var;
            kcpLib.GetRttVerboseInfo(out min, out max, out avg, out last, out smoothed, out var);

            int xmit1 = 0, xmit2 = 0, xmit3 = 0, xmit4 = 0, xmitOther = 0, xmitMax = 0;
            if (kcpLib.SendSegLog != null)
            {
                foreach (var sendlog in kcpLib.SendSegLog)
                {
                    switch (sendlog.TransmitCount)
                    {
                        case 1:
                            xmit1++;
                            break;
                        case 2:
                            xmit2++;
                            break;
                        case 3:
                            xmit3++;
                            break;
                        case 4:
                            xmit4++;
                            break;
                        default:
                            xmitOther++;
                            break;
                    }
                    if (xmitMax < sendlog.TransmitCount)
                        xmitMax = (int)sendlog.TransmitCount;
                }
            }

            report.ConnectedTime = dbgInfo.ConnectedTime.ToLocalTime();
            report.RunningSeconds = dbgInfo.RunningSeconds;
            report.RTTMin = min;
            report.RTTMax = max;
            report.RTTAvg = avg;

            if (!kcpLib.ServerSide)
            {
                report.SendMB = dbgInfo.RAWSendBandwidth.SizeAll / (1024 * 1024 * 1.0f);
                report.RecvMB = dbgInfo.RAWRecvBandwidth.SizeAll / (1024 * 1024 * 1.0f);
                report.AppSendMB = dbgInfo.AppData.Send.Size / (1024 * 1024 * 1.0f);
                report.AppRecvMB = dbgInfo.AppData.Recv.Size / (1024 * 1024 * 1.0f);
                report.SendSpeed = dbgInfo.RunningSeconds == 0 ? 0 : dbgInfo.RAWSendBandwidth.SizeAll / 1024.0f / dbgInfo.RunningSeconds;
                report.RecvSpeed = dbgInfo.RunningSeconds == 0 ? 0 : dbgInfo.RAWRecvBandwidth.SizeAll / 1024.0f / dbgInfo.RunningSeconds;

                report.AppSendCount = dbgInfo.AppData.Send.Count;
                report.AppSend = dbgInfo.AppData.Send.Size;
                report.SendCount = dbgInfo.RAWSendBandwidth.CountAll;
                report.SendSize = dbgInfo.RAWSendBandwidth.SizeAll;
                report.AppRecvCount = dbgInfo.AppData.Recv.Count;
                report.AppRecv = dbgInfo.AppData.Recv.Size;
                report.RecvCount = dbgInfo.RAWRecvBandwidth.CountAll;
                report.RecvSize = dbgInfo.RAWRecvBandwidth.SizeAll;

                report.AppTagSendCount = dbgInfo.KCPUpHeader.Send.Count;
                report.AppTagSendSize = dbgInfo.KCPUpHeader.Send.Size;
                report.AppExSendSize = dbgInfo.KCPUpHeader.Send.Size + dbgInfo.AppData.Send.Size;
                report.CmdSendCount = kcpLib.KCPHeaderWithoutPayload.Send.Count;
                report.CmdSendSize = kcpLib.KCPHeaderWithoutPayload.Send.Size;
                report.PushSendCount = kcpLib.KCPHeaderWithPayload.Send.Count;
                report.PushSendSize = kcpLib.KCPHeaderWithPayload.Send.Size;
                report.AppExRetransCount = kcpLib.KCPPayloadRetrans.Send.Count;
                report.AppExRetransSendSize = kcpLib.KCPPayloadRetrans.Send.Size;
                report.UDPHeaderSendCount = dbgInfo.UdpHeader.Send.Count;
                report.UDPHeaderSendSize = dbgInfo.UdpHeader.Send.Size;

                report.AppTagRecvCount = dbgInfo.KCPUpHeader.Recv.Count;
                report.AppTagRecvSize = dbgInfo.KCPUpHeader.Recv.Size;
                report.AppExRecvSize = dbgInfo.KCPUpHeader.Recv.Size + dbgInfo.AppData.Recv.Size;
                report.CmdRecvCount = kcpLib.KCPHeaderWithoutPayload.Recv.Count;
                report.CmdRecvSize = kcpLib.KCPHeaderWithoutPayload.Recv.Size;
                report.PushRecvCount = kcpLib.KCPHeaderWithPayload.Recv.Count;
                report.PushRecvSize = kcpLib.KCPHeaderWithPayload.Recv.Size;
                report.AppExReSendRecvCount = kcpLib.KCPPayloadRetrans.Recv.Count;
                report.AppExRetransRecvSize = kcpLib.KCPPayloadRetrans.Recv.Size;
                report.UDPHeaderRecvCount = dbgInfo.UdpHeader.Recv.Count;
                report.UDPHeaderRecvSize = dbgInfo.UdpHeader.Recv.Size;
            }
            else
            {
                report.RecvMB = dbgInfo.RAWSendBandwidth.SizeAll / (1024 * 1024 * 1.0f);
                report.SendMB = dbgInfo.RAWRecvBandwidth.SizeAll / (1024 * 1024 * 1.0f);
                report.AppRecvMB = dbgInfo.AppData.Send.Size / (1024 * 1024 * 1.0f);
                report.AppSendMB = dbgInfo.AppData.Recv.Size / (1024 * 1024 * 1.0f);
                report.RecvSpeed = dbgInfo.RunningSeconds == 0 ? 0 : dbgInfo.RAWSendBandwidth.SizeAll / 1024.0f / dbgInfo.RunningSeconds;
                report.SendSpeed = dbgInfo.RunningSeconds == 0 ? 0 : dbgInfo.RAWRecvBandwidth.SizeAll / 1024.0f / dbgInfo.RunningSeconds;

                report.AppRecvCount = dbgInfo.AppData.Send.Count;
                report.AppRecv = dbgInfo.AppData.Send.Size;
                report.RecvCount = dbgInfo.RAWSendBandwidth.CountAll;
                report.RecvSize = dbgInfo.RAWSendBandwidth.SizeAll;

                report.AppSendCount = dbgInfo.AppData.Recv.Count;
                report.AppSend = dbgInfo.AppData.Recv.Size;
                report.SendCount = dbgInfo.RAWRecvBandwidth.CountAll;
                report.SendSize = dbgInfo.RAWRecvBandwidth.SizeAll;

                report.AppTagRecvCount = dbgInfo.KCPUpHeader.Send.Count;
                report.AppTagRecvSize = dbgInfo.KCPUpHeader.Send.Size;
                report.AppExRecvSize = dbgInfo.KCPUpHeader.Send.Size + dbgInfo.AppData.Send.Size;
                report.CmdRecvCount = kcpLib.KCPHeaderWithoutPayload.Send.Count;
                report.CmdRecvSize = kcpLib.KCPHeaderWithoutPayload.Send.Size;
                report.PushRecvCount = kcpLib.KCPHeaderWithPayload.Send.Count;
                report.PushRecvSize = kcpLib.KCPHeaderWithPayload.Send.Size;
                report.AppExReSendRecvCount = kcpLib.KCPPayloadRetrans.Send.Count;
                report.AppExRetransRecvSize = kcpLib.KCPPayloadRetrans.Send.Size;
                report.UDPHeaderRecvCount = dbgInfo.UdpHeader.Send.Count;
                report.UDPHeaderRecvSize = dbgInfo.UdpHeader.Send.Size;

                report.AppTagSendCount = dbgInfo.KCPUpHeader.Recv.Count;
                report.AppTagSendSize = dbgInfo.KCPUpHeader.Recv.Size;
                report.AppExSendSize = dbgInfo.KCPUpHeader.Recv.Size + dbgInfo.AppData.Recv.Size;
                report.CmdSendCount = kcpLib.KCPHeaderWithoutPayload.Recv.Count;
                report.CmdSendSize = kcpLib.KCPHeaderWithoutPayload.Recv.Size;
                report.PushSendCount = kcpLib.KCPHeaderWithPayload.Recv.Count;
                report.PushSendSize = kcpLib.KCPHeaderWithPayload.Recv.Size;
                report.AppExRetransCount = kcpLib.KCPPayloadRetrans.Recv.Count;
                report.AppExRetransSendSize = kcpLib.KCPPayloadRetrans.Recv.Size;
                report.UDPHeaderSendCount = dbgInfo.UdpHeader.Recv.Count;
                report.UDPHeaderSendSize = dbgInfo.UdpHeader.Recv.Size;
            }

            report.Xmit1Count = xmit1;
            report.Xmit2Count = xmit2;
            report.Xmit3Count = xmit3;
            report.Xmit4Count = xmit4;
            report.XmitOtherCount = xmitOther;
            report.XMitMax = xmitMax;

            report.RecalcRates();
            return report;
        }
    }
}
