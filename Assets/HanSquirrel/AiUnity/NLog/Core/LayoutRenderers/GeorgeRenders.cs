//  Added By GG 20180701@Van

#if AIUNITY_CODE
namespace AiUnity.NLog.Core.LayoutRenderers
{
    using GLib;
    using System;
    using System.Text;
    using System.Threading;
    using UnityEngine;

    [LayoutRenderer("log_path_gg")]
    public class LogPathLayoutRendererGG : LayoutRenderer
    {
        public static readonly string LogPath = Application.persistentDataPath.Sub("NLog").CreateDir();

        static LogPathLayoutRendererGG()
        {
            Debug.LogFormat("NLog的日志目录：[{0}]", LogPath);
        }

        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            builder.AppendFormat(LogPath);
        }
    }


    [LayoutRenderer("framecount_gg")]
    public class FrameCountLayoutRendererGG : LayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            if (Thread.CurrentThread.ManagedThreadId == NLogManager.Instance.MainThreadID)
                builder.AppendFormat("{0:D4}", UnityEngine.Time.frameCount % 10000);
            else
                builder.AppendFormat("0000");
        }
    }

    [LayoutRenderer("appstarttime_gg")]
    public class AppStartTimeLayoutRendererGG : LayoutRenderer
    {
        public static void Reset()
        {
            _AppStartupTimeStr = DateTime.Now.ToString("MMdd-HHmmss");
        }

        private static string _AppStartupTimeStr = DateTime.Now.ToString("MMdd-HHmmss");
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            builder.Append(_AppStartupTimeStr);
        }
    }
}
#endif
