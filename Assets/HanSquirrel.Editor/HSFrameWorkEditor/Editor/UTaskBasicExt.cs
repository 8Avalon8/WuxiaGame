using System;
using System.Threading.Tasks;
using System.Threading;
using HSFrameWork.Common;

namespace HSFrameWork.ConfigTable.Editor.Impl
{
    /// <summary>
    /// 跨平台。System.Threading.Tasks.Task的一些快捷函数，Task库来自 https://www.nuget.org/packages/TaskParallelLibrary/
    /// </summary> 
    public static partial class TE
    {
        //缺省为禁用；因为MAC下无法使用。
        public static bool NoThreadExtention = true;
        public static void CheckIfFrozen()
        {
            if (NoThreadExtention) throw new Exception("程序编写错误：.NET线程扩展被禁用。");
        }

        public static void NoOp(this Task task) { }

        static TE()
        {
            _tcs.SetResult(true);
        }

        private static TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();
        public static Task CompeletedTask
        {
            get
            {
                CheckIfFrozen();
                return _tcs.Task;
            }
        }

        /// <summary>
        /// 两个参数都可以为null
        /// </summary>
        public static Task SendMe(this Task t, Action<Task> action)
        {
            if (t == null)
            {
                return null;
            }

            CheckIfFrozen();

            if (action != null)
            {
                action(t);
            }
            return t;
        }

        /// <summary>
        /// t可以为空，用于类似 Task t= CreateTask(...).Wait();
        /// </summary>
        public static Task WaitC(this Task t)
        {
            if (t == null)
            {
                return null;
            }

            CheckIfFrozen();

            t.Wait();
            return t;
        }

        /// <summary>
        /// t可以为空，用于类似 Task t= CreateTask(...).Wait();
        /// </summary>
        public static Task WaitC(this Task t, int ms)
        {
            if (t == null)
            {
                return null;
            }

            CheckIfFrozen();

            t.Wait(ms);
            return t;
        }

        public static Task RunInPool(Action a)
        {
            return RunInPool(a, CancellationToken.None);
        }

        public static void RunInPoolVoid(Action a)
        {
            RunInPool(a, CancellationToken.None).ContinueWith(t =>
            {
                try
                {
                    t.Wait();
                }
                catch (AggregateException ae)
                {
                    foreach (var e in ae.Flatten().InnerExceptions)
                    {
                        HSUtils.LogException(e);
                    }
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public static Task RunInPool(Action a, CancellationToken token)
        {
            CheckIfFrozen();

            return Task.Factory.StartNew(delegate
            {
                SafeMarkThreadAsPool();
                a();
            }, token, TaskCreationOptions.None, TaskScheduler.Default);
        }

        public static Task RunInPool<T1>(Action<T1> a, T1 t1)
        {
            return RunInPool(a, t1, CancellationToken.None);
        }

        public static Task RunInPool<T1>(Action<T1> a, T1 t1, CancellationToken token)
        {
            return RunInPool(() => a(t1), token);
        }

        public static Task RunInPool<T1, T2>(Action<T1, T2> a, T1 t1, T2 t2)
        {
            return RunInPool(a, t1, t2, CancellationToken.None);
        }

        public static Task RunInPool<T1, T2>(Action<T1, T2> a, T1 t1, T2 t2, CancellationToken token)
        {
            return RunInPool(() => a(t1, t2), token);
        }

        public static Task RunInPool<T1, T2, T3>(Action<T1, T2, T3> a, T1 t1, T2 t2, T3 t3)
        {
            return RunInPool(a, t1, t2, t3, CancellationToken.None);
        }

        public static Task RunInPool<T1, T2, T3>(Action<T1, T2, T3> a, T1 t1, T2 t2, T3 t3, CancellationToken token)
        {
            return RunInPool(() => a(t1, t2, t3), token);
        }
    }
}
