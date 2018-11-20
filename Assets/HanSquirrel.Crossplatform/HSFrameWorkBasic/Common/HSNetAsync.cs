#if HSFRAMEWORK_NET_ABOVE_4_5
using System;
using System.Threading.Tasks;

namespace HSFrameWork.Net
{
    public interface IPlayerLinkAsync : IPlayerLink
    {
        Task MainTask { get; }
    }

    public interface IHSNetClientASync : IPlayerLinkClient, IPlayerLinkAsync
    {
        /// <summary>
        /// 通过Status传递的错误终止信息，不会通过异常传递。
        /// </summary>
        Task RunAsync(byte[] serverEndPoint, byte localSessionId, byte[] handShakeData, Func<byte[], int, int, bool> hv);
    }

    public interface ISimpleRunStop
    {
        Task MainTask { get; }
        /// <summary>
        /// 可能会抛出异常
        /// <returns></returns>
        Task RunAsync();
        /// <summary>
        /// 可能会抛出异常
        /// </summary>
        void SignalStop();
    }

    public interface IAsyncStopable
    {
        Task Stop();
    }

    /// <summary>
    /// UnityEditor/Windows
    /// </summary>
    public interface INotifier : ISimpleRunStop
    {
        string Name { get; }
        string Desc { get; }
        string Version { get; }

        bool MonitorDisabled { get; set; }

        /// <summary>
        /// 是否无法监控。（任务未正常运行，或者MonitorDisabled）
        /// </summary>
        bool IsDummy { get; }

        String Status { get; }
    }

}
#endif