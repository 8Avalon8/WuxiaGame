using System;
using System.Threading.Tasks;

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace HSFrameWork.ConfigTable.Editor.Impl
{
    /// <summary>
    /// 可以被Task.Factory.CreateNew使用的TaskScheduler。会让Task在Unity主线程工作。
    /// </summary>
    internal class UITaskScheduler
    {
        /// <summary>
        /// 唯一对外接口，用于Task.Factory.CreateNew里指定Task在Unity的UI线程中执行。
        /// </summary>
        public static TaskScheduler Default { get; private set; }

        private static DateTime _start;
        private static int _updateCount;

        /// <summary>
        /// 开发者调试使用
        /// </summary>
        public static double Freq{
            get
            {
                return _updateCount / (DateTime.Now - _start).TotalSeconds;
            }
        }

        private static void  OnUpdate()
        {
            _updateCount++;

            if ((DateTime.Now - _start).TotalSeconds > 5)
            {
                _start = DateTime.Now;
                _updateCount = 0;
            }

            TE.MarkThreadAsUI();
            (Default as BurtonTaskScheduler).ExecuteOne();
        }


        private static bool _inited=false;

        internal static void ColdBind()
        {
            if (_inited)
            {
                return;
            }

            _inited = true;
            _start = DateTime.Now;
            Default = new BurtonTaskScheduler();
            EditorApplication.update += OnUpdate;
        }

        private sealed class BurtonTaskScheduler : TaskScheduler
        {
            private ConcurrentQueue<Task> tasksCollection = new ConcurrentQueue<Task>();

            public void ExecuteOne()
            {
                Task task = null;
                if( tasksCollection.TryDequeue(out task))
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
