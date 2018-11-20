using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using StrDict = System.Collections.Generic.Dictionary<string, string>;
using StrSetDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<string>>;
using StrListDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>;
using UnityEngine;
using UnityEditor;
using GLib;
using HSFrameWork.Common;
using HSFrameWork.ConfigTable.Editor;
using HanSquirrel.ResourceManager.Impl;

namespace HanSquirrel.ResourceManager.Editor.Inner
{
    public class SmartABCacheCleaner : IDisposable
    {
        public static SmartABCacheCleaner NewInstance
        {
            get
            {
                return new SmartABCacheCleaner();
            }
        }

        HashSet<AssetBundle> bundles = new HashSet<AssetBundle>(Resources.FindObjectsOfTypeAll<AssetBundle>());
        public void Dispose()
        {
            Resources.FindObjectsOfTypeAll<AssetBundle>().Where(ab => !bundles.Contains(ab)).ForEachG(ab => ab.Unload(true));
        }

        public IEnumerable<AssetBundle> Added()
        {
            return Resources.FindObjectsOfTypeAll<AssetBundle>().Where(ab => !bundles.Contains(ab));
        }

        public static void UnloadNamed()
        {
            Resources.FindObjectsOfTypeAll<AssetBundle>().Where(ab => ab.name.Visible()).ForEachG(ab => ab.Unload(true));
        }
    }
}

namespace HanSquirrel.ResourceManager.Editor
{
    using HanSquirrel.ResourceManager.Editor.Inner;
    /// <summary>
    /// 打包程序需要的参数
    /// </summary>
    public class ABBuildArg
    {
        /// <summary>
        /// AB包名称 - "Asset/*" 的字典。此目录会被打包为一个独立的AB包；
        /// IAssetBundleManager可以通过这个路径名来加载AB包。
        /// </summary>
        public Dictionary<string, string> AbFolderDict;

        /// <summary>
        ///  "Asset/*" 的数组。
        ///  打包程序将会搜索这些目录下的所有prefab；
        ///  并按照文件名作为AB包名称，将这些prefab及其依赖的不在AbFolderDict
        ///  里面的资源全部打到同一个AB包里面。
        /// </summary>
        public IEnumerable<string> PrefabSearchPaths;

        public BuildTarget Target;

        /// <summary>
        /// 如果有ValueV2的数据包存在，是否包含完整Values。
        /// </summary>
        public bool IncludeValuesWhenValuesV2Exists = true;

        /// <summary>
        /// 是否在打包后清理不在此打包范围 AB包。在正式打包的时候要设置为TRUE。
        /// 在黑盒测试的时候，如果多个用例的输出都是同一个路径，则最好设置为FALSE，
        /// 否则每次打包时间都很长。
        /// </summary>
        public bool ClearWasteAB;

        /// <summary>
        /// 除了AssetBundleEditorV2打的AB包之外，还有哪些AB包是合法的。
        /// 对于不合法的，如果ClearWasteAB是true，就会被删除。
        ///（框架内部会自动添加：索引文件、Android、lua、filter、values、以及values_*）
        /// </summary>
        public string[] AdditionalABs;

        /// <summary>
        /// AB包输出路径，正式打包的时候是 HSCTC.StreamingAssetsPath
        /// </summary>
        public string OutputFolder;

        /// <summary>
        /// 打包过程详细记录文件的左边部分。正式打包的时候是  HSCTC.InDebug("AB_")
        /// </summary>
        public string DebugFileLeftPart;

        /// <summary>
        /// 是否模拟打包：所有详细记录文件和索引文件都会输出，就是不去生成AB包。
        /// </summary>
        public bool FakeBuild;
    }


    /// <summary>
    /// 无外部依赖的AB包打包模块，和AssetBundleManager对应。
    /// </summary>
    public class AssetBundleEditorV2
    {
        private StartStopHelper _SSH = new StartStopHelper();
        private ABBuildArg _Arg;

        private static ABBuildArg STEP_0_CheckArg(ABBuildArg arg)
        {
            arg.AbFolderDict = new StrDict(arg.AbFolderDict);

            var nonePaths = arg.AbFolderDict.Where(x => !x.Value.Exists()).ToArray();
            if (nonePaths.Length > 0)
            {
                Debug.LogErrorFormat("AB包定义的如下路径不存在，被忽略；然而程序可能不会正常运行。：\r\n" +
                    nonePaths.Select(x => x.Value).JoinC("\r\n"));
                nonePaths.ForEachG(x => arg.AbFolderDict.Remove(x.Key));
            }

            if (AssetDatabase.FindAssets("t:Prefab", arg.PrefabSearchPaths.ToArray())
                             .Select(x => AssetDatabase.GUIDToAssetPath(x).NameWithoutExt())
                             .Union(arg.AbFolderDict.Keys)
                             .Any(x => x.ToLower().StartsWith("hsframework_")))
            {
                throw new Exception("外部配置错误：AB包名字或者要打包的Prefab名字不能以 hsframework_ 开头。");
            }

            Debug.Log("自动将资源索引文件打包为单独AB包。");
            //索引文件 AB包
            arg.AbFolderDict.Add(HSUnityEnv.ResourceABIndexABName, HSUnityEnv.ResourceABIndexFile);

            return arg;
        }

        /// <summary>
        /// 根据arg来打AB包
        /// </summary>
        public void Build(ABBuildArg arg)
        {
            _SSH.Start_ExceptionIfStarted("每个AssetBundleEditorV2只能用一次");

            _Arg = STEP_0_CheckArg(arg);

            using (new SmartABCacheCleaner())
            using (HSUtils.ExeTimer("Build({0})".f(arg.Target)))
            {
                StrDict allFileABDict1;
                using (HSUtils.ExeTimerEnd("STEP_1_GenFileABDict_4_FolderABDefs"))
                    allFileABDict1 = STEP_1_GenFileABDict_For_FolderABDefs();

                StrDict allFileABDict2;
                using (HSUtils.ExeTimerEnd("STEP_2_GenFileABDict_4_PrefabABDefs"))
                    allFileABDict2 = STEP_2_GenFileABDict_For_PrefabABDefs(allFileABDict1);

                using (HSUtils.ExeTimerEnd("STEP_3_Search_ExtraDeps_In_FolderABDefs"))
                    STEP_3_Search_ExtraDeps_In_FolderABDefs(allFileABDict1, allFileABDict2);

                STEP_4_CheckUniquess(allFileABDict1, allFileABDict2);

                StrListDict allABPathsDict4Build;
                using (HSUtils.ExeTimerEnd("STEP_5_GenAllABPathsDict4Build"))
                {
                    allABPathsDict4Build = STEP_5_GenAllABPathsDict4Build(allFileABDict2);
                    STEP_5X_DumpAllABPathsDict4Build(allABPathsDict4Build);
                }

                using (HSUtils.ExeTimerEnd("STEP_7_GenFinalPathABsMapping"))
                {
                    var foreignRefDict = STEP_6_PrepareForeignRefDict(allFileABDict1, allFileABDict2);
                    Dictionary<UInt32, ResourceABPair> assetBundleData = STEP_7_GenFinalPathABsMapping(allFileABDict1, allFileABDict2, foreignRefDict);
                    STEP_6X_DumpForeignRefs(foreignRefDict);
                    STEP_7X_DumpFinalPathABMapping(assetBundleData, _Arg.DebugFileLeftPart + "7_FinalV2.txt");

                    HSUnityEnv.ResourceABIndexFile.WriteAllBytes(assetBundleData.Serialize());
                    AssetDatabase.ImportAsset(HSUnityEnv.ResourceABIndexFile, ImportAssetOptions.ForceSynchronousImport);
                }

                if (_Arg.FakeBuild)
                {
                    HSUtils.LogWarning("打AB包被配置忽略。没有真正打包，仅仅生成索引文件。");
                    if (ResourceLoader.LoadFromABAlways)
                        Debug.LogWarning("当前设置永远从AB包加载。如果不打AB包，程序运行可能会异常。");
                    return;
                }

                if (_Arg.ClearWasteAB)
                    STEP_8_ClearWasteABUI(allABPathsDict4Build.Keys);

                using (HSUtils.ExeTimerEnd("BuildAssetBundles：普通AB包"))
                {
                    EditorUtility.DisplayProgressBar("正式打包：普通AB包", "BuildPipeline.BuildAssetBundles", 0.8f);
                    var manifest = BuildPipeline.BuildAssetBundles(_Arg.OutputFolder,
                        STEP_9_GenBuildTask(allABPathsDict4Build), BuildAssetBundleOptions.ChunkBasedCompression, arg.Target);
                    if (manifest == null)
                        Debug.LogError("AssetBundle 打包失败");
                    else
                        Debug.Log("AssetBundle 打包完毕");
                }

#if HSFRAMEWORK_VALUES_IN_AB
                using (HSUtils.ExeTimerEnd("BuildAssetBundles 内部配置表等"))
                    STEP_A_BuildCEValues();
#endif
                using (HSUtils.ExeTimerEnd("Refreshing"))
                    AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// 将定义在[AB包名字-文件夹]下的所有文件都放入字典。
        /// </summary>
        private StrDict STEP_1_GenFileABDict_For_FolderABDefs()
        {
            int progress = 0;
            StrDict allFileABDict = new StrDict();
            using (var sw = File.CreateText(_Arg.DebugFileLeftPart + "1_Folder_Files_V2.txt"))
            {
                sw.WriteLine("此文件记录了 所有自定义的AB包中的所有原始资源（不列出外部依赖）。");
                foreach (var kv in _Arg.AbFolderDict.ToList().SortC((a, b) => a.Value.CompareTo(b.Value)))
                {
                    if ((progress++) % 10 == 0)
                        EditorUtility.DisplayProgressBar("查找资源", kv.Key, progress * 1.0f / _Arg.AbFolderDict.Count);

                    sw.WriteLine("{0} [{1}]", kv.Value, kv.Key);
                    allFileABDict.Add(kv.Value.RemoveDirEndTag(), kv.Key.ToLower()); //目录名
                    if (kv.Value.ExistsAsFolder())
                    {
                        EnumAllResourceFiles(kv.Value)
                            .ForEachG(x =>
                            {
                                allFileABDict.Add(x, kv.Key.ToLower());
                                sw.WriteLine("\t" + x);
                            });  //该目录下的所有资源文件
                    }
                }
            }
            EditorUtility.ClearProgressBar();
            return allFileABDict;
        }

        /// <summary>
        /// 将定义在[Prefab搜索文件夹][AB包名字-文件夹]下的所有Prefab以及其依赖的所有文件，放入字典。
        /// </summary>
        private StrDict STEP_2_GenFileABDict_For_PrefabABDefs(StrDict allFileABDict1)
        {
            StrDict allFileABDict2 = new StrDict();
            string currentPrefab = null;
            string currentABName = null;

            using (var sw = File.CreateText(_Arg.DebugFileLeftPart + "2_DepLog_Prefab.txt"))
            {
                sw.WriteLine("此文件记录了 Pefab文件 所依赖的资源。");
                EnumAllPrefabDepsSorted(
                prefab =>
                {
                    currentPrefab = prefab;
                    if (!allFileABDict1.TryGetValue(prefab, out currentABName) && //先去目录AB中去找
                        !allFileABDict2.TryGetValue(prefab, out currentABName))   //再去PrefabAB中去找
                    {
                        currentABName = currentPrefab.NameWithoutExt().ToLower();
                        allFileABDict2.Add(prefab, currentABName);
                    }
                    sw.WriteLine("{0} [{1}]", currentPrefab, currentABName);
                },
                file =>
                {
                    string abName;
                    if (allFileABDict1.TryGetValue(file, out abName) || allFileABDict2.TryGetValue(file, out abName))
                    {
                        sw.WriteLine("\t{0} [{1}]", file, abName);
                    }
                    else
                    {
                        allFileABDict2.Add(file, currentABName);
                        sw.WriteLine("\t{0} [{1}]", file, currentABName);
                    }
                });
            }
            return allFileABDict2;
        }

        /// <summary>
        /// 在自定义目录中搜索对外部资源的引用
        /// </summary>
        private void STEP_3_Search_ExtraDeps_In_FolderABDefs(StrDict allFileABDict1, StrDict allFileABDict2)
        {
            int progress = 0;
            using (var sw = File.CreateText(_Arg.DebugFileLeftPart + "3_DepLog_Folder.txt"))
            {
                sw.WriteLine("此文件记录了 自定义资源文件夹 所依赖的外部资源。");
                foreach (var kv in allFileABDict1)
                {
                    if (kv.Key.ExistsAsFolder())
                        continue;

                    if ((progress++) % 10 == 0)
                        EditorUtility.DisplayProgressBar("查找资源依赖项", kv.Key, progress * 1.0f / allFileABDict1.Count);

                    bool headerLogged = false;
                    foreach (var file in GetDepCached(kv.Key))
                    {
                        if (!IsResourceFile(file) || allFileABDict1.ContainsKey(file) || allFileABDict2.ContainsKey(file))
                            continue;
                        if (!headerLogged)
                        {
                            headerLogged = true;
                            sw.WriteLine("{0} [{1}]", kv.Key, kv.Value);
                        }
                        sw.WriteLine("\t" + file);
                        allFileABDict2.Add(file, kv.Value);
                    }
                }
            }
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// 检查确保两个字典的KEY没有重复。
        /// </summary>
        private static void STEP_4_CheckUniquess(StrDict dict1, StrDict dict2)
        {
            if (new HashSet<string>(dict1.Keys.Union(dict2.Keys)).Count != dict1.Count + dict2.Count)
                throw new Exception("George编程错误：allFileABDict1和allFileABDict2有重复。");
        }

        /// <summary>
        /// 构造调用 BuildPipeline.BuildAssetBundles 需要的 AbName-string[]
        /// </summary>
        private StrListDict STEP_5_GenAllABPathsDict4Build(StrDict allFileABDict2)
        {
            StrListDict allABPathsDict4Build = new StrListDict();
            //目录 AB包
            _Arg.AbFolderDict.ForEachG(kv => allABPathsDict4Build.GetOrAdd(kv.Key).Add(kv.Value.RemoveDirEndTag()));

            //Prefab AB包
            allFileABDict2.ForEachG(kv => allABPathsDict4Build.GetOrAdd(kv.Value).Add(kv.Key));

            allABPathsDict4Build.Values.ForEachG(l => l.Sort());
            return allABPathsDict4Build;
        }

        private void STEP_5X_DumpAllABPathsDict4Build(StrListDict allABPathsDict)
        {
            using (var sw = File.CreateText(_Arg.DebugFileLeftPart + "5_AB_Paths4BuildV2.txt"))
            {
                sw.WriteLine("此文件记录了 各个AB包所包含的路径（文件和文件夹）。");
                foreach (var kv in allABPathsDict)
                {
                    sw.WriteLine(kv.Key);
                    foreach (var file in kv.Value)
                        sw.WriteLine("\t" + file);
                }
            }
        }

        private StrSetDict STEP_6_PrepareForeignRefDict(StrDict dict1, StrDict dict2)
        {
            IEnumerable<string> _PrefabFolders = _Arg.PrefabSearchPaths
                .Where(x => x.ExistsAsFolder()).Select(x => x.SafeAddDirEndTagForFolder());

            StrSetDict foreignRefDict = new StrSetDict();
            dict2.Select(kv => kv.Key)
                .Where(x => !dict1.ContainsKey(x) && !_PrefabFolders.Any(y => x.StartsWith(y)))
                .ForEachG(x => foreignRefDict.GetOrAdd(x));
            return foreignRefDict;
        }

        private void STEP_6X_DumpForeignRefs(StrSetDict foreignRefDict)
        {
            if (foreignRefDict.Count == 0)
                return;

            var fileName = _Arg.DebugFileLeftPart + "6_Foreign_V2.txt";
            Debug.LogWarningFormat("发现引用定义外的资源 [{0}]个，已经存入[{1}]", foreignRefDict.Count, fileName);
            using (var sw = File.CreateText(fileName))
            {
                sw.WriteLine("此文件记录了 所有被引用的外部资源和引用者。");
                foreignRefDict.ToList().SortC((a, b) => a.Key.CompareTo(b.Key)).ForEach(kv =>
                {
                    sw.WriteLine(kv.Key);
                    foreach (var file in kv.Value.ToList().SortC())
                        sw.WriteLine("\t" + file);
                });
            }
        }

        /// <summary>
        /// 生成 资源 - AB包索引文件。
        /// </summary>
        private Dictionary<UInt32, ResourceABPair> STEP_7_GenFinalPathABsMapping(StrDict dict1, StrDict dict2, StrSetDict foreignRefDict)
        {
            int progress = 0;
            Dictionary<UInt32, ResourceABPair> ret = new Dictionary<UInt32, ResourceABPair>();
            dict1.Union(dict2).ForEachG(kv =>
                {
                    if ((progress++) % 10 == 0)
                        EditorUtility.DisplayProgressBar("生成资源AB包依赖索引", kv.Key, progress * 1.0f / (dict1.Count + dict2.Count));
                    uint crc = Crc32.GetCrc32(kv.Key);
                    ret.Add(crc, new ResourceABPair
                    {
                        Crc = crc,
                        PathKey = kv.Key,
                        ABName = kv.Value,
                        DepdentABNames = STEP_7x_GetDepAb(kv.Value, kv.Key, dict1, dict2, foreignRefDict)
                    });
                });

            EditorUtility.ClearProgressBar();
            return ret;
        }

        private List<string> STEP_7x_GetDepAb(string thisABName, string path, StrDict dict1, StrDict dict2, StrSetDict foreignRefDict)
        {
            HashSet<string> depAbSet = new HashSet<string>();
            if (path.ExistsAsFolder())
            {
                EnumAllResourceFiles(path).ForEachG(x => STEP_7xx_FillDepAb(depAbSet, x, dict1, dict2, foreignRefDict));
            }
            else
            {
                STEP_7xx_FillDepAb(depAbSet, path, dict1, dict2, foreignRefDict);
            }
            depAbSet.Remove(thisABName);
            return depAbSet.ToList();
        }

        private HashSet<string> STEP_7xx_FillDepAb(HashSet<string> depAbSet, string path, StrDict dict1, StrDict dict2, StrSetDict foreignRefDict)
        {
            foreach (var file in GetDepCached(path))
            {
                if (!IsResourceFile(file))
                    continue;

                string abName;
                if (dict1.TryGetValue(file, out abName) || dict2.TryGetValue(file, out abName))
                {
                    depAbSet.Add(abName);
                    HashSet<string> traitors;
                    if (file != path && foreignRefDict.TryGetValue(file, out traitors))
                        traitors.Add(path);
                }
                else
                {
                    Debug.LogErrorFormat("[{0}] Missing [{1}]", path, file);
                }
            }
            return depAbSet;
        }

        private static void STEP_7X_DumpFinalPathABMapping(Dictionary<UInt32, ResourceABPair> bundleData, string file)
        {
            using (var sw = File.CreateText(file))
            {
                sw.WriteLine("此文件记录了 每个资源所在的AB包和所依赖的AB包。");
                foreach (var ab in bundleData.Values.ToList().SortC((a, b) => a.PathKey.CompareTo(b.PathKey)))
                    sw.WriteLine("{0} [{1}] {2} - {3}", ab.PathKey, ab.Crc, ab.ABName, ab.DepdentABNames.SortC().JoinC(", "));
            }
        }

        private static readonly string[] _ABNamesInner = new string[]
        {
            "StreamingAssets", "Android",
            "console.css", "favicon.ico", "index.html", "CUDLR"
        };

        private void STEP_8_ClearWasteABUI(IEnumerable<string> validABNames)
        {
            if (_Arg.AdditionalABs == null)
                _Arg.AdditionalABs = new string[0];

            HashSet<string> allFileNames = new HashSet<string>(
                validABNames.Union(_Arg.AdditionalABs).Union(_ABNamesInner).Union(HSCTC.AllCEABNames)
                .SelectMany(s => new string[] { s, s + ".meta", s + ".manifest", s + ".manifest.meta" }));

            new DirectoryInfo(_Arg.OutputFolder).GetFiles("*", SearchOption.AllDirectories)
                .Where(fi => !allFileNames.Contains(fi.Name))
                .ForEachG(fi =>
                {
                    Debug.LogWarning("请手工删除无效AB包或者添加到Constr.AdditionalABs中。: {0}".Eat(fi.Name));
                    //File.Delete(fi.FullName);
                });
        }

        private AssetBundleBuild[] STEP_9_GenBuildTask(StrListDict allABPathsDict4Build)
        {
            var ret = allABPathsDict4Build.Select(kv =>
                   new AssetBundleBuild
                   {
                       assetBundleName = kv.Key,
                       assetNames = kv.Value.ToArray()
                   }).ToArray();
            return ret; //Debug
        }

#if HSFRAMEWORK_VALUES_IN_AB
        private void STEP_A_BuildCEValues()
        {
            StrListDict abNamePathDict = new StrListDict();

            //压缩加密ValuesV2 AB包
            var tags = HSCTC.ValueBundleTags.ToList();
            if (_Arg.IncludeValuesWhenValuesV2Exists || tags.Count == 0)
            {
                //压缩加密Values AB包
                abNamePathDict.GetOrAdd(HSUnityEnv.CEValuesABName).Add(HSUnityEnv.CEValuesPath);
                Debug.Log("自动将Values文件打包为单独AB包。");
            }

            if (tags.Count > 0)
            {
                Debug.LogFormat("自动将 [{0}] 个子ValueBundle文件打包为单独AB包。", tags.Count);
                tags.ForEach(tag =>
                        abNamePathDict.GetOrAdd(HSUnityEnv.CEValuesV2ABName.f(tag)).Add(HSUnityEnv.CEValuesV2Path.f(tag)));
            }

            if (HSUnityEnv.CEFilterPath.ExistsAsFile())
            {
                Debug.Log("自动将filter文件打包为单独AB包。");
                abNamePathDict.GetOrAdd(HSUnityEnv.CEFilterABName).Add(HSUnityEnv.CEFilterPath);
            }

            if (HSUnityEnv.CELuaPath.ExistsAsFile())
            {
                Debug.Log("自动将Lua文件打包为单独AB包。");
                abNamePathDict.GetOrAdd(HSUnityEnv.CELuaABName).Add(HSUnityEnv.CELuaPath);
            }

            if (abNamePathDict.Count == 0)
            {
                Debug.LogWarning("没有任何Values文件被打包，APP可能无法运行。请先运行GenData！");
                return;
            }

            EditorUtility.DisplayProgressBar("最后一步：配置表打包", "BuildPipeline.BuildAssetBundles", 0.9f);
            var manifest = BuildPipeline.BuildAssetBundles(_Arg.OutputFolder,
                STEP_9_GenBuildTask(abNamePathDict), BuildAssetBundleOptions.None, _Arg.Target);
            if (manifest == null)
                Debug.LogError("内部配置表等 打包失败");
            else
                Debug.Log("内部配置表等 打包完毕");
        }
#endif

#region 小函数
        /// <summary>
        /// 将path下 的所有资源都找出来，格式为 Asset/....
        /// </summary>
        public static IEnumerable<string> EnumAllResourceFiles(string path)
        {
            return new DirectoryInfo(path).GetFiles("*", SearchOption.AllDirectories)
                        .Where(x => IsResourceFile(x.Name))
                        .Select(x => ConvertFullPathToAssetPath(x.FullName));
        }

        private static string ConvertFullPathToAssetPath(string fullPath)
        {
            fullPath = fullPath.Replace('\\', '/');
            var x = fullPath.IndexOf("/Assets/");
            if (x == -1)
                throw new Exception("ConvertFullPathToAssetPath({0}) 无法找到Assets".f(fullPath));
            return fullPath.Substring(x + 1);
        }

        private void EnumAllPrefabDepsSorted(Action<string> onChangePrefabFile, Action<string> onFindDep, bool onlyResourceFile = true)
        {
            EditorUtility.DisplayProgressBar("查找所有Prefab", "正在搜索...", 0.1f);
            var prefabFiles = AssetDatabase.FindAssets("t:Prefab", _Arg.PrefabSearchPaths.ToArray())
                                        .Select(AssetDatabase.GUIDToAssetPath)
                                        .ToList().SortC();
            for (int i = 0; i < prefabFiles.Count; i++)
            {
                var prefabFile = prefabFiles[i];
                onChangePrefabFile(prefabFile);

                if (i % 10 == 0)
                    EditorUtility.DisplayProgressBar("查找Prefab依赖项", prefabFile, i * 1.0f / prefabFiles.Count);

                if (onlyResourceFile)
                    GetDepCached(prefabFile).Where(IsResourceFile).ForEachG(onFindDep);
                else
                    GetDepCached(prefabFile).ForEachG(onFindDep);
            }
            EditorUtility.ClearProgressBar();
        }

        private static HashSet<string> _NonResourceExts = new HashSet<string> { ".cs", ".js", ".meta" };

        /// <summary>
        /// 开发者内部使用。
        /// 如果文件后缀是 .cs .js .meta，则返回false。否则返回true。
        /// </summary>
        public static bool IsResourceFile(string file)
        {
            return !_NonResourceExts.Contains(file.Ext().ToLower());
        }

        private Dictionary<string, string[]> _PathDepCache = new Dictionary<string, string[]>();

        /// <summary>
        /// 开发者内部使用。
        /// </summary>
        public string[] GetDepCached(string path)
        {
            string[] ret;
            if (!_PathDepCache.TryGetValue(path, out ret))
            {
                ret = AssetDatabase.GetDependencies(path).SortC();
                _PathDepCache.Add(path, ret);
            }
            return ret;
        }
        #endregion

        #region 测试小函数
        /// <summary>
        /// 开发者内部使用。
        /// </summary>
        public static void DumpAllFolderAB()
        {
            new AssetBundleEditorV2().STEP_1_GenFileABDict_For_FolderABDefs();
        }

        /// <summary>
        /// 开发者内部使用。
        /// </summary>
        public static void DumpAllPrefabDeps(IEnumerable<string> prefabPaths, string fileName)
        {
            AssetBundleEditorV2 abe = new AssetBundleEditorV2();
            abe._Arg = new ABBuildArg { PrefabSearchPaths = prefabPaths };
            using (var sw = File.CreateText(fileName))
            {
                sw.WriteLine("此文件记录了 所有Prefab文件所依赖的文件。");
                abe.EnumAllPrefabDepsSorted(prefab => sw.WriteLine(prefab), file => sw.WriteLine("\t" + file), false);
            }
        }
#endregion
    }
}
