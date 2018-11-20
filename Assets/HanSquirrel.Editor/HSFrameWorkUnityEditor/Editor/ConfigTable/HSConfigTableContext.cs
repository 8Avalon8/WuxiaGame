using UnityEngine;
using System.IO;
using HSFrameWork.XLS2XML;
using System;
using GLib;
using HSFrameWork.Common.Editor;
using HSFrameWork.Common;
using System.Linq;
using System.Collections.Generic;
using AiUnity.NLog.Core.LayoutRenderers;
using HanSquirrel.ResourceManager;

namespace HSFrameWork.ConfigTable.Editor
{
    using Inner;
    /// <summary>
    /// HSConfigTableContext（上下文）
    /// </summary>
    public class HSCTC : HSCTCAbstract
    {
        /// <summary>
        /// 当前工程最新重载时间（Play或者重新编译等都会引起这个时间重置）
        /// </summary>
        public static DateTime CurrentGameStartTime;

        private static void OnPlayModeChanged(PlayModeState currentState, PlayModeState changedState)
        {
            if (currentState == PlayModeState.Stopped && changedState == PlayModeState.Playing)
            {
                CurrentGameStartTime = DateTime.Now;
            }

            if (changedState == PlayModeState.Stopped)
            {
                Container.HideResolveException = true;
            }
        }

        /// <summary> 是否是打包机模式 </summary>
        public static bool IsRobotMode = false;

        /// <summary>
        /// Project/Assets/
        /// </summary>
        public static readonly string AppDataPath = Application.dataPath;
        /// <summary> Project根目录 </summary>
        public static readonly string AppPath = AppDataPath.Sub("..");
        /// <summary>
        /// Project/Assets/StreamingAssets
        /// </summary>
        public static readonly string StreamingAssetsPath = AppDataPath.Sub("StreamingAssets/");
        /// <summary> 工程根目录/Assets/Resources/ </summary>
        public static readonly string ResourcesPath = AppDataPath.Sub("Resources/");
        /// <summary>
        /// HSFrameWork动态生成的资源的输出路径。
        /// 缺省为 "工程根目录/Assets/BuildSource/NO_SVN_HanSqurrel"
        /// </summary>
        public static readonly string ResourceOutputFolder = AppPath.Sub(HSUnityEnv.ResourceOutputFolder);

        /// <summary> 配置表二进制文件的路径。缺省为 工程根目录/Assets/StreamingAssets/hsframework_cevalues.bytes </summary>
        public static readonly string CEValues = AppPath.Sub(HSUnityEnv.CEValuesPath);

        /// <summary> 配置表二进制文件子包的路径格式。缺省为 工程根目录/Assets/StreamingAssets/hsframework_cevalues_{0}.bytes </summary>
        public static readonly string CEValuesV2 = AppPath.Sub(HSUnityEnv.CEValuesV2Path);

        /// <summary>
        /// 传统的 REG文件所在目录。
        /// </summary>
        public static readonly string RegPath0 = AppPath.Sub("/reg/");
        /// <summary>
        /// 新的 REG文件所在目录。
        /// </summary>
        public static readonly string RegPath1 = AppPath.Sub("/data/reg/");
        /// <summary>
        /// V2版本的REG文件所在目录。
        /// </summary>
        public static readonly string RegV2Path = AppPath.Sub("/data/regV2/");
        /// <summary>
        /// 子包定义文件所在目录。
        /// </summary>
        public static readonly string ValueBundlePath = AppPath.Sub("/data/valuebundle/");
        /// <summary>
        /// XLS和REGV2对应关系索引文件。
        /// </summary>
        public static readonly string ConvertPairFile = AppPath.Sub("/data/valuebundle/INDEX.xml");

        /// <summary>
        /// 热更包输出目录，将要被拷贝到服务器上。
        /// </summary>
        public static readonly string HotFixFolder = AppPath.Sub("/ABMD5/HotFix");

        /// <summary>
        /// StreamingAsssets下所有AB包的MD5文件路径
        /// </summary>
        public static readonly string Md5File = AppDataPath.StandardSub("Resources/ABMD5.txt");

        /// <summary>
        /// HotFix的原始版本的Ab包MD5文件在不同平台下的路径
        /// </summary>
        public static readonly string Md5FileByPlatform =
#if UNITY_ANDROID
            AppPath.Sub("ABMD5/AndroidABMD5.txt");
#elif UNITY_IOS
            AppPath.Sub("ABMD5/IOSABMD5.txt");
#else
            AppPath.Sub("ABMD5/WIN.txt");
#endif

        /// <summary>
        /// 传统的Excel所在目录
        /// </summary>
        public static readonly string ExcelPath0 = AppPath.Sub("/excel/");
        /// <summary>
        /// 新版本的Excel所在目录
        /// </summary>
        public static readonly string ExcelPath1 = AppPath.Sub("/data/excel/");
        /// <summary>
        /// EXCEL转换的XML的输出目录
        /// </summary>
        public static readonly string XmlPath = AppPath.Sub("/data/Scripts/");
        /// <summary>
        /// HSFrameWork的配置文件所在目录
        /// </summary>
        public static readonly string ConfigPath = AppPath.Sub("/data/HSConfigTable/");

        /// <summary>
        /// HSFrameWork的调试临时文件所在目录
        /// </summary>
        public static readonly string DebugPath = AppPath.Sub("/data/Debug/");
        /// <summary>
        /// 语言包相关资源所在目录
        /// </summary>
        public static readonly string TranslatePath = AppPath.Sub("/data/Translate/");

        /// <summary>
        /// HSFrameWork内部使用的缓存目录
        /// </summary>
        public static readonly string CachePath = AppPath.Sub("/data/Cache/HSFrameWork/HSConfigTable/");

        /// <summary>
        /// 二进制的配置表数据文件路径
        /// </summary>
        public static readonly string ValuesFile = AppPath.Sub("/data/data/values.bytes");
        /// <summary>
        /// 二进制的子包配置表数据文件路径格式
        /// </summary>
        public static readonly string ValuesFileV2 = AppPath.Sub("/data/data/values_{0}.bytes");

        /// <summary>
        /// HSFrameWork的配置信息帮助文件路径
        /// </summary>
        public static readonly string HelpFile = AppDataPath.Sub(@"HanSquirrel.Editor/HSFrameWorkUnityEditor/Editor/ConfigTable/readme.txt");

        /// <summary>
        /// 配置文件：是否使用传统的LZMA3DES格式来压缩加密二进制配置表数据
        /// </summary>
        public static readonly string UseLegendLZMA3DESRaw = ConfigPath.Sub("uselegend");
        /// <summary>
        /// 配置文件：是否使用ZIP/Easy3DES格式来压缩加密二进制配置表数据
        /// </summary>
        public static readonly string UseZipTagFile = ConfigPath.Sub("usezip");
        /// <summary>
        /// 配置文件：是否禁止多线程
        /// </summary>
        public static readonly string NoThreadExtentionTagFile = ConfigPath.Sub("single_thread");
        /// <summary>
        /// 配置文件：是否用XML生成二进制配置表文件过程中缓存中间结果。
        /// </summary>
        public static readonly string UseXMLObjTagFile = ConfigPath.Sub("use_xml_obj");
        /// <summary>
        /// 配置文件：是否将用户自行编辑的XML文件视作过期。
        /// </summary>
        public static readonly string XMLUserModifyAsExpiredTagFile = ConfigPath.Sub("xml_user_modify_as_expired");
        /// <summary>
        /// 配置文件：XLS转换XML过程中是否显示详细调试信息。
        /// </summary>
        public static readonly string XMLConvertVerboseTagFile = ConfigPath.Sub("xml_convert_verbose");
        /// <summary>
        /// 配置文件：最大并发线程
        /// </summary>
        public static readonly string MaxDegreeOfParallelismTagFile = ConfigPath.Sub("parallel_level.txt");
        /// <summary>
        /// 配置文件：当前语言
        /// </summary>
        public static readonly string LanguageCodeFile = ConfigPath.Sub("language.txt");
        /// <summary>
        /// 配置文件：生成语言包的时候是否保留老版本的输出文件
        /// </summary>
        public static readonly string KeepTransOldOutputTagFile = ConfigPath.Sub("keeptransoldoutput");
        /// <summary>
        /// 配置文件：打包的时候是否跳过最后的打AB包过程
        /// </summary>
        public static readonly string SkipBuildCurrentTagFile = ConfigPath.Sub("skipbuildcurrent");
        /// <summary>
        /// 配置文件：打包的时候是否输出详细日志
        /// </summary>
        public static readonly string DumpABDepLog = ConfigPath.Sub("DumpPrefabDepLog");
        /// <summary>
        /// 配置文件：游戏运行时是否强制测试HotPatch功能
        /// </summary>
        public static readonly string ForceHotPatchTestInAppTagFile = ConfigPath.Sub("hotpatchtestinapp");

        /// <summary>
        /// 当前打包的语言输出文件路径。最终用于终端判断当前语言。
        /// </summary>
        public static readonly string OutputLanguageCodeFile = ResourcesPath.Sub("language.txt");
        /// <summary>
        /// 生成语言包翻译文件过程中是否生成详细的XML文件。
        /// </summary>
        public static readonly string GenLanguagePackVerboseXMLTagFile = ConfigPath.Sub("gen_language_pack_verbose_xml");

        /// <summary>
        /// XML转换为配置表二进制的中间缓存路径
        /// </summary>
        public static readonly string XMLObjPath = CachePath.Sub("xmlobj/");
        /// <summary>
        /// XLS转换为XML的中间缓存路径
        /// </summary>
        public static readonly string XmlTSPath = XmlPath.Sub("_ts");
        /// <summary>
        /// 上一次的压缩方式存储路径
        /// </summary>
        public static readonly string LastCompressMode = CachePath.Sub("compressmode");

        /// <summary>
        /// 上次的语言代码存储路径
        /// </summary>
        public static readonly string LastLanCode = CachePath.Sub("lancode");

        /// <summary>
        /// 二进制配置表的统计数据文件路径
        /// </summary>
        public static readonly string ValuesSummaryFile = CachePath.Sub("values_summary");
        /// <summary>
        /// 子包二进制配置表的统计数据文件路径格式
        /// </summary>
        public static readonly string ValuesSummaryFileV2 = CachePath.Sub("values_summary_{0}");

        /// <summary>
        /// 返回一个有效的在Debug目录的全路径
        /// </summary>
        public static string InDebug(string shortFileName)
        {
            return DebugPath.Sub(shortFileName);
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        public static void ColdBind()
        {
            CurrentGameStartTime = DateTime.Now;
            EditorPlayMode.PlayModeChanged += OnPlayModeChanged;

            Directory.CreateDirectory(StreamingAssetsPath);
            Directory.CreateDirectory(DebugPath);
            Directory.CreateDirectory(XmlPath);
            Directory.CreateDirectory(XMLObjPath);
            Directory.CreateDirectory(ConfigPath);
            Directory.CreateDirectory(CachePath);
            Directory.CreateDirectory(ResourceOutputFolder);

            ConvertorHelper.Config(ConvertPairFile.ExistsAsFile() ? RegV2Path : RegPath, ExcelPath, XmlPath, HSUtils.Log, x => HSUtils.LogWarning(x), HSUtils.LogException, 8);
            ConvertorHelper.V2IndexFile = ConvertPairFile.ExistsAsFile() ? ConvertPairFile : null;

            ConvertorHelper.BeforeStart += () =>
            {
                ConvertorHelper.MaxDegreeOfParallelism = MaxDegreeOfParallelism;
                ConvertorHelper.UserModifyAsExpired = XMLUserModifyAsExpired;
                ConvertorHelper.Verbose = XMLConverterVerbose;
            };
        }

        /// <summary>
        /// 只读。V2版本的Reg文件系统是否启用
        /// </summary>
        public static bool RegV2Active
        {
            get
            {
                return ConvertPairFile.ExistsAsFile();
            }
        }

        /// <summary>
        /// 只读，从ValueBundlePath下面取得除了INDEX.XML之外的其他 *.xml的文件名（不包含后缀）
        /// </summary>
        public static IEnumerable<string> ValueBundleTags
        {
            get
            {
                if (!ValueBundlePath.ExistsAsFolder())
                    return new List<string>();
                else
                    return new DirectoryInfo(ValueBundlePath).GetFiles("*.xml")
                        .Select(fi => fi.Name.NameWithoutExt())
                        .Where(t => t.ToUpper() != "INDEX");
            }
        }

        /// <summary>
        /// 只读。遍历V2版本的二进制配置表文件
        /// </summary>
        public static IEnumerable<string> ValueFileV2s
        {
            get
            {
                return ValueBundleTags.Select(t => ValuesFileV2.f(t));
            }
        }

        /// <summary>
        /// 只读。遍历V2版本的二进制配置表加密压缩文件
        /// </summary>
        public static IEnumerable<string> CEValuesV2s
        {
            get
            {
                return ValueBundleTags.Select(t => CEValuesV2.f(t));
            }
        }

        /// <summary>
        /// 只读，包括hsframework_cevalues.bytes hsframework_cevalues_{0}.bytes hsframework_lua.bytes hsframework_filter.bytes 所在的AB包。
        /// </summary>
        public static IEnumerable<string> AllCEABNames
        {
            get
            {
                return ValueBundleTags.Select(x => HSUnityEnv.CEValuesV2ABName.f(x))
                    .Union(new string[]
                    { HSUnityEnv.CEValuesABName, HSUnityEnv.CELuaABName, HSUnityEnv.CEFilterABName });
            }
        }

        /// <summary>
        /// 只读。在XML生成二进制配置表时是否使用中间缓存
        /// </summary>
        public static bool UseXMLObj
        {
            get
            {
                return File.Exists(UseXMLObjTagFile);
            }
        }

        /// <summary>
        /// 只读。当前的二进制配置文件压缩加密方式
        /// </summary>
        public static CompressMode ActiveCompressMode
        {
            get
            {
                if (File.Exists(UseZipTagFile))
                {
                    HSUtils.Log("使用HSIonic.Zib_EasyDes来压缩Value数据");
                    return CompressMode.HS_IONIC_ZIP_EASYDES;
                }
                else if (File.Exists(UseLegendLZMA3DESRaw))
                {
                    HSUtils.Log("使用传统LZMA和3DES来压缩Value数据");
                    return CompressMode.LZMA_TDES_RAW;
                }
                else
                {
                    HSUtils.Log("使用HSLZMA_EasyDes来压缩Value数据");
                    return CompressMode.HS_LZMA_EASYDES;
                }
            }
        }

        /// <summary>
        /// 只读。是否保留旧版本的语言包翻译任务文件。
        /// </summary>
        public static bool KeepTransOldOutput
        {
            get
            {
                return KeepTransOldOutputTagFile.ExistsAsFile();
            }
        }

        /// <summary>
        /// 只读，是否跳过真正的AB包打包。开发测试时使用，因为AssetTool.BuildCurrent实在太慢啦。
        /// </summary>
        public static bool SkipBuildCurrent
        {
            get
            {
                return SkipBuildCurrentTagFile.ExistsAsFile();
            }
        }

        /// <summary>
        /// 只读，当前语言
        /// </summary>
        public static string ActiveLanguage
        {
            get
            {
                if (LanguageCodeFile.Exists())
                {
                    string code = File.ReadAllText(LanguageCodeFile).Trim();
                    if (code.Visible())
                        return code;
                }
                return null;
            }
            set
            {
                if (value == null)
                    LanguageCodeFile.Delete();
                else
                    File.WriteAllText(LanguageCodeFile, value);
            }
        }

        /// <summary>
        /// 只读，用于显示的当前语言包
        /// </summary>
        public static string DisplayActiveLanguage
        {
            get
            {
                string c = ActiveLanguage;
                return c == null ? "中文简体语言包" : c;
            }
        }

        /// <summary>
        /// 只读，当前实际所用的REG目录
        /// </summary>
        public static string RegPath
        {
            get
            {
                return RegPath0.Exists() ? RegPath0 : RegPath1;
            }
        }

        /// <summary>
        /// 只读，当前实际所用的EXCEL目录
        /// </summary>
        public static string ExcelPath
        {
            get
            {
                return ExcelPath0.Exists() ? ExcelPath0 : ExcelPath1;
            }
        }

        /// <summary>
        /// 只读。当前所支持的所有语言。
        /// </summary>
        public static List<String> AllLanguages
        {
            get
            {
                if (!TranslatePath.ExistsAsFolder())
                    return new List<string>();
                else
                    return new DirectoryInfo(TranslatePath).GetDirectories("*", SearchOption.TopDirectoryOnly).Where(di => di.Name != "default").Select(di => di.Name).ToList();
            }
        }

        /// <summary>
        /// 只读。当前使用的语言包目录。如果没有，则返回null
        /// </summary>
        public static string ActiveTranslateDir
        {
            get
            {
                string code = ActiveLanguage;
                return code == null ? null : TranslatePath.Sub(code).CreateDir();
            }
        }

        /// <summary>
        /// 只读。当前使用的语言包输出目录。如果没有，则返回null
        /// </summary>
        public static string ActiveTranslateOutputDir
        {
            get
            {
                string code = ActiveLanguage;
                return code == null ? null : TranslatePath.Sub(code).Sub("Output").CreateDir();
            }
        }


        /// <summary>
        /// 只读。当前使用的语言包共享字典目录。如果没有，则返回null
        /// </summary>
        public static string ActiveTranslateSharedDir
        {
            get
            {
                string code = ActiveLanguage;
                return code == null ? null : TranslatePath.Sub(code).Sub("shared").CreateDir();
            }
        }

        /// <summary>
        /// 只读。当前使用的语言包字体目录。如果没有，则返回null
        /// </summary>
        public static string ActiveFontDir
        {
            get
            {
                string code = ActiveLanguage;
                return TranslatePath.Sub(code == null ? "default" : code).Sub("font").CreateDir();
            }
        }

        /// <summary>
        /// 只读。当前的语言包目录。如果没有语言包，也会返回语言包根目录下的default目录。
        /// </summary>
        public static string ActiveLanguageDir
        {
            get
            {
                string code = ActiveLanguage;
                return TranslatePath.Sub(code == null ? "default" : code).CreateDir();
            }
        }

        /// <summary>
        /// 当前有效的二进制配置表文件路径。
        /// </summary>
        public static string ActiveValueFile
        {
            get
            {
                string code = ActiveLanguage;
                return code == null ? ValuesFile : ValuesFile + "." + code;
            }
        }

        /// <summary>
        /// 当前有效的子包二进制配置表文件路径格式。
        /// </summary>
        public static string ActiveValueFileV2
        {
            get
            {
                string code = ActiveLanguage;
                return code == null ? ValuesFileV2 : ValuesFileV2 + "." + code;
            }
        }


        /// <summary>
        /// 当前的二进制配置表文件的统计信息文件路径
        /// </summary>
        public static string ActiveValueSummaryFile
        {
            get
            {
                string code = ActiveLanguage;
                return code == null ? ValuesSummaryFile : ValuesSummaryFile + "." + code;
            }
        }

        /// <summary>
        /// 当前的子包二进制配置表文件的统计信息文件路径格式
        /// </summary>
        public static string ActiveValueSummaryFileV2
        {
            get
            {
                string code = ActiveLanguage;
                return code == null ? ValuesSummaryFileV2 : ValuesSummaryFileV2 + "." + code;
            }
        }

        /// <summary>
        /// 只读。语言包功能是否生成详细的XML文件。
        /// </summary>
        public static bool GenLanguagePackVerboseXML { get { return File.Exists(GenLanguagePackVerboseXMLTagFile); } }

        /// <summary>
        /// 只读。是否将用户修改过的XML当作过期。
        /// </summary>
        public static bool XMLUserModifyAsExpired { get { return File.Exists(XMLUserModifyAsExpiredTagFile); } }

        /// <summary>
        /// 只读。是否在XLS转换为XML过程中输出详细信息。
        /// </summary>
        public static bool XMLConverterVerbose { get { return File.Exists(XMLConvertVerboseTagFile); } }

        /// <summary>
        /// 只读。当前并发参数。
        /// </summary>
        public static int MaxDegreeOfParallelism
        {
            get
            {
                try
                {
                    return Math.Min(16, Math.Max(4, int.Parse(File.ReadAllLines(MaxDegreeOfParallelismTagFile)[0])));
                }
                catch
                {
                    if (MaxDegreeOfParallelismTagFile.Exists())
                    {
                        Debug.LogWarning("[{0}]格式错误。".Eat(MaxDegreeOfParallelismTagFile));
                    }
                    return 8;
                }
            }
        }

        /// <summary>
        /// 删除所有的二进制配置文件。
        /// </summary>
        public static void ClearValuesOnly()
        {
            LastCompressMode.Delete();
            ValuesSummaryFile.Delete();
            ValuesFile.Delete();
            ValueFileV2s.ForEachG(f => f.Delete());
        }

        /// <summary>
        /// 删除所有的加密压缩二进制配置文件。
        /// </summary>
        public static void ClearAllCEValues()
        {
            LastCompressMode.Delete();
            ValuesSummaryFile.Delete();

            ValuesFile.Delete();
            CEValues.DeleteWithMeta();

            ValueFileV2s.ForEachG(f => f.Delete());
            CEValuesV2s.ForEachG(f => f.DeleteWithMeta());

            HSUnityEnv.CELuaPath.DeleteWithMeta();
            HSUnityEnv.CEFilterPath.DeleteWithMeta();

            AllCEABNames.ForEachG(x => StreamingAssetsPath.Sub(x).DeleteAsABFile());
        }

        /// <summary>
        /// 删除所有目标文件和中间缓存文件。
        /// </summary>
        public static void ClearAllCacheAndDstFiles()
        {
            Mini.ClearDirectory(XmlPath);
            Mini.ClearDirectory(XmlTSPath);
            Mini.ClearDirectory(CachePath);
            Mini.ClearDirectory(XMLObjPath);
            Mini.ClearDirectory(DebugPath);

            try
            {
                LogPathLayoutRendererGG.LogPath.ClearDirectory();
            }
            catch
            {
            }

            HotPatch.ClearAllHotPatchFiles();
            ClearAllCEValues();
            HSUnityEnv.ResourceABIndexFile.DeleteWithMeta();
            StreamingAssetsPath.Sub(HSUnityEnv.ResourceABIndexABName).DeleteAsABFile();

            var languageBK = ActiveLanguage;
            using (DisposeHelper.Create(() => { ActiveLanguage = languageBK; })) //异常的时候也会恢复
                AllLanguages.ForEach(lan =>
                {
                    ActiveLanguage = lan;
                    ActiveValueFile.Delete();
                    ActiveValueSummaryFile.Delete();
                    Mini.ClearDirectory(ActiveTranslateOutputDir);
                });
        }


    }
}
