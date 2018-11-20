#if HSFRAMEWORK_NET_ABOVE_4_5
using System;
using HSFrameWork.Common.Inner;
using System.Threading.Tasks;
using GLibEx;
using System.Threading;
using System.Collections.Concurrent;

namespace HSFrameWork.Scut.Inner
{
    public class ActionClientAsync : ActionClient, IActionClientAsync
    {
        #region 公开接口实现
        public Task<TO> CallExAsync<TI, TO>(TI input, string method = "any", bool bShowLoading = true)
        {
            if (Interlocked.CompareExchange(ref _Closed, 0, 0) == 1)
            {
                return GetCanceledException<TO>();
            }
            else
            {
                var tcs = new TaskCompletionSource<TO>();
                _CommondQueue.Enqueue(() =>
                {
                    CallEx<TI, TO>(input, tcs.SetResult,
                        (ec, em) => tcs.SetException(new ActionAsyncException(ec, em)),
                        method, bShowLoading);
                }, false);
                return tcs.Task;
            }
        }

        public Task<ActionResult> SendExAsync(int actionId, ActionParam actionParam = null, bool bShowLoading = true)
        {
            if (Interlocked.CompareExchange(ref _Closed, 0, 0) == 1)
                return _CanceledTask;

            var tcs = new TaskCompletionSource<ActionResult>();
            _CommondQueue.Enqueue(() =>
            {
                SendEx(actionId, tcs.SetResult,
                    (ec, em) => tcs.SetException(new ActionAsyncException(ec, em)),
                    actionParam, bShowLoading);
            }, false);
            return tcs.Task;
        }

        public Task<ActionResult> SendInActionQueueAsync(int actionId, ActionParam actionParam, bool bShowLoading = true)
        {
            if (Interlocked.CompareExchange(ref _Closed, 0, 0) == 1)
                return _CanceledTask;

            var tcs = new TaskCompletionSource<ActionResult>();
            _CommondQueue.Enqueue(() =>
            {
                SendInActionQueueEx(actionId,
                tcs.SetResult,
                (ec, em) => tcs.SetException(new ActionAsyncException(ec, em)),
                actionParam, bShowLoading);
            }, false);
            return tcs.Task;
        }

        public Task CloseAsync()
        {
            if (Interlocked.CompareExchange(ref _Closed, 0, 0) == 0)
            {
                _CommondQueue.Enqueue(() =>
                {
                    base.Close(false);
                }, false);
            }

            return _MainTask;
        }
        #endregion

        #region 私有
        private SwitchQueue<Action> _CommondQueue = new SwitchQueue<Action>();
        private readonly Task _MainTask;
        internal ActionClientAsync(string url, IActionClientSettings actionClientSetting) : base(url, null, actionClientSetting)
        {
            _MainTask = Task.Run(async () =>
            {
                try
                {
                    while (Interlocked.CompareExchange(ref _Closed, 0, 0) == 0)
                    {
                        await Task.WhenAny(_CommondQueue.EnqueueEvent.WaitAsync(), TaskUtils.Delay(30));
                        ProcessCommandQueue();
                    }
                    ProcessCommandQueue();
                    Logger.Trace("MainTask Exits.");
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex, "George编程错误，ActionClientAsync主任务出现未处理的异常。");
                }
            });
        }

        private void ProcessCommandQueue()
        {
            _CommondQueue.Switch();
            while (_CommondQueue.Count > 0)
                _CommondQueue.Dequeue().Invoke();

            Update();
        }

        private static Task<ActionResult> _CanceledTask;
        private static ConcurrentDictionary<Type, object> _CanceledTaskDict = new ConcurrentDictionary<Type, object>();
        private static Task<T> GetCanceledException<T>()
        {
            return _CanceledTaskDict.GetOrAdd(typeof(T), x =>
            {
                var tcs = new TaskCompletionSource<T>();
                tcs.SetException(new ActionAsyncException(ErrorCode.UserCanceled, "用户关闭ActionClient"));
                return tcs.Task;
            }) as Task<T>;
        }

        static ActionClientAsync()
        {
            var tcs = new TaskCompletionSource<ActionResult>();
            tcs.SetException(new ActionAsyncException(ErrorCode.UserCanceled, "用户关闭ActionClient"));
            _CanceledTask = tcs.Task;
        }
        #endregion
    }
}
#endif
