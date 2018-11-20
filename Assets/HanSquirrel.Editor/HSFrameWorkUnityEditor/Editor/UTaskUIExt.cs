using System;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using GLib;

namespace HSFrameWork.ConfigTable.Editor.Impl
{
    public static partial class TE
    {
        #region 只读状态
        public static string ThreadSetStatus
        {
            get
            {
                return "PoolThread [{0}] 个, UIThread [{1}] 个".Eat(_poolThreadIDset.Count, _uiThreadIDset.Count);
            }
        }

        /// <summary>
        /// 当前线程是否是Unity主线程。
        /// </summary>
        public static bool IsUIThread
        {
            get
            {
                lock (_LockObj)
                {
                    if (_uiThreadIDset.Contains(Thread.CurrentThread.ManagedThreadId))
                        return true;
                    if (_poolThreadIDset.Contains(Thread.CurrentThread.ManagedThreadId))
                        return false;
                    try
                    {
                        string s = Application.dataPath;
                        _uiThreadIDset.Add(Thread.CurrentThread.ManagedThreadId);
                    }
                    catch
                    {
                        Debug.Log("请忽略上面的那个dataPath错误。仅仅是程序内部自动检测。打扰见谅。".EatWithTID());
                        lock (_poolThreadIDset)
                            _poolThreadIDset.Add(Thread.CurrentThread.ManagedThreadId);
                    }
                    return IsUIThread;
                }
            }
        }
        public static void ThrowIfNotInUI(string opname)
        {
            lock (_LockObj)
            {
                try
                {
                    string s = Application.dataPath;
                    _uiThreadIDset.Add(Thread.CurrentThread.ManagedThreadId);
                }
                catch
                {
                    lock (_poolThreadIDset)
                        _poolThreadIDset.Add(Thread.CurrentThread.ManagedThreadId);
                    throw new Exception("编程错误：[{0}] 只能在主线程运行。".EatWithTID(opname));
                }
            }
        }
        #endregion

        #region 内部设置工具函数
        public static void MarkThreadAsUI()
        {
            lock (_LockObj)
            {
                if (_uiThreadIDset.Contains(Thread.CurrentThread.ManagedThreadId))
                    return;

                try
                {
                    string s = Application.dataPath;
                    _uiThreadIDset.Add(Thread.CurrentThread.ManagedThreadId);
                }
                catch
                {
                    throw new Exception("程序编写错误：MarkThreadAsUI只能在UI线程中调用{0}".EatWithTID());
                }
            }
        }

        /// <summary>
        /// 有可能此线程是UI线程，只是大可能是Pool
        /// </summary>
        public static void SafeMarkThreadAsPool()
        {
            CheckIfFrozen();

            lock (_LockObj)
            {
                if (_uiThreadIDset.Contains(Thread.CurrentThread.ManagedThreadId))
                {
                    return;
                }

                lock (_poolThreadIDset)
                    _poolThreadIDset.Add(Thread.CurrentThread.ManagedThreadId);
            }
        }
        #endregion

        #region 在UI线程执行
        /// <summary>
        /// 判读如果当前是UI线程，则Block执行之；否则在UI线程执行，并等待其完成。
        /// </summary>
        public static void RunInOrSendToUI(Action a)
        {
            RunInOrSendToUI(CancellationToken.None, a);
        }

        /// <summary>
        /// 判读如果当前是UI线程，则Block执行之；否则在UI线程执行，并等待其完成。
        /// </summary>
        public static void RunInOrSendToUI(CancellationToken token, Action a)
        {
            if (IsUIThread)
            {
                token.SafeThrowIfCancellationRequested();
                a();
            }
            else
            {
                PostToUI(a, token).Wait();
            }
        }

        /// <summary>
        /// 判读如果当前是UI线程，则Block执行之；否则在UI线程执行，并等待其完成。
        /// </summary>
        public static T RunInOrSendToUI<T>(Func<T> func)
        {
            if (IsUIThread)
            {
                return func();
            }
            else
            {
                CheckIfFrozen();
                return Task.Factory.StartNew(func, CancellationToken.None, TaskCreationOptions.None, UITaskScheduler.Default)
                    .GetAwaiter().GetResult();
            }
        }
        #endregion

        #region 各种UITask创建函数
        public static Task PostToUI(Action a, int ms = 0)
        {
            return PostToUI(a, ms, CancellationToken.None);
        }

        public static Task PostToUI(Action a, CancellationToken token)
        {
            return PostToUI(a, 0, token);
        }

        public static Task PostToUI(Action a, int ms, CancellationToken token)
        {
            CheckIfFrozen();

            if (ms == 0)
            {
                return Task.Factory.StartNew(a, token, TaskCreationOptions.None, UITaskScheduler.Default);
            }
            else
            {
                return RunInPool(() =>
                {
                    Thread.Sleep(ms);
                    PostToUI(a, 0).GetAwaiter().GetResult();
                });
            }
        }

        public static Task PostToUI<T>(Action<T> a, T t, int ms = 0)
        {
            return PostToUI(a, t, ms, CancellationToken.None);
        }

        public static Task PostToUI<T>(Action<T> a, T t, CancellationToken token)
        {
            return PostToUI(a, t, 0, token);
        }

        public static Task PostToUI<T>(Action<T> a, T t, int ms, CancellationToken token)
        {
            return PostToUI(() => a(t), ms, token);
        }


        public static Task PostToUI<T, T1>(Action<T, T1> a, T t, T1 t1, int ms = 0)
        {
            return PostToUI(a, t, t1, ms, CancellationToken.None);
        }

        public static Task PostToUI<T, T1>(Action<T, T1> a, T t, T1 t1, CancellationToken token)
        {
            return PostToUI(a, t, t1, 0, token);
        }

        public static Task PostToUI<T, T1>(Action<T, T1> a, T t, T1 t1, int ms, CancellationToken token)
        {
            return PostToUI(() => a(t, t1), ms, token);
        }


        public static Task PostToUI<T, T1, T2>(Action<T, T1, T2> a, T t, T1 t1, T2 t2, int ms = 0)
        {
            return PostToUI(a, t, t1, t2, ms, CancellationToken.None);
        }

        public static Task PostToUI<T, T1, T2>(Action<T, T1, T2> a, T t, T1 t1, T2 t2, CancellationToken token)
        {
            return PostToUI(a, t, t1, t2, 0, token);
        }

        public static Task PostToUI<T, T1, T2>(Action<T, T1, T2> a, T t, T1 t1, T2 t2, int ms, CancellationToken token)
        {
            return PostToUI(() => a(t, t1, t2), ms, token);
        }

        public static Task UITask(Func<Task> funcTask)
        {
            CheckIfFrozen();

            Task uiTask = null;
            TE.PostToUI(() => uiTask = funcTask()).Wait();
            return uiTask;
        }

        public static Task UITask<T>(Func<T, Task> funcTask, T t)
        {
            CheckIfFrozen();

            Task uiTask = null;
            TE.PostToUI(() => uiTask = funcTask(t)).Wait();
            return uiTask;
        }

        public static Task UITask<T1, T2>(Func<T1, T2, Task> funcTask, T1 t1, T2 t2)
        {
            CheckIfFrozen();
            Task uiTask = null;
            TE.PostToUI(() => uiTask = funcTask(t1, t2)).Wait();
            return uiTask;
        }

        public static Task UITask<T1, T2, T3>(Func<T1, T2, T3, Task> funcTask, T1 t1, T2 t2, T3 t3)
        {
            CheckIfFrozen();
            Task uiTask = null;
            TE.PostToUI(() => uiTask = funcTask(t1, t2, t3)).Wait();
            return uiTask;
        }
        #endregion

        #region 内部私有
        private static object _LockObj = new object();
        private static HashSet<int> _poolThreadIDset = new HashSet<int>();
        private static HashSet<int> _uiThreadIDset = new HashSet<int>();
        #endregion
    }
}
