using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;
using Stopwatch = System.Diagnostics.Stopwatch;
using GLib;
using UnityEditor;
using HSFrameWork.ConfigTable.Editor.Impl;
using HSFrameWork.Common;

namespace HSFrameWork.ConfigTable.Editor
{
    /// <summary>
    /// 用于在程序出现异常的时候也可以关闭掉进度条。
    /// </summary>
    public class ProgressBarAutoHide : IDisposable
    {
        private int WaitMS;
        public static ProgressBarAutoHide Get(int ms)
        {
            ProgressBarAutoHide v = new ProgressBarAutoHide();
            v.WaitMS = ms;
            return v;
        }
        public void Dispose()
        {
            Thread.Sleep(WaitMS);
            MenuHelper.SafeClearProgressBar();
        }
    }

    /// <summary>
    /// 无状态独立工具类
    /// </summary>
    public static class MenuHelper
    {

        public static void RunInOrSendToUI(Action a)
        {
            TE.RunInOrSendToUI(a);
        }

        /// <summary>
        /// 支持NoThreadExtention和RobotMode；
        /// 如果允许多线程，则将action在线程池里面运行，目的是让后台和界面互不干扰。
        /// </summary>
        public static void SafeWrapMenuAction(string title, Action<string> action)
        {
            bool hasException = false;
            bool success = false;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (TE.NoThreadExtention)
            {
                try
                {
                    HSUtils.Log("→→→→→→→→→→ 任务[{0}] 开始执行....... @{1}".Eat(title, Thread.CurrentThread.ManagedThreadId));
                    action(title);
                    HSUtils.Log("√√√√√√√√√任务[{0}]成功完成，总共花费时间{1}√√√ @{2}".Eat(title, stopwatch.Elapsed.FormatTimeSpanShort(),
                        Thread.CurrentThread.ManagedThreadId));
                    SafeShow100Progress(title);
                }
                catch (Exception e)
                {
                    HSUtils.LogException(e);
                    HSUtils.LogError("⚉⚉⚉⚉⚉⚉⚉⚉任务[{0}]异常终止，总共花费时间{1}⚉⚉⚉ @{2}".Eat(title, stopwatch.Elapsed.FormatTimeSpanShort(),
                         Thread.CurrentThread.ManagedThreadId));
                    SafeDisplayDialog(title, "发生异常，请查看日志。", "关闭");
                }

                SafeClearProgressBar();
            }
            else
            {
                TE.RunInPool(() =>
                {   //不会被外部检查的Task，内部必须TryCatch，否则有异常没有人知道。
                    try
                    {
                        HSUtils.Log("→→→→→→→→→→ 任务[{0}] 开始执行....... @{1}".Eat(title, Thread.CurrentThread.ManagedThreadId));
                        action(title);
                        success = true;
                    }
                    catch (AggregateException ae)
                    {
                        foreach (var e in ae.Flatten().InnerExceptions)
                        {
                            if (!(e is TaskCanceledException) && !(e is OperationCanceledException))
                            {
                                hasException = true;
                                HSUtils.LogException(e);
                            }
                        }
                    }
                    catch (Exception e)
                    {   //理论上不会有这样的Exception，仅仅是为了万全
                        hasException = true;
                        HSUtils.LogException(e);
                    }

                    stopwatch.Stop();

                    if (hasException)
                    {
                        HSUtils.LogError("⚉⚉⚉⚉⚉⚉⚉⚉任务[{0}]异常终止，总共花费时间{1}⚉⚉⚉ @{2}".Eat(title, stopwatch.Elapsed.FormatTimeSpanShort(),
                            Thread.CurrentThread.ManagedThreadId));
                        SafeDisplayDialog(title, "发生异常，请查看日志。", "关闭");
                    }
                    else if (success)
                    {
                        HSUtils.Log("√√√√√√√√√任务[{0}]成功完成，总共花费时间{1}√√√ @{2}".Eat(title, stopwatch.Elapsed.FormatTimeSpanShort(),
                            Thread.CurrentThread.ManagedThreadId));
                    }
                    else
                    {
                        HSUtils.LogWarning("⍉⍉⍉任务[{0}]被用户取消，总共花费时间{1}⍉⍉⍉ @{2}".Eat(title, stopwatch.Elapsed.FormatTimeSpanShort(),
                            Thread.CurrentThread.ManagedThreadId));
                    }

                    SafeClearProgressBar();
                });
            }
        }

        /// <summary> 如果全局无界面，则无操作；如果在用户线程，则直接显示；如果在线程池，则SendToUI执行并等待其返回。 </summary>
        public static void SafeDisplayProgressBar(string title, string info, float progress)
        {
            if (HSCTC.IsRobotMode)
                return;

            title = "{0} @[{1}]".EatWithTID(title, TE.IsUIThread ? "主线程" : "线程池");

            TE.RunInOrSendToUI(() => EditorUtility.DisplayProgressBar(title, info, progress));
        }

        /// <summary> 如果全局无界面，则无操作；如果在用户线程，则直接显示；如果在线程池，则SendToUI执行并等待其返回。 </summary>
        public static void SafeClearProgressBar()
        {
            if (HSCTC.IsRobotMode)
                return;

            TE.RunInOrSendToUI(EditorUtility.ClearProgressBar);
        }

        /// <summary> 如果全局无界面，则无操作；如果在用户线程，则直接显示；如果在线程池，则SendToUI执行并等待其返回。 </summary>
        public static void SafeDisplayOKDialog()
        {
            SafeDisplayDialog("恭喜", "任务完成", "OK");
        }

        /// <summary> 如果全局无界面，则自动返回TRUE；如果在用户线程，则直接显示；如果在线程池，则SendToUI执行并等待其返回。 </summary>
        public static bool SafeDisplayDialog(string title, string message, string ok, string cancel = "")
        {
            return HSCTC.IsRobotMode ? true : TE.RunInOrSendToUI(() => EditorUtility.DisplayDialog(title, message, ok, cancel));
        }

        /// <summary>
        /// 显示100%进度条，并自动在1秒后关闭。在打包机模式下自动忽略。
        /// </summary>
        /// <param name="title"></param>
        public static void SafeShow100Progress(string title)
        {
            if (HSCTC.IsRobotMode)
                return;

            SafeDisplayProgressBar(title, "全部完成", 1.0f);
            Thread.Sleep(1000);
            SafeClearProgressBar();
        }
    }
}

