#if false
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using GLib;
using HSFrameWork.ConfigTable.Editor.Impl;
using HSFrameWork.Common;
using System;

namespace HanSquirrel.ResourceManager
{
    [Obsolete]
    public class AssetBundleEditorV1
    {
        private Dictionary<string, string> BuildDefinedPathABNameDict()
        {
            Dictionary<string, string> definedPathABNameDict = new Dictionary<string, string>();
            ConStr.ABFolderDict.ForEachG(kv =>
            {
                if (definedPathABNameDict.ContainsKey(kv.Value))
                    throw new Exception("AB配置表中路径有重复，请检查：" + kv.Value);
                if (!HSCTC.AppDataPath.Sub("..").Sub(kv.Value).Exists())
                    Debug.LogWarningFormat("AB包配置中的路径不存在: [{0}]", kv.Value);
                definedPathABNameDict.Add(kv.Value, kv.Key);
            });

            return definedPathABNameDict;
        }

        private Dictionary<string, string> BuildPrefabPathABNameDict()
        {
            using (HSUtils.ExeTimer("寻找Prefab所依赖的所有资源"))
            {
                Dictionary<string, string> prefabPathABNameDict = new Dictionary<string, string>();
                List<Tuple<string, List<string>>> prefabABNamePathTupleList = new List<Tuple<string, List<string>>>();

                //所有已经包含的文件夹和文件集合，全部以"Assets/"开头，将其中的文件夹尾部安全添加上"/"
                HashSet<string> processedPathSet = new HashSet<string>(
                    ConStr.ABFolderDict.Select(kv => kv.Value.SafeAddDirEndTagForFolder()));

                //用户定义的字典：文件或者文件夹 - ABName （文件夹都调整为以 ‘/’ ）结尾。
                Dictionary<string, string> definedPathABNameDictEx = new Dictionary<string, string>();
                ConStr.ABFolderDict.ForEachG(
                    kv => definedPathABNameDictEx.Add(kv.Value.SafeAddDirEndTagForFolder(), kv.Key));

                int progress = 0;
                var allDirs = ConStr.ABFolderDict.Where(x => x.Value.ExistsAsFolder())
                    .Select(x => x.Value).Union(ConStr.PrefabSearchPaths).ToArray();
                //GGV2 因为目录中也存在prefab，这些必须也要添加进来。

                var prefabFiles = AssetDatabase.FindAssets("t:Prefab", allDirs)
                                            .Select(AssetDatabase.GUIDToAssetPath)
                                            .ToList();
                for (int i = 0; i < prefabFiles.Count; i++)
                {
                    var prefabFile = prefabFiles[i];
                    //prefabFile全部以"Assets/"开头
                    if (HasProcessed(processedPathSet, prefabFile))
                        continue;   //该prefab文件已经处理过了

                    EditorUtility.DisplayProgressBar("查找Prefab依赖项", prefabFile, (progress++) * 1.0f / prefabFiles.Count);
                    Debug.LogFormat("PREFAB: {0}", prefabFile);

                    processedPathSet.Add(prefabFile);
                    List<string> allDependPath = new List<string> { prefabFile };
                    foreach (var file in AssetDatabase.GetDependencies(prefabFile))
                    {
                        if (NonResourceFile(file) || HasProcessed(processedPathSet, file))
                            continue; //如果file=prefabFile 也会在这里返回

                        if (file.ToLower().EndsWith(".prefab") && file != prefabFile && !prefabFiles.Contains(file))
                        {
                            prefabFiles.Add(file); //GGV2: 发现依赖的prefab，也要去处理。
                            Debug.LogWarningFormat("SmartAdd [{0}]", file);
                        }

                        allDependPath.Add(file);
                        processedPathSet.Add(file);
                    }
                    string abName = SmartGetDefinedABName(definedPathABNameDictEx, prefabFile);
                    allDependPath.ForEach(x => prefabPathABNameDict.Add(x, abName));
                    prefabABNamePathTupleList.Add(Tuple.Create(abName, allDependPath));
                }
                DumpPrefabDepends(prefabABNamePathTupleList);
                return prefabPathABNameDict;
            }
        }

        private bool SameOrSub(string path, string file)
        {
            return (path == file) || (path.EndsWith("/") && file.StartsWith(path));
        }

        private string SmartGetDefinedABName(Dictionary<string, string> definedPathABNameDict, string prefabFile)
        {
            foreach (var kv in definedPathABNameDict)
            {
                if (SameOrSub(kv.Key, prefabFile))
                    return kv.Value;
            }
            return prefabFile.NameWithoutExt().ToLower();
        }

        private void BindPathWithABName(Dictionary<string, string> definedPathABNameDict, Dictionary<string, string> prefabPathABNameDict)
        {
            using (HSUtils.ExeTimer("设置AB包"))
            {
                int progress = 0;
                int count = definedPathABNameDict.Count + prefabPathABNameDict.Count;
                foreach (var kv in definedPathABNameDict.Union(prefabPathABNameDict))
                {
                    if ((progress++) % 100 == 0)
                        EditorUtility.DisplayProgressBar("设置文件夹AB包名", kv.Value + " - " + kv.Key, progress * 1.0f / count);
                    AssetImporter assetImport = AssetImporter.GetAtPath(kv.Key);
                    if (assetImport != null)
                        assetImport.assetBundleName = kv.Value;
                    else
                        Debug.LogErrorFormat("AssetImporter.GetAtPath({0}) failed.", kv.Key);
                }
            }
        }

        public void Build(BuildTarget target)
        {
            try
            {
                BuildInner(target);
            }
            finally
            {
                using (HSUtils.ExeTimer("收尾"))
                {
                    UnBindSysABTag();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        private void BuildInner(BuildTarget target)
        {
            // 用户配置的：ABName-Dir
            Dictionary<string, string> definedPathABNameDict = BuildDefinedPathABNameDict();

            // Prefab所包含的所有文件：File-PrefabName
            Dictionary<string, string> prefabPathABNameDict = BuildPrefabPathABNameDict();

            BindPathWithABName(definedPathABNameDict, prefabPathABNameDict);

            //系统数据库中的：文件 - ABName
            Dictionary<string, string> sysAllFileABNameDict = BuildAllPathABNameDictFromAssetDatabase();

            using (HSUtils.ExeTimer("计算AB包索引并存盘"))
            {
                var bundleData = BuildMyAssertBundleData(definedPathABNameDict, sysAllFileABNameDict);

                SerializeTools.BinarySerilize(ConStr.ABConfigBytes, bundleData);
                SerializeTools.XmlSerialize(ConStr.ABConfigXML, bundleData);
                AssetBundleEditorV2.STEP_7X_DumpFinalPathABMapping(bundleData, HSCTC.InDebug("0_FinalV1.txt"));
            }

            //根据ab包名打包
            using (HSUtils.ExeTimer("BuildAssetBundles"))
            {
                AssetBundleEditorV2.STEP_8_ClearWasteABUI(AssetDatabase.GetAllAssetBundleNames());
                AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath, BuildAssetBundleOptions.ChunkBasedCompression, target);
                if (manifest == null)
                    Debug.LogError("AssetBundle 打包失败");
                else
                    Debug.Log("AssetBundle 打包完毕");
            }

            //拷贝配置文件到streamingassets文件夹下面
            File.Copy(Application.dataPath.Sub("/BuildSource/AssetDependenciesCfg.bytes"), Application.dataPath.Sub("/StreamingAssets/AssetDependenciesCfg.bytes"), true);
        }

        private void UnBindSysABTag()
        {
            //清除所有文件Ab包名
            using (HSUtils.ExeTimer("清理AB包"))
            {
                string[] oldAssetbundleNames = AssetDatabase.GetAllAssetBundleNames();
                for (int i = 0; i < oldAssetbundleNames.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("清除AssetbundleName", oldAssetbundleNames[i], i * 1.0f / oldAssetbundleNames.Length);
                    AssetDatabase.RemoveAssetBundleName(oldAssetbundleNames[i], true);
                }
            }
        }

        private bool HasProcessed(HashSet<string> pathSet, string filePath)
        {
            //路径直接相等是true,或者此路径包含之前路径并且去除之前路径后第一个为/
            //return pathSet.Any(x => path == x || (path.Contains(x) && (path.Replace(x, "")[0] == '/')));

            //GGTODO新逻辑：如果是prefab文件，则必须完整匹配，否则仅仅匹配路径部分。
            return filePath.ToLower().EndsWith(".prefab") ? pathSet.Contains(filePath) : pathSet.Any(x => SameOrSub(x, filePath));
        }

        private Dictionary<string, string> BuildAllPathABNameDictFromAssetDatabase()
        {
            //key值为每个路径或者文件，value对应的AB包名称
            Dictionary<string, string> allPathABNameDict = new Dictionary<string, string>();

            using (HSUtils.ExeTimer("读取AB-File映射"))
            using (var sw = File.CreateText(HSCTC.InDebug("0_SYS_AB-File-Mapping.txt")))
            {
                var abNames = AssetDatabase.GetAllAssetBundleNames();
                for (int i = 0; i < abNames.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("读取AB-File映射", abNames[i], i * 1.0f / abNames.Length);

                    sw.WriteLine(abNames[i]);
                    foreach (var path in AssetDatabase.GetAssetPathsFromAssetBundle(abNames[i]).Where(x => !NonResourceFile(x)))
                    {
                        sw.WriteLine("\t" + path);
                        allPathABNameDict.Add(path, abNames[i]);
                    }
                }
            }
            return allPathABNameDict;
        }

        private AssetBundleData BuildMyAssertBundleData(Dictionary<string, string> definedPathABNameDict, Dictionary<string, string> sysAllFileABNameDict)
        {
            AssetBundleData bundleData = new AssetBundleData { AllABList = new List<ABPath>() };

            //因为游戏中会用一个目录名来加载一个资源数组，因此需要加入字典。
            foreach (var kv in definedPathABNameDict)
            {
                bundleData.AllABList.Add(new ABPath
                {
                    FullPathDevOnly = kv.Key,
                    Crc = Crc32.GetCrc32(kv.Key),
                    AssetBundleName = kv.Value,
                    DependAssetBundle = CheckDirectoryDependencies(kv.Key, kv.Value, sysAllFileABNameDict)
                });
            }

            foreach (var kv in sysAllFileABNameDict)
            {
                bundleData.AllABList.Add(new ABPath
                {
                    FullPathDevOnly = kv.Key,
                    Crc = Crc32.GetCrc32(kv.Key),
                    AssetBundleName = kv.Value,
                    DependAssetBundle = new List<string>(AddDepAb(kv.Value, kv.Key, new HashSet<string>(), sysAllFileABNameDict))
                });
            }

            return bundleData;
        }

        private HashSet<string> _NonResourceExts = new HashSet<string> { ".cs", ".js", ".tpsheet" };
        private bool NonResourceFile(string file)
        {
            return _NonResourceExts.Contains(file.Ext().ToLower());
        }

        private HashSet<string> AddDepAb(string thisABName, string path, HashSet<string> depAbSet, Dictionary<string, string> sysAllFileABNameDict)
        {
            string abName;
            foreach (var file in AssetDatabase.GetDependencies(path))
            {
                if (file == path || NonResourceFile(file))
                    continue;
                if (!sysAllFileABNameDict.TryGetValue(file, out abName))
                    Debug.LogWarningFormat("Missing [{0}]", file);
                if (thisABName != abName)
                    depAbSet.Add(abName);
            }
            return depAbSet;
        }

        private List<string> CheckDirectoryDependencies(string directory, string currentAB, Dictionary<string, string> sysAllFileABNameDict)
        {
            HashSet<string> ret = new HashSet<string>();
            if (directory.ExistsAsFolder())
            {
                foreach (var file in new DirectoryInfo(directory).GetFiles("*", SearchOption.AllDirectories))
                {
                    string path = file.FullName.Replace(@"\", "/").Replace(Application.dataPath.Replace("Assets", ""), "");
                    AddDepAb(currentAB, path, ret, sysAllFileABNameDict);
                }
            }
            return new List<string>(ret);
        }

        private void DumpPrefabDepends(List<Tuple<string, List<string>>> prefabDepDict)
        {
            using (var sw = File.CreateText(HSCTC.InDebug("0_SYS_Prefab_Dep.txt")))
            {
                foreach (var kv in prefabDepDict)
                {
                    sw.WriteLine(kv.Item1);
                    foreach (var p in kv.Item2)
                        sw.WriteLine("\t" + p);
                }
            }
        }

        public static void DumpFinalBytes(string bytes, string file)
        {
            AssetBundleData assetBundleData = SerializeTools.BinaryDeserilize<AssetBundleData>(bytes);
            using (var sw = File.CreateText(file))
            {
                foreach (var x in assetBundleData.AllABList)
                {
                }
            }
        }
    }
}
#endif