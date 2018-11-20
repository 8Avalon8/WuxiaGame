namespace HSFrameWork.Common
{
    using GLib;
    using UnityEngine;

    /// <summary>
    /// 一些Unity环境下的静态配置类型变量
    /// </summary>
    public class HSUnityEnv
    {
        public static readonly string ProductName = Application.productName;

        public const string CurrentBuildTarget =
#if UNITY_STANDALONE_WIN
            "win";
#elif UNITY_ANDROID
            "android";
#elif UNITY_IPHONE
            "ios";
#elif UNITY_STANDALONE_OSX
            "ios";
#endif

        /// <summary>
        /// 在Resources目录下面的标记
        /// </summary>
        public const string ForceHotPatchTestInAppTagPath = "HSFrameWork/hotpatchtestinapp.txt";
        public const string ForceHotPatchTestInAppTagKey = "HSFrameWork/hotpatchtestinapp";
        public const string StreamingAssetsFolder = "Assets/StreamingAssets";
        public const string ResourceOutputFolder = "Assets/BuildSource/NO_SVN_HanSqurrel";

        public const string ResourceABIndexFile = ResourceOutputFolder + "/resourceindex.bytes";
        public const string ResourceABIndexABName = "hsframework_resourceabindex";

#if HSFRAMEWORK_VALUES_IN_AB

        //因为Values的生成是在HSFrameWork内部，因此这些必须定义在HSFrameWork内部。
        //AB包名字在Unity里面最终都会小些，直接小些省去可能的BUG。
        public const string CEValuesABName = "hsframework_cevalues";
        public const string CEValuesV2ABName = "hsframework_cevalues_{0}";
        public const string CELuaABName = "hsframework_lua";
        public const string CEFilterABName = "hsframework_filter";

        public const string CEValuesPath = ResourceOutputFolder + "/cevalues.bytes";
        public const string CEValuesV2Path = ResourceOutputFolder + "/cevalues_{0}.bytes";
        public const string CELuaPath = ResourceOutputFolder + "/celua.bytes";
        public const string CEFilterPath = ResourceOutputFolder + "/cefilter.bytes";
#else
        public const string CEValuesABName = "hsframework_cevalues.bytes";
        public const string CEValuesV2ABName = "hsframework_cevalues_{0}.bytes";
        public const string CELuaABName = "hsframework_lua.bytes";
        public const string CEFilterABName = "hsframework_filter.bytes";

        public const string CEValuesPath = StreamingAssetsFolder + "/hsframework_cevalues.bytes";
        public const string CEValuesV2Path = StreamingAssetsFolder + "/hsframework_cevalues_{0}.bytes";
        public const string CELuaPath = StreamingAssetsFolder + "/hsframework_lua.bytes";
        public const string CEFilterPath = StreamingAssetsFolder + "/hsframework_filter.bytes";
#endif
        /// <summary>
        /// dir不可以以 / 结束。文件可以以 / 或者 \ 开始。
        /// </summary>
        public static string EasySub(string dir, string file)
        {
            if (file.StartsWith("/"))
                return dir + file;
            else if (file.StartsWith(@"\"))
                return dir + "/" + file.Substring(1);
            else
                return dir + "/" + file;
        }

        /// <summary>
        /// 文件可以以 / 或者 \ 开始。
        /// </summary>
        public static string InPersistentDataFolder(string file)
        {
            return EasySub(PersistentDataPath, file);
        }

        /// <summary>
        /// 文件可以以 / 或者 \ 开始。
        /// </summary>
        public static string InStreamingAssetsFolder(string file)
        {
            return EasySub(StreamingAssetsPath, file);
        }

        /// <summary>
        /// 文件可以以 / 或者 \ 开始。
        /// </summary>
        public static string InHotPatchFolder(string file)
        {
            return HotPatchFolder.Sub(file);
        }

        public static readonly string LastHotPatchTestCase = Application.persistentDataPath.Sub("_HSFrameWorkDevTest/lasthotpatchTestCase.bin");
        public static readonly string HotPatchFolder = Application.persistentDataPath.Sub("hansqurrel_hotpatch");
        public static readonly string PersistentDataPath = Application.persistentDataPath;
        public static readonly string StreamingAssetsPath = Application.streamingAssetsPath;
        public static readonly bool IsEditor = Application.isEditor;
        public static readonly string DataPath = Application.dataPath;
        public static readonly string ProjectPath = Application.dataPath.Sub("../");


        /// <summary>
        /// 初始化静态变量。
        /// </summary>
        public static void WarmUp() { }

        public static bool AutoLogin
        {
            get
            {
                return Application.persistentDataPath.StandardSub("George/autologin").ExistsAsFile() ||
                       Application.dataPath.StandardSub("../data/HSConfigTable/autologin").ExistsAsFile();
            }
        }

        private static RuntimePlatform _platform = Application.platform;
        /// <summary>
        /// 只有在PC下面才需要。
        /// </summary>
        public static bool NeedElegantDispose
        {
            get
            {
                switch (_platform)
                {
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.OSXPlayer:
                    case RuntimePlatform.LinuxEditor:
                    case RuntimePlatform.LinuxPlayer:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public static bool RunInPureClient { get { return !NeedElegantDispose; } }
    }
}
