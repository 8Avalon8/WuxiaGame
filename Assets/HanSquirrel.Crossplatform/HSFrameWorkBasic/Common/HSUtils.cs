using GLib;
using System;

namespace HSFrameWork.Common
{
    using Inner;
    namespace Inner
    {
        public delegate void AssertDelegate(bool condition, string format, params object[] args);
        public delegate void LogDelegate(string format, params object[] args);
    }

    /// <summary>
    /// 日志、调试相关的工具类
    /// </summary>
    public static class HSUtils
    {
        /// <summary>
        /// 无论HSFrameWork是什么状态，确定可以输出的错误日志
        /// </summary>
        public static void BasicLogError(Exception e, string format, params object[] args)
        {
#if HSFRAMEWORK_RUN_IN_MS_CONSOLE
            Mini.WriteLineError(format.Format(args));
            Mini.WriteLineError(e.ToString());
#else
            UnityEngine.Debug.LogFormat(format, args);
            UnityEngine.Debug.LogException(e);
#endif
        }

        public static bool Inited { get; private set; }
        public static bool StopOnUnexpectedError = false;

        private static LogDelegate LogExImpl, LogWarningImpl, LogErrorImpl, LogSuccessImpl;
        private static Action<Exception> LogExceptionImpl;
        private static Action<string> LogImpl;

        private static AssertDelegate AssertImpl;

        public static void SetLogImpls(Action<string> log, LogDelegate logEx, LogDelegate logWarning, LogDelegate logError, AssertDelegate assert, Action<Exception> logException)
        {
            LogImpl = log;
            LogExImpl = logEx;
            LogWarningImpl = logWarning;
            LogErrorImpl = logError;
            AssertImpl = assert;
            LogExceptionImpl = logException;
            Inited = true;
        }

        public static void SetLogSuccessImpls(LogDelegate log)
        {
            LogSuccessImpl = log;
        }

        /// <summary>
        /// 仅仅显示结束时的计时
        /// </summary>
        public static ExeTimer ExeTimerEnd(string desc)
        {
            return GLib.ExeTimer.GetOne(desc, Log, false);
        }

        /// <summary>
        /// 显示开始和结束时的计时
        /// </summary>
        public static ExeTimer ExeTimer(string desc)
        {
            return GLib.ExeTimer.GetOne(desc, Log, true);
        }

        public static ExeTimer ExeTimerSilent(string desc)
        {
            return GLib.ExeTimer.GetOne(desc, delegate { }, true);
        }

        public static void Log(string format)
        {
            if (LogImpl != null)
                LogImpl(format);
        }

        public static void Log(string format, params object[] args)
        {
            if (LogExImpl != null)
                LogExImpl(format, args);
        }

        public static void LogSuccess(string format, params object[] args)
        {
            if (LogSuccessImpl != null)
                LogSuccessImpl(format, args);
            else if (LogExImpl != null)
                LogExImpl(format, args);
        }

        public static void LogWarning(string format, params object[] args)
        {
            if (LogWarningImpl != null)
                LogWarningImpl(format, args);
        }

        private static int ErrorSeq;

        public static void LogError(string format, params object[] args)
        {
            if (LogErrorImpl != null)
                LogErrorImpl(format, args);

            ErrorSeq++;
            if (!_IsErrorExpected && StopOnUnexpectedError)
                Assert(false, "发生未期望的错误");
        }

        public static void LogException(Exception e)
        {
            if (LogExceptionImpl != null)
                LogExceptionImpl(e);
        }

        private static bool _IsErrorExpected = false;

        public static void ExpectError(string memo, Action action)
        {
            Log("期望发生错误！！！");
            _IsErrorExpected = true;
            int errorSeq = ErrorSeq;

            using (DisposeHelper.Create(() => _IsErrorExpected = false))
                action();

            if (ErrorSeq == errorSeq)
            {   //没有发生错误
                Assert(false, "期望发生错误，然而没有发生：" + memo);
            }
        }

        public static void ExceptException<T>(string memo, Action action) where T : Exception
        {
            try
            {
                Log("期望发生异常");
                action();
            }
            catch (Exception e)
            {
                if (e is T)
                {
                    LogException(e);
                    LogSuccess("异常如期发生，OK；");
                    return;
                }
                else
                {
                    Assert(false, "异常类型不对，期望：{0}，获得{1}。", typeof(T), e.GetType());
                }
            }
            Assert(false, "期望发生异常, 然而没有发生异常");
        }

        public static void Assert(bool condition)
        {
            Assert(condition, "");
        }

        public static void Assert(bool condition, string format, params object[] args)
        {
            if (AssertImpl != null)
                AssertImpl(condition, format, args);
        }

#if HSFRAMEWORK_NET_ABOVE_4_5
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
        public static void SelfTest(Action<string> displayAction)
        {
            var backup = HSUtils.StopOnUnexpectedError;

            using (DisposeHelper.Create(() => HSUtils.StopOnUnexpectedError = backup))
            {
                HSUtils.StopOnUnexpectedError = true;

                {
                    bool catched = false;
                    try
                    {
                        HSUtils.LogError("dddd");
                    }
                    catch
                    {
                        catched = true;
                        displayAction("捕获到不期望的Error");
                    }

                    if (!catched)
                        throw new Exception("HSUTils自测没有通过");
                }

                {
                    bool catched = false;
                    try
                    {
                        HSUtils.ExpectError("", delegate { });
                    }
                    catch
                    {
                        catched = true;
                        displayAction("【希望错误，但是没有发生。】也是一种错误。这种错误被成功捕获。");
                    }

                    if (!catched)
                        throw new Exception("HSUTils自测没有通过");
                }

                HSUtils.ExpectError("", delegate { HSUtils.LogError("dddd"); });

                HSUtils.Log("HSUtils自测通过");
                displayAction("HSUtils自测通过");
            }
        }
    }

    public class PassiveResourceDisposer
    {
        /// <summary>
        /// 用法举例 static private PassiveResourceDisposer _passiveResourceDisposer = PassiveResourceDisposer.Create(Clear);
        /// Clear为释放资源的Action
        /// </summary>
        public static PassiveResourceDisposer Create(Action disposeAction)
        {
            return new PassiveResourceDisposer(disposeAction);
        }

        public static PassiveResourceDisposer Create(Action OnCreate, Action onDispose)
        {
            if (OnCreate != null)
                OnCreate.Invoke();
            return new PassiveResourceDisposer(onDispose);
        }

        private readonly Action _onDispose;
        private PassiveResourceDisposer(Action onDispose)
        {
            _onDispose = onDispose;
        }

        ~PassiveResourceDisposer()
        {
            if (_onDispose != null) _onDispose.Invoke();
        }
    }
}
