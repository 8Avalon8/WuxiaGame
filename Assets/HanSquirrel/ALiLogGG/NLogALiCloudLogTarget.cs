#if HSFRAMEWORK_RUN_IN_MS_CONSOLE || (AIUNITY_CODE && HSFRAMEWORK_ALIYUN_LOG)
using System.Collections.Generic;
using System;
using Aliyun.Api.LOG;
using Aliyun.Api.LOG.Request;
using Aliyun.Api.LOG.Data;
using Aliyun.Api.LOG.Common.Utilities;
using Aliyun.Api.LOG.Response;
using GLib;
using HSFrameWork.Common;
using System.Linq;
using Aliyun.Api.LOG.Utilities;
using HSFrameWork.Common.Inner;

#if AIUNITY_CODE && HSFRAMEWORK_ALIYUN_LOG
using AiUnity.NLog.Core.Common;
using AiUnity.NLog.Core.Config;
using AiUnity.NLog.Core.Internal;
using AiUnity.NLog.Core.Layouts;
using AiUnity.Common.InternalLog;
using AiUnity.Common.Attributes;
namespace AiUnity.NLog.Core.Targets
#else
using NLog.Common;
namespace NLog.Targets
#endif
{
    [Target("AliCloud")]
    public class ALiCloudLogTarget : TargetWithLayout
    {
        protected override void Write(AsyncLogEventInfo logEvent)
        {
            Write((IList<AsyncLogEventInfo>)new AsyncLogEventInfo[1]
                {
                    logEvent
                });
        }

#if HSFRAMEWORK_RUN_IN_MS_CONSOLE
        [Obsolete("Instead override Write(IList<AsyncLogEventInfo> logEvents. Marked obsolete on NLog 4.5")]
#endif
        protected override void Write(AsyncLogEventInfo[] logEvents)
        {
            Write((IList<AsyncLogEventInfo>)logEvents);
        }


        private static FixedCircularQueue<Tuple<bool, DateTime, int>> _WriteLog = new FixedCircularQueue<Tuple<bool, DateTime, int>>(100);

        public static void DumpWriteLog(Action<string> output)
        {
            output(_WriteLog.Select(x => "{0}\t{1}\t{2}".f(x.Item1 ? "Post" : "Local", x.Item2.ToString("yyyy/MM/dd HH:mm:ss.fff"), x.Item3)).ToList().JoinC("\r\n"));
        }

        private static int _SendSeq = 0;
        private static int _LogSeq = 0;
#if HSFRAMEWORK_RUN_IN_MS_CONSOLE
        protected override void Write(IList<AsyncLogEventInfo> logEvents)
#else
        protected void Write(IList<AsyncLogEventInfo> logEvents)
#endif
        {
            _WriteLog.Enqueue(Tuple.Create(false, DateTime.Now, logEvents.Count));
            LogClient client = new LogClient(EndPoint, AccesskeyId, AccessKey);
            client.ConnectionTimeout = client.ReadWriteTimeout = 10000;

            PutLogsRequest putLogsReqError = new PutLogsRequest();
            putLogsReqError.Project = Project;
            putLogsReqError.Logstore = LogStore;
            putLogsReqError.Topic = _SourceAgent.GetGameName();
            putLogsReqError.Source = _SourceAgent.GetSource();
            putLogsReqError.LogItems = new List<LogItem>();
            foreach (var le in logEvents)
            {
                PostLogToRemoteIfSizeEnoughThenClear(false, client, putLogsReqError);

                LogItem logItem = new LogItem();
                logItem.Time = DateUtils.TimeSpan(le.LogEvent.TimeStamp);

                logItem.PushBack("sendseq", _SendSeq.ToString());
                logItem.PushBack("seq", (_LogSeq++).ToString());
                logItem.PushBack("logtime", DateUtils.ToBJTime(le.LogEvent.TimeStamp).ToString("yyyy/MM/dd HH:mm:ss.fff"));

#if HSFRAMEWORK_RUN_IN_MS_CONSOLE
                logItem.PushBack("msg", RenderLogEvent(Layout, le.LogEvent));
#else
                logItem.PushBack("msg", this.Layout.Render(le.LogEvent));
#endif

                putLogsReqError.LogItems.Add(logItem);
            }

            PostLogToRemoteIfSizeEnoughThenClear(true, client, putLogsReqError);
        }

        private static void PostLogToRemoteIfSizeEnoughThenClear(bool forceOutput, LogClient client, PutLogsRequest putLogsReqError)
        {
            if (putLogsReqError.LogItems.Count > 0 && (forceOutput || putLogsReqError.LogItems.Count >= LogConsts.LIMIT_LOG_COUNT))
            {
                _WriteLog.Enqueue(Tuple.Create(true, DateTime.Now, putLogsReqError.LogItems.Count));
                _SendSeq++;
                try
                {
                    PutLogsResponse putLogRespError = client.PutLogs(putLogsReqError);
                }
                catch (System.Exception ex)
                {
                    HSUtils.BasicLogError(ex, "ALiYunLog PutLogs错误, 丢掉{0}个日志。", putLogsReqError.LogItems.Count);
                }
                putLogsReqError.LogItems.Clear();
            }
        }

        private static IALiCloudLogSourceAgent _SourceAgent = Container.Resolve<IALiCloudLogSourceAgent>() ?? new DefaultALiCloudLogSourceAgent();
        private const string EndPoint = "cn-shenzhen.log.aliyuncs.com";
        private const string AccesskeyId = "LTAI3RFZWmQdtkWU";
        private const string AccessKey = "CrEEHCzHk2wTt9cm5B3iqu2R6IeQLV";
        private const string Project = "hs-test";
        private const string Topic = "george_topic";
        private const string LogStore = "georgetest1";

        static ALiCloudLogTarget()
        {
            GoolgeProtobufCooker.ColdBind();
        }
    }
}

namespace HSFrameWork.Common
{
    public interface IALiCloudLogSourceAgent
    {
        string GetSource();
        string GetGameName();
    }

    public class DefaultALiCloudLogSourceAgent : IALiCloudLogSourceAgent
    {
        public string GetSource()
        {
            return "DummySource";
        }

        public string GetGameName()
        {
            return "DummyGame";
        }

    }
}

#endif
