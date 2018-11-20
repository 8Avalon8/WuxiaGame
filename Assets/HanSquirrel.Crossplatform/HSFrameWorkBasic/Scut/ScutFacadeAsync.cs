#if HSFRAMEWORK_NET_ABOVE_4_5
using HSFrameWork.Common;
using HSFrameWork.Scut.Inner;
using System;
using System.Threading.Tasks;

namespace HSFrameWork.Scut
{
    public class ActionAsyncException : Exception
    {
        public readonly ErrorCode ErrorCode;
        public readonly string ErrorInfo;

        public ActionAsyncException(ErrorCode ec, string errorInfo) : base($"{ec}:{errorInfo}")
        {
            ErrorCode = ec;
            ErrorInfo = errorInfo;
        }
    }

    public interface IActionClientAsync : IActionClient
    {
        /// <summary>
        /// 如果是错误，只可能是用户取消。因为队列会自动重传。
        /// </summary>
        Task<ActionResult> SendInActionQueueAsync(int actionId, ActionParam actionParam, bool bShowLoading = true);
        /// <summary>
        /// 可能有各种错误。
        /// </summary>
        Task<ActionResult> SendExAsync(int actionId, ActionParam actionParam = null, bool bShowLoading = true);
        /// <summary>
        /// 可能有各种错误。
        /// </summary>
        Task<TO> CallExAsync<TI, TO>(TI input, string method = "any", bool bShowLoading = true);

        Task CloseAsync();
    }

    public partial class ActionClientFactory
    {
        public static IActionClientAsync CreateAsync(string url)
        {
            return new ActionClientAsync(url, Container.Resolve<IActionClientSettings>());
        }
    }
}

#endif
