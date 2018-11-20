using GLib;
using System;

namespace HSFrameWork.Common.Inner
{
    /// <summary>
    /// 为HSUtils在Unity平台下的实现函数。（运行时 和 Editor都需要）
    /// </summary>
    public static class HSUtilsHSLogImpl
    {
        /// <summary> 设置HSUtils的各种Log函数的实现； </summary>
        public static void ColdBind()
        {
            HSUtils.SetLogImpls(Log1, Log, LogWarning, LogError, Assert, LogException);
        }

        private static void Log1(string format)
        {
            Log(format);
        }

        private static void Log(string format, params object[] args)
        {
            _Logger.Info(format, args);
        }

        private static void LogWarning(string format, params object[] args)
        {
            _Logger.Warn(format, args);
        }

        private static void LogError(string format, params object[] args)
        {
            _Logger.Error(format, args);
        }

        private static void LogException(Exception e)
        {
            _Logger.Error(e, "");
        }

        private static void Assert(bool condition, string format, params object[] args)
        {
            if (!condition)
                throw new Exception(format.Eat(args));
        }


        private static IHSLogger _Logger = HSLogManager.GetLogger("Default");
    }
}
