#if HSFRAMEWORK_NET_ABOVE_4_5
using System.Threading.Tasks;

using UnityEngine;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System;

namespace HSFrameWork.Common
{
    public class UTask
    {
        public static Task RunAsync(Action action)
        {
            return Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, UTaskScheduler.Default);
        }

        public static Task RunAsync(Action<object> action, object state)
        {
            return Task.Factory.StartNew(action, state, CancellationToken.None, TaskCreationOptions.None, UTaskScheduler.Default);
        }

        public static Task<TResult> RunAsync<TResult>(Func<TResult> function)
        {
            return Task.Factory.StartNew(function, CancellationToken.None, TaskCreationOptions.None, UTaskScheduler.Default);
        }

        public static Task<TResult> RunAsync<TResult>(Func<object, TResult> function, object state)
        {
            return Task.Factory.StartNew(function, state, CancellationToken.None, TaskCreationOptions.None, UTaskScheduler.Default);
        }

    }

    public class UTaskScheduler
    {
        /// <summary>
        /// 唯一对外接口，用于Task.Factory.CreateNew里指定Task在Unity的UI线程中执行。
        /// </summary>
        public static TaskScheduler Default { get; private set; }

        public static void ColdBind()
        {
            UTaskSchedulerMB.Instance.ColdBind();
        }

        private class UTaskSchedulerMB : SingletonMB<UTaskSchedulerMB, UTaskSchedulerMB>
        {
            static UTaskSchedulerMB()
            {
                Debug.Log("UnityTaskScheduler开始运行");
                UTaskScheduler.Default = new BurtonTaskScheduler();
            }

            public void ColdBind()
            {
            }

            void Update()
            {
                (Default as BurtonTaskScheduler).ExecuteOne();
            }

            void Awake()
            {
                DontDestroyOnLoad(this);
            }
        }

        private sealed class BurtonTaskScheduler : TaskScheduler
        {
            private ConcurrentQueue<Task> tasksCollection = new ConcurrentQueue<Task>();

            public void ExecuteOne()
            {
                Task task = null;
                if (tasksCollection.TryDequeue(out task))
                {
                    TryExecuteTask(task);
                }
            }

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return tasksCollection.ToArray();
            }

            protected override void QueueTask(Task task)
            {
                if (task != null)
                    tasksCollection.Enqueue(task);
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return false;
            }
        }
    }
}
#endif
