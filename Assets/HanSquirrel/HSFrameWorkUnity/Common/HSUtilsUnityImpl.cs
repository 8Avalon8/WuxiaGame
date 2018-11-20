using GLib;
using System;
using System.IO;
using UnityEngine;

namespace HSFrameWork.Common.Inner
{
    /// <summary>
    /// 为HSUtils在Unity平台下的实现函数。（运行时 和 Editor都需要）
    /// </summary>
    public static class HSUtilsUnityImpl
    {
        /// <summary> 设置HSUtils的各种Log函数的实现； </summary>
        public static void ColdBind()
        {
            HSUtils.SetLogImpls(Log1, Log, LogWarning, LogError, Assert, Debug.LogException);
        }

        private static object _lockObj = new object();

        public static string LogFile { get; set; }

        private static void Log1(string format)
        {
            Log(format);
        }

        private static void Log(string format, params object[] args)
        {
            if (Debug.logger.IsLogTypeAllowed(LogType.Log))
            {
                string str = format.NullOrWhiteSpace() ? format : "{0} : {1}".EatWithTID(Mini.NowShort, format.Eat(args));
                if (LogFile != null)
                {
                    lock (_lockObj)
                        using (var sw = File.AppendText(LogFile))
                            sw.WriteLine(str);
                }
                Debug.Log(str);
            }
        }

        private static void LogWarning(string format, params object[] args)
        {
            if (Debug.logger.IsLogTypeAllowed(LogType.Warning))
            {
                string str = "{0} : {1}".EatWithTID(Mini.NowShort, format.Eat(args));
                if (LogFile != null)
                {
                    lock (_lockObj)
                        using (var sw = File.AppendText(LogFile))
                            sw.WriteLine(str);
                }
                Debug.LogWarning(str);
            }
        }

        private static void LogError(string format, params object[] args)
        {
            if (Debug.logger.IsLogTypeAllowed(LogType.Error))
            {
                string str = "{0} : {1}".EatWithTID(Mini.NowShort, format.Eat(args));
                if (LogFile != null)
                {
                    lock (_lockObj)
                        using (var sw = File.AppendText(LogFile))
                            sw.WriteLine(str);
                }
                Debug.LogError(str);
            }
        }

        private static void Assert(bool condition, string format, params object[] args)
        {
            //Debug.AssertFormat(condition, format, args);
            if (!condition)
                throw new Exception(format.Eat(args));
        }

    }
}
