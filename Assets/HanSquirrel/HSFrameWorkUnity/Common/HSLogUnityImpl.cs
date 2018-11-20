using System;

namespace HSFrameWork.Common.Impl
{
    internal class HSLogManagerUnityImpl : HSLogManager
    {
        public static bool TraceEnabled { get; set; }
        public static bool DebugEnabled { get; set; }

        public static HSLogManagerUnityImpl Instance = new HSLogManagerUnityImpl();

        private HSLogManagerUnityImpl() { }

        protected override IHSLogger GetLoggerInner(string name)
        {
            return _Logger;
        }

        private static HSLogger _Logger = new HSLogger();
        private class HSLogger : IHSLogger
        {
            public bool IsTraceEnabled
            {
                get
                {
                    return HSLogManagerUnityImpl.TraceEnabled && UnityEngine.Debug.logger.IsLogTypeAllowed(UnityEngine.LogType.Log);
                }
            }

            public void Trace(string message, params object[] args)
            {
                if (HSLogManagerUnityImpl.TraceEnabled)
                    UnityEngine.Debug.LogFormat(message, args);
            }

            public void Debug(string message, params object[] args)
            {
                if (HSLogManagerUnityImpl.DebugEnabled)
                    UnityEngine.Debug.LogFormat(message, args);
            }
            public void Info(string message, params object[] args)
            {
                UnityEngine.Debug.LogFormat(message, args);
            }

            public void Warn(string message, params object[] args)
            {
                UnityEngine.Debug.LogWarningFormat(message, args);
            }

            public void Warn(Exception e, string message, params object[] args)
            {
                UnityEngine.Debug.LogException(e);
                UnityEngine.Debug.LogWarningFormat(message, args);
            }

            public void Error(Exception e, string message, params object[] args)
            {
                UnityEngine.Debug.LogException(e);
                UnityEngine.Debug.LogErrorFormat(message, args);
            }

            public void Error(string message, params object[] args)
            {
                UnityEngine.Debug.LogErrorFormat(message, args);
            }

            public void Fatal(Exception e, string message, params object[] args)
            {
                UnityEngine.Debug.LogException(e);
                UnityEngine.Debug.LogErrorFormat(message, args);
            }

            public void Fatal(string message, params object[] args)
            {
                UnityEngine.Debug.LogErrorFormat(message, args);
            }
        }
    }
}
