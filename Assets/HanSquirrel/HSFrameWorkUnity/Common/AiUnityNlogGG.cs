namespace AiUnity.NLog.GG
{
#if HSFRAMEWORK_AIUNITY_NLOG_GG
    using HSFrameWork.Common;
    using System;
    using GLib;
    using AiUnity.NLog.Core;
    using UnityEngine;

    internal class HSLogManagerALogImpl : HSLogManager
    {
        public static HSLogManagerALogImpl Instance = new HSLogManagerALogImpl();
        private HSLogManagerALogImpl() { }

        protected override IHSLogger GetLoggerInner(string name)
        {
            return new HSLogger(NLogManager.Instance.GetLogger(name, null, null));
        }

        private class HSLogger : IHSLogger
        {
            private NLogger _Logger;
            public HSLogger(NLogger logger)
            {
                _Logger = logger;
            }

            public bool IsTraceEnabled { get { return _Logger.IsTraceEnabled; } }

            public void Trace(string message, params object[] args)
            {
                _Logger.Trace(message, args);
            }
            public void Debug(string message, params object[] args)
            {
                _Logger.Debug(message, args);
            }
            public void Info(string message, params object[] args)
            {
                _Logger.Info(message, args);
            }
            public void Warn(string message, params object[] args)
            {
                _Logger.Warn(message, args);
            }
            public void Error(string message, params object[] args)
            {
                _Logger.Error(message, args);
            }

            public void Fatal(string message, params object[] args)
            {
                _Logger.Fatal(message, args);
            }

            public void Warn(Exception e, string message, params object[] args)
            {
                _Logger.Warn(e, message, args);
            }
            public void Error(Exception e, string message, params object[] args)
            {
                _Logger.Error(e, message, args);
            }
            public void Fatal(Exception e, string message, params object[] args)
            {
                _Logger.Fatal(e, message, args);
            }
        }
    }
#endif
}
