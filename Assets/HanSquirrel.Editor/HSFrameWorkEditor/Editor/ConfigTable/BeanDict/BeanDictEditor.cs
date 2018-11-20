using System;
using SysTask = System.Threading.Tasks.Task;
using GLib;
using System.Collections.Generic;
using System.IO;

using BeanDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, HSFrameWork.ConfigTable.BaseBean>>;
using System.Threading;
using HSFrameWork.Common;
using HSFrameWork.ConfigTable.Editor;
using HSFrameWork.ConfigTable.Inner;

namespace HSFrameWork.ConfigTable.Editor.Impl
{
    /// <summary>
    /// 必须要用完后Dispose，否则ResourceManager和RunTimeData会一直被冻结，跨平台
    /// </summary>
    public sealed class BeanDictEditor : IDisposable
    {
        private BeanDictEditor()
        {
            RunTimeFrozenChecker.FrozenOne("打包数据");
        }

        public void Dispose()
        {
            RunTimeFrozenChecker.WarmOne();
        }

        public static BeanDictEditor Wrap(BeanDict beanDict)
        {
            var bd = new BeanDictEditor();
            bd._values = beanDict;
            return bd;
        }

        public static BeanDictEditor Create()
        {
            return new BeanDictEditor();
        }

        public BeanDictEditor Load(byte[] data)
        {
            _values = ProtoBufTools.Deserialize<BeanDict>(data);
            return this;
        }

        public BeanDictEditor Load(string binFile)
        {
            _values = ProtoBufTools.Deserialize<BeanDict>(File.ReadAllBytes(binFile));
            return this;
        }

        public BeanDictEditor Load(IBeanDictColdLoader loader, Action<string, float> onProgress)
        {
            return Load(loader, CancellationToken.None, onProgress);
        }

        public BeanDictEditor Load(IBeanDictColdLoader loader, CancellationToken token, Action<string, float> onProgress)
        {
            token.ThrowIfCancellationRequested();
            loader.LoadAll(_values, token, onProgress);
            return this;
        }

        public void Visit(Action<BeanDict> visitor)
        {
            visitor(_values);
        }

        public void Visit(Action<BaseBean, float, int, int> visitAction)
        {
            int i = 0;
            int c = Count;

            Mini.NewList(_values.Keys).SortC().ForEach(type =>
                Mini.NewList(_values[type].Keys).SortC()
                    .ForEach(pk =>
                    {
                        i++;
                        visitAction(_values[type][pk], (0.0f + i) / c, i, c);
                    }));
        }

        public int Count
        {
            get
            {
                int c = 0;
                foreach (var v in _values.Values)
                    c += v.Count;
                return c;
            }
        }

        BeanDict _values = new BeanDict();

        /// <summary>
        /// 将_values二进制存到文件里面
        /// </summary>
        public BeanDictEditor SaveBin(string binFile)
        {
            ProtoBufTools.Serialize(_values, binFile);
            return this;
        }

        /// <summary>
        /// 会额外处理GlobalTrigger，使用story+MD5而不是GUID作为KEY
        /// </summary>
        public BeanDictEditor SaveSortedBin(string binFile)
        {
            Dictionary<string, Dictionary<string, BaseBean>> sortedV = new Dictionary<string, Dictionary<string, BaseBean>>();
            EnumPojoSorted((k, v) =>
            {
                if (!sortedV.ContainsKey(k))
                    sortedV.Add(k, new Dictionary<string, BaseBean>());
                sortedV[k].Add(v.PK, v);
            });
            ProtoBufTools.Serialize(sortedV, binFile);
            return this;
        }

        private void EnumPojoSorted(Action<string, BaseBean> action)
        {
            Mini.NewList(_values.Keys).SortC(string.CompareOrdinal).ForEach(type =>
                Mini.NewList(_values[type].Keys).SortC(string.CompareOrdinal)
                    .ForEach(pk => action(type, _values[type][pk])));
        }

        /// <summary>
        /// 统计每个Type的Pojo个数
        /// </summary>
        public BeanDictEditor SortSaveSummary(string fileName)
        {
            if (fileName == null)
                return this;
            using (var tr = File.CreateText(fileName))
            {
                Mini.NewList(_values.Keys).SortC()
                    .ForEach(name => tr.WriteLine("{0} : {1}".Eat(name, _values[name].Count)));
            }
            return this;
        }

        /// <summary>
        /// 将数据字典排序存储为TXT(XML)或者HEX
        /// </summary>
        public BeanDictEditor SortSaveTXT(string xmlFileName, bool xmlOrHex)
        {
            if (xmlFileName == null)
                return this;

            using (var trXml = File.CreateText(xmlFileName))
            {
                EnumPojoSorted((t, v) =>
                {
                    try
                    {
                        trXml.WriteLine("{0}\t{1}\r\n{2}"
                       .Eat(t, v.PK, xmlOrHex ? ToolsShared.SerializeXML(v)
                                        : BitConverter.ToString(ProtoBufTools.Serialize(v, false))));
                    }
                    catch (Exception e)
                    {
                        HSUtils.LogException(e);
                        HSUtils.LogError("BeanDictEditor.SortSaveTXT 出现异常: {0}-{1}".Eat(t, v.PK));
                        //throw e;
                        //因为有时候会有非法字符在里面。不抛出异常可以在LOG里面看到有哪些非法字符。
                    }
                });
            }
            return this;
        }

        /// <summary>
        /// 加载数据文件，排序存储为TXT(XML)或者HEX；目标文件都可以为null，表示不生成。
        /// </summary>
        public static void LoadAndConvertToTxt(string src, string txtDst, string sumFile, string hexDst)
        {
            using (var bde = Create())
            {
                bde.Load(src).SortSaveTXT(txtDst, true)
                .SortSaveTXT(hexDst, false)
                .SortSaveSummary(sumFile);
            }
        }

        public static void LoadAndConvertVerbose(string title, string mainName)
        {
            LoadAndConvertVerbose(title, GetBinPath(mainName), mainName + "_converted");
        }

        public static void LoadAndConvertVerbose(string title, string src, string dstMainName)
        {
            using (var bde = Create())
            {
                MenuHelper.SafeDisplayProgressBar(title, "BeanDictEditor.Load", 0.1f);
                using (HSUtils.ExeTimer("BeanDictEditor.Load"))
                    bde.Load(src);

                bde.Save5Verbose(title, dstMainName, 0.3f, 1.0f, false, false, false, true);
            }
        }
        public static string GetXMLPath(string mainName)
        {
            return HSCTC.InDebug(mainName + "_Xml.txt");
        }

        public static string GetBinPath(string mainName)
        {
            return HSCTC.InDebug(mainName + "_bin.bin");
        }

        public static string GetSortedBinPath(string mainName)
        {
            return HSCTC.InDebug(mainName + "_bin_sorted.bin");
        }

        public BeanDictEditor Save5Verbose(string title, string mainName, float begin, float end, bool saveBin, bool saveSortedBin, bool saveHex, bool saveXML)
        {
            return Save5Verbose(title, mainName, begin, end, saveBin, saveSortedBin, saveHex, saveXML, null, null);
        }

        public BeanDictEditor Save5Verbose(string title, string mainName, float begin, float end, bool saveBin, bool saveSortedBin, bool saveHex, bool saveXML,
            CancellationTokenSource cts, Action<SysTask> showTaskChange)
        {
            if (saveBin)
            {
                MenuHelper.SafeDisplayProgressBar(title, "BeanDictEditor.SaveBin", begin);
                using (HSUtils.ExeTimer("BeanDictEditor.SaveBin"))
                    SaveBin(HSCTC.InDebug(mainName + "_bin.bin"));
            }

            if (saveSortedBin)
            {
                MenuHelper.SafeDisplayProgressBar(title, "BeanDictEditor.SaveSortedBin", begin + (end - begin) / 5);
                using (HSUtils.ExeTimer("BeanDictEditor.SaveSortedBin"))
                    SaveSortedBin(HSCTC.InDebug(mainName + "_bin_sorted.bin"));
            }

            if (saveHex)
            {
                MenuHelper.SafeDisplayProgressBar(title, "BeanDictEditor.SortSaveTXT-HEX", begin + 2 * (end - begin) / 5);
                using (HSUtils.ExeTimer("BeanDictEditor.SortSaveTXT-HEX"))
                    SortSaveTXT(HSCTC.InDebug(mainName + "_HEX.txt"), false);
            }

            if (saveXML)
            {

                MenuHelper.SafeDisplayProgressBar(title, "BeanDictEditor.SortSaveTXT-XML", begin + 3 * (end - begin) / 5);
                using (HSUtils.ExeTimer("BeanDictEditor.SortSaveTXT-XML"))
                    SortSaveTXT(HSCTC.InDebug(mainName + "_Xml.txt"), true);
            }

            MenuHelper.SafeDisplayProgressBar(title, "BeanDictEditor.SortSaveTXT-SUM", begin + 4 * (end - begin) / 5);
            using (HSUtils.ExeTimer("BeanDictEditor.SortSaveTXT-SUM"))
                SortSaveSummary(HSCTC.InDebug(mainName + "_sum.txt"));

            MenuHelper.SafeDisplayProgressBar(title, "BeanDictEditor.Save5Verbose结束", end);
            return this;
        }
    }
}
