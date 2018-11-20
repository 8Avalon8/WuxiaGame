using System;
using System.IO;
using GLib;
using System.Collections.Generic;
using System.Xml.Serialization;
using HSFrameWork.XLS2XML;
using System.Linq;
using System.Threading;
using SysTask = System.Threading.Tasks.Task;
using HSFrameWork.Common;
using BeanDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, HSFrameWork.ConfigTable.BaseBean>>;
using HanSquirrel.ResourceManager;
using HSFrameWork.ConfigTable.Editor;

namespace HSFrameWork.ConfigTable.Editor.Impl
{
    [XmlType("include")]
    public class WorkInclude
    {
        [XmlAttribute("file")]
        public string File { get; set; }
        public static List<WorkInclude> Load(string file)
        {
            return GLib.XMLUtils.LoadList<WorkInclude>(file);
        }
    }

    public class PartialXmlHelper
    {
        private string _ValueBundleXmlFileShort;
        public PartialXmlHelper(string valueBundleXmlFileShort)
        {
            _ValueBundleXmlFileShort = valueBundleXmlFileShort;
        }
        public string[] XmlFiles
        {
            get
            {
                var dict = new ConvertPairDict(HSCTC.ConvertPairFile);
                var files = WorkInclude.Load(HSCTC.ValueBundlePath.Sub(_ValueBundleXmlFileShort + ".xml"));
                foreach (var file in files)
                {
                    if (file.File.Ext().ToUpper() != ".XML")
                    {
                        try
                        {
                            file.File = dict.Xls2XmlDict[file.File];
                        }
                        catch (KeyNotFoundException)
                        {
                            throw new Exception("索引文件 [{0}] 中不存在数据包文件 [{1}] 中的 [{2}].".f(
                                HSCTC.ConvertPairFile, _ValueBundleXmlFileShort, file.File));
                        }
                    }
                }
                return files.Select(f => HSCTC.XmlPath.Sub(f.File)).ToArray();
            }
        }
    }

    public class PartialXMLBDLoader : XMLBDLoader
    {
        private PartialXmlHelper _Helper;
        /// <summary>
        /// new PartialXMLBDLoader("0")。不能添加".xml"。
        /// </summary>
        public PartialXMLBDLoader(string valueBundleXmlFileShort)
        {
            _Helper = new PartialXmlHelper(valueBundleXmlFileShort);
        }

        public override string[] XmlFiles { get { return _Helper.XmlFiles; } }
    }

    public class PartialXMLBDUpdater : XMLBDUpdater
    {
        private static Dictionary<string, PartialXMLBDUpdater> _Dict = new Dictionary<string, PartialXMLBDUpdater>();
        public static PartialXMLBDUpdater Get(string tag)
        {
            lock (_Dict)
            {
                PartialXMLBDUpdater ret;
                if (!_Dict.TryGetValue(tag, out ret))
                {
                    ret = new PartialXMLBDUpdater(tag);
                    _Dict.Add(tag, ret);
                }
                return ret;
            }
        }

        private PartialXmlHelper _Helper;
        /// <summary>
        /// new PartialXMLBDUpdater("0")。不能添加".xml"。
        /// </summary>
        protected PartialXMLBDUpdater(string valueBundleXmlFileShort)
        {
            _Helper = new PartialXmlHelper(valueBundleXmlFileShort);
        }

        /// <summary>
        /// HOT，NO CACHE
        /// </summary>
        public override string[] XmlFiles { get { return _Helper.XmlFiles; } }
    }

    public class ValueBundleHelper
    {
        public static void GenValueFileWithSummaryV1(bool force)
        {
            ValueBundleHelper.GenValueFileWithSummary("", force,
                new XMLBDLoader(), "2", HSCTC.ValuesFile, HSCTC.ValuesSummaryFile);
        }

        public static void GenValueFileWithSummaryV2(bool force, string tag)
        {
            GenValueFileWithSummaryV2("", force, tag);
        }

        public static void GenValueFileWithSummaryV2(string title, bool force, string tag)
        {
            ValueBundleHelper.GenValueFileWithSummary(title, force,
                new PartialXMLBDLoader(tag), "2_" + tag, HSCTC.ValuesFileV2.f(tag), HSCTC.ValuesSummaryFileV2.f(tag));
        }

        public static void GenValueFileWithSummary(string title, bool force, IXMLBDLoader loader, string minTag, string valueFile, string valueSummaryFile)
        {
            var xmlFiles = loader.XmlFiles;

            if ((!force) && FileSummary.PackedFileValid(xmlFiles, valueFile, valueSummaryFile))
            {
                HSUtils.Log("★★★ XML没有更新，因此不需要更新 {0} ★★★★", valueFile.ShortName());
            }
            else
            {
                HSUtils.Log("★★★ 有XML更新，重新生成 {0} ★★★★", valueFile.ShortName());
                using (var bde = BeanDictEditor.Create())
                {
                    MenuHelper.SafeDisplayProgressBar(title, "加载XML", 0.3f);
                    using (HSUtils.ExeTimer("BeanDictEditor.Load"))
                        bde.Load(loader, CancellationToken.None,
                            (s, p) => MenuHelper.SafeDisplayProgressBar("加载XML", s, p));

                    bde.Save5Verbose(title, minTag, 0.6f, 0.8f, true, false, false, false);
                    File.Copy(BeanDictEditor.GetBinPath(minTag), valueFile, true);
                    FileSummary.WriteSummary(xmlFiles, valueFile, valueSummaryFile);
                }
            }
        }

        public static void GenValueCEFileV1()
        {
            GenValueCEFile("", HSCTC.ActiveValueFile, HSCTC.CEValues);
        }

        public static void GenValueCEFileV2(string tag)
        {
            GenValueCEFile("", HSCTC.ActiveValueFileV2.f(tag), HSCTC.CEValuesV2.f(tag));
        }

        public static void GenValueCEFile(string title, string valueFile, string ceValueFile)
        {
            HSUtils.Log("★★★ [{0}] 有更新，重新生成 [{1}] ★★★★", valueFile.ShortName(), ceValueFile.ShortName());
            MenuHelper.SafeDisplayProgressBar(title, "压缩加密 {0}".f(valueFile.ShortName()), 0.8f);
            File.Delete(ceValueFile);

            switch (HSCTC.ActiveCompressMode)
            {
                case HSCTC.CompressMode.LZMA_TDES_RAW:
                    HSPackToolRaw.LZMA_3DES(valueFile, ceValueFile);
                    ProtoBufTools.Serialize(HSCTC.CompressMode.LZMA_TDES_RAW, HSCTC.LastCompressMode);
                    break;
                case HSCTC.CompressMode.HS_IONIC_ZIP_EASYDES:
                    HSPackToolEx.IonicZip_EasyDes(valueFile, ceValueFile);
                    ProtoBufTools.Serialize(HSCTC.CompressMode.HS_IONIC_ZIP_EASYDES, HSCTC.LastCompressMode);
                    break;
                case HSCTC.CompressMode.HS_LZMA_EASYDES:
                    HSPackToolEx.HSLZMA_EasyDes(valueFile, ceValueFile);
                    ProtoBufTools.Serialize(HSCTC.CompressMode.HS_LZMA_EASYDES, HSCTC.LastCompressMode);
                    break;
                default:
                    throw new NotImplementedException("没有实现该种压缩方式：[{0}]".Eat(HSCTC.ActiveCompressMode));
            }
        }
    }

    public class ValueLoadHelper
    {
        public static byte[] LoadConfigTableData()
        {
            return HSCTC.CEValues.ReadAllBytes();
        }

        public static byte[] LoadConfigTableDataV2(string tag)
        {
            return HSCTC.CEValuesV2.f(tag).ReadAllBytes();
        }

        public static void ResourceManagerLoadDesignModeDelegate(BeanDict beanDict)
        {
            beanDict.Clear();
            XMLBDUpdater.Instance.Reset();
            XMLBDUpdater.Instance.UpdateChanged(beanDict, null);
        }

        public static void ResourceManagerLoadDesignModeDelegateV2(string tag, BeanDict beanDict)
        {
            beanDict.Clear();
            PartialXMLBDUpdater.Get(tag).Reset();
            PartialXMLBDUpdater.Get(tag).UpdateChanged(beanDict, null);
        }
    }
}
