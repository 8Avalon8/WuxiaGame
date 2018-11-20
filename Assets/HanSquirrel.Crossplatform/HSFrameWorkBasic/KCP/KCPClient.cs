using System;
using HSFrameWork.Common;
using HSFrameWork.Net;

namespace HSFrameWork.KCP.Client
{
    public partial class KCPClientFactory
    {
        public static Func<IPlayerLinkClient, bool> NeedTraceFunc { get; set; }
#if HSFRAMEWORK_NET_ABOVE_4_5
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
        public static IPlayerLinkClientSync CreateSync(uint displayName, object state, RecvDataHandler recvData)
        {
            return new KCPClientSyncImpl(displayName, false, 0, state, recvData);
        }

        public static IPlayerLinkClientSync CreateSync(uint displayName, bool traceMe, int sendLogMaxSize, object state, RecvDataHandler recvData)
        {
            return new KCPClientSyncImpl(displayName, traceMe, sendLogMaxSize, state, recvData);
        }

#if HSFRAMEWORK_NET_ABOVE_4_5
        public static IHSNetClientASync CreateASync(uint displayName, object state, RecvDataHandler recvData)
        {
            return new KCPClientASyncImpl(displayName, false, 0, state, recvData);
        }

        public static IHSNetClientASync CreateASync(uint displayName, bool traceMe, int sendLogMaxSize, object state, RecvDataHandler recvData)
        {
            return new KCPClientASyncImpl(displayName, traceMe, sendLogMaxSize, state, recvData);
        }

        public static IHSNetClientASync CreateASync(bool wrapper, uint displayName, object state, RecvDataHandler recvData)
        {
            return wrapper ? KCPClientFactoryAsyncWrapper.CreateAsyncWrapper(displayName, state, recvData) : CreateASync(displayName, state, recvData);
        }

        public static IHSNetClientASync CreateASync(bool wrapper, uint displayName, bool traceMe, int sendLogMaxSize, object state, RecvDataHandler recvData)
        {
            return wrapper ? KCPClientFactoryAsyncWrapper.CreateAsyncWrapper(displayName, state, recvData) : CreateASync(displayName, traceMe, sendLogMaxSize, state, recvData);
        }
            
#endif
    }
}
