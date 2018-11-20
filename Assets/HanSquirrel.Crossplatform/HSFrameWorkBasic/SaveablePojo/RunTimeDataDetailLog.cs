using GLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HSFrameWork.Common;
using HSFrameWork.Common.Inner;

namespace HSFrameWork.SPojo.TestCase
{
    /// <summary>
    /// Saveable运行时的监控
    /// </summary>
    public static class RunTimeDataDetailLog
    {
        private static string DataPath;

        public static string GetFileInDataPath(string name)
        {
            return DataPath.StandardSub("{0}{1}".Eat(CurrentGameStartTimeStr, name));
        }

        //找不到新老不同的用例。
#if HSFRAMEWORK_RUN_IN_MS_CONSOLE
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
        private static void SaveLoaderV0Diff(string[] invalidKeys)
        {
            using (var sw = File.AppendText(RuntimeDataFile))
            {
                WriteTitle(sw, "原始版本加载后剩余的");
                if (invalidKeys == null)
                {
                    sw.WriteLine("没有剩余");
                }
                else
                {
                    foreach (var s in invalidKeys)
                        sw.WriteLine(s);
                }
            }
        }

        public static void Init(string dataPath, bool enabled)
        {
            DataPath = dataPath;
            Directory.CreateDirectory(DataPath);
            ResetFileNames();

            if (!enabled) return;

            ExeTimer.Disabled = false;
            ExeTimerSum.Disabled = false;

            HSUtils.LogWarning("详细的RuntimeData提交信息被记录在[data/debug]。如果想要关闭，请删除 [data/HSConfigTable/runtimedatalog] 文件。");

            Saveable.DebugFacade.ArchiveLoaderConfiger.CompareWithMethodV0 = false;
            Saveable.DebugFacade.ArchiveLoaderConfiger.CreateSeqDebugFile = GetFileInDataPath("newSeq.txt");
            Saveable.DebugFacade.ArchiveLoaderConfiger.ServerDataAfterLoad = (memo, root, data, winfo) => SaveRuntimeData("{0}加载后剩余的数据，共 [{1}]个。".Eat(memo, data.Count), null, data, root, winfo);
            Saveable.DebugFacade.ArchiveLoaderConfiger.OnDiffFromV0 = SaveLoaderV0Diff;
            Saveable.DebugFacade.EnableSubmitLog(data => data.toJsonG().DecodeFromMiniJsonOutput(false));
            Saveable.DebugFacade.SubmitRuntimeAction = (seq, skipped, data, logs) =>
            {
                string title = Saveable.DebugFacade.GetSubmitStatics();
                SaveRuntimeData(title, null, data, null, null);
                if (logs.Count != 0)
                {
                    using (var sw = File.AppendText(SubmitLogFile))
                    {
                        WriteTitle(sw, title);
                        foreach (var line in logs)
                            sw.WriteLine(line);
                    }
                }
            };

        }
        #region 初始化和小函数

        public static void ResetFileNames()
        {
            CurrentGameStartTime = DateTime.Now;
            CurrentGameStartTimeStr = CurrentGameStartTime.ToString("MMdd-HHmm.ss-");
            RawDataFile = GetFileInDataPath("raw.txt");
            WarningFile = GetFileInDataPath("waning.txt");
            RuntimeDataFile = GetFileInDataPath("data.txt");
            RuntimeDataTreeFile = GetFileInDataPath("dataEx.txt");
            RuntimeDataSummaryFile = GetFileInDataPath("sum.txt");
            SubmitLogFile = GetFileInDataPath("submitLog.txt");
        }

        public static DateTime CurrentGameStartTime { get; private set; }
        public static string CurrentGameStartTimeStr { get; private set; }
        private static string RawDataFile;
        private static string WarningFile;
        private static string RuntimeDataFile;
        private static string RuntimeDataTreeFile;
        private static string RuntimeDataSummaryFile;
        private static string SubmitLogFile;

        private static void WriteTitle(StreamWriter sw, string title)
        {
            sw.WriteLine();
            sw.WriteLine(new string('_', 10) + Mini.NowShort + new string('_', 10) + title + new string('_', 10));
        }
        #endregion

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private static void SaveOrgJsonIf(string orgJson)
        {
            if (orgJson != null)
                using (var sw = File.AppendText(RawDataFile))
                {
                    //WriteTitle(sw, title);
                    sw.WriteLine(orgJson);
                }
        }

        public static void SaveRuntimeData(string title, string orgJson, Hashtable data, Saveable root, IEnumerable<string> wInfo)
        {
            SaveOrgJsonIf(orgJson);

            if (wInfo != null)
                using (var sw = File.AppendText(WarningFile))
                {
                    WriteTitle(sw, title);
                    foreach (var w in wInfo)
                        sw.WriteLine(w);
                }

            Dictionary<string, int> sumDict = new Dictionary<string, int>();
            foreach (var classname in data.Keys.OfType<string>().Select(Saveable.DebugFacade.GetClassName).Where(StringExt.Visible))
            {
                if (sumDict.ContainsKey(classname))
                    sumDict[classname]++;
                else
                    sumDict[classname] = 1;
            }

            using (var sw = File.AppendText(RuntimeDataSummaryFile))
            {
                WriteTitle(sw, title + " [按照类名排序] ");

                foreach (var key in sumDict.Keys.ToList().SortC())
                {
                    sw.WriteLine("{0} : [{1}]".Eat(key, sumDict[key]));
                }

                WriteTitle(sw, title + " [按照个数排序] ");
                foreach (var kv in sumDict.ToList().SortC((a, b) => b.Value.CompareTo(a.Value)))
                {
                    sw.WriteLine("{0} : [{1}]".Eat(kv.Key, kv.Value));
                }
            }

            using (var sw = File.AppendText(RuntimeDataFile))
            {
                WriteTitle(sw, title);
                sw.WriteLine(data.toJsonG().DecodeFromMiniJsonOutput(false));
            }

            if (root != null)
            {
                using (HSUtils.ExeTimer("GenRuntimeDataTreeFile"))
                {
                    using (var sw = File.AppendText(RuntimeDataTreeFile))
                    {
                        HSUtils.Log("将RuntimeData结构化存储为 [{0}]", RuntimeDataTreeFile);
                        WriteTitle(sw, title + "结构化的输出");
                        sw.WriteLine(root.Dump());
                    }
                }
            }
        }
    }
}