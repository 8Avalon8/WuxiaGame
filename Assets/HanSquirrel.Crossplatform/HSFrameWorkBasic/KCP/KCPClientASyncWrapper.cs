#if HSFRAMEWORK_NET_ABOVE_4_5
using System;
using HSFrameWork.Common;
using System.Threading.Tasks;
using HSFrameWork.Net;

namespace HSFrameWork.KCP.Client
{
    /// <summary>
    /// UnityEditor/Windows
    /// </summary>
    public class KCPClientFactoryAsyncWrapper
    {
        public static IHSNetClientASync CreateAsyncWrapper(uint displayName, object state, RecvDataHandler recvData)
        {
            return new KCPClientASyncWrapper(displayName, false, 0, state, recvData);
        }

        public static IHSNetClientASync CreateAsyncWrapper(uint displayName, bool traceMe, int sendLogMaxSize, object state, RecvDataHandler recvData)
        {
            return new KCPClientASyncWrapper(displayName, traceMe, sendLogMaxSize, state, recvData);
        }

        private class KCPClientASyncWrapper : KCPClientFactory.KCPClientSyncImpl, IHSNetClientASync
        {
            public KCPClientASyncWrapper(uint displayName, bool traceMe, int sendLogMaxSize, object state, RecvDataHandler recvData)
                : base(displayName, traceMe, sendLogMaxSize, state, recvData) { }

            public Task MainTask { get { return _MainTCS.Task; } }

            private TaskCompletionSource<bool> _MainTCS = new TaskCompletionSource<bool>();
            public Task RunAsync(byte[] serverEndPoint, byte localSessionId, byte[] handShakeData, Func<byte[], int, int, bool> hv)
            {
                Connect(serverEndPoint, localSessionId, handShakeData, hv, delegate { _MainTCS.TrySetResult(true); });
                return _MainTCS.Task;
            }
        }
    }
}
#endif