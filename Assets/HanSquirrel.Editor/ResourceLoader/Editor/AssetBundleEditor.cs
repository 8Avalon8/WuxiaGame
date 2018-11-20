#if false
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Xml;
using System.Linq;
using GLib;
using HSFrameWork.ConfigTable.Editor.Impl;
using System;

namespace HanSquirrel.ResourceManager
{
    [Obsolete]
    public class AssetBundleEditor
    {
        //按文件夹打包的列表,前面为ab包名字，后面为文件夹路径
        private static Dictionary<string, string> m_AllFileDirectory = new Dictionary<string, string>();
        //把所有已经按文件夹打包的保存起来，在prefab依赖打包的时候剔除这些文件
        private static List<string> m_AllFileAB = new List<string>();
        //按文件夹打包的列表,前面为ab包名字，后面为文件夹路径
        private static Dictionary<string, List<string>> m_AllPrefabs = new Dictionary<string, List<string>>();
        //按全路径储存ab包名字
        private static Dictionary<string, string> m_DirToABDic = new Dictionary<string, string>();

        public static void Build(BuildTarget target)
        {
            m_AllFileDirectory.Clear();
            m_AllFileAB.Clear();
            m_AllPrefabs.Clear();
            m_DirToABDic.Clear();
            
            //按文件夹打包
#if true
            ConStr.ABFolderDict.ForEachG(kv => m_AllFileDirectory.Add(kv.Key, kv.Value.RemoveDirEndTag()));
#else
            ABConfig abConfig = AssetCreat.GetAsset<ABConfig>("Assets/HanSquirrel.Editor/ResourceLoader/Editor/ABConfig.asset");
            for (int i = 0; i < abConfig.m_AllFileDirAB.Count; i++)
            {
                ABConfig.FileDirABName file = abConfig.m_AllFileDirAB[i];
                if (m_AllFileDirectory.ContainsKey(file.Name))
                {
                    Debug.LogError("AB配置表中ab包名字命名有重复，请检查：" + file.Name);
                }
                else
                {
                    m_AllFileDirectory.Add(file.Name, file.Path);
                }
            }
#endif
            //添加入过滤缓存
            foreach (string path in m_AllFileDirectory.Values)
            {
                m_AllFileAB.Add(path);
            }

            List<string> guidList = new List<string>();
#if true
            for (int i = 0; i < ConStr.PrefabSearchPaths.Length; i++)
            {
                guidList.AddRange(AssetDatabase.FindAssets("t:Prefab", new string[] { ConStr.PrefabSearchPaths[i] }));
            }
#else
            for (int i = 0; i < abConfig.m_AllPrefabFile.Count; i++)
            {
                guidList.AddRange(AssetDatabase.FindAssets("t:Prefab", new string[] { abConfig.m_AllPrefabFile[i] }));
            }
#endif
            for (int i = 0; i < guidList.Count; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guidList[i]);
                EditorUtility.DisplayProgressBar("Assetbundle", "查找Prefab依赖项: " + path, i * 1.0f / guidList.Count);
                if (!ContenAllFileAB(path))
                {
                    //GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path); //GG 20180908
                    List<string> allDependPath = new List<string>();
                    //把自己加入到里面
                    allDependPath.Add(path);
                    string[] allDepend = AssetDatabase.GetDependencies(path);
                    //此处遍历依赖项主要查看之前文件夹打包是否已经打入资源
                    for (int j = 0; j < allDepend.Length; j++)
                    {
                        if (allDepend[j] != path && !ContenAllFileAB(allDepend[j]) && !allDepend[j].EndsWith(".cs") && !allDepend[j].EndsWith(".prefab"))
                        {

                            allDependPath.Add(allDepend[j]);
                            //所有打过的资源都加入缓存，保证资源不会冗余
                            m_AllFileAB.Add(allDepend[j]);
                        }
                    }
                    //m_AllPrefabs.Add(obj.name, allDependPath); //GG 20180908
                    m_AllPrefabs.Add(path.NameWithoutExt(), allDependPath);
                }
            }

            //设置AB包名字
            Dictionary<string, string>.Enumerator fileIt = m_AllFileDirectory.GetEnumerator();
            int tempFileIndex = 0;
            while (fileIt.MoveNext())
            {
                EditorUtility.DisplayProgressBar("Assetbundle", "设置文件夹AB包名: " + fileIt.Current.Value, tempFileIndex * 1.0f / m_AllFileDirectory.Count);
                SetAssetBundleName(fileIt.Current.Key, fileIt.Current.Value);
                tempFileIndex++;
            }

            Dictionary<string, List<string>>.Enumerator PrefabIt = m_AllPrefabs.GetEnumerator();
            int tempPrefabIndex = 0;
            while (PrefabIt.MoveNext())
            {
                EditorUtility.DisplayProgressBar("Assetbundle", "设置文件夹AB包名: " + PrefabIt.Current.Key, tempPrefabIndex * 1.0f / m_AllPrefabs.Count);
                SetAssetBundleName(PrefabIt.Current.Key, PrefabIt.Current.Value);
                tempPrefabIndex++;
            }

            //根据ab包名打包
            BuildNew(target);
            //清除所有文件Ab包名
            string[] oldAssetbundleNames = AssetDatabase.GetAllAssetBundleNames();
            for (int i = 0; i < oldAssetbundleNames.Length; i++)
            {
                AssetDatabase.RemoveAssetBundleName(oldAssetbundleNames[i], true);
                EditorUtility.DisplayProgressBar("Assetbundle", "清除AssetbundleName: " + oldAssetbundleNames[i], i * 1.0f / oldAssetbundleNames.Length);
            }

            //拷贝配置文件到streamingassets文件夹下面
            File.Copy(Application.dataPath.Sub("/BuildSource/AssetDependenciesCfg.bytes"), Application.dataPath.Sub("/StreamingAssets/AssetDependenciesCfg.bytes"), true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }

        static bool ContenAllFileAB(string path)
        {
            for (int i = 0; i < m_AllFileAB.Count; i++)
            {
                //路径直接相等是true,或者此路径包含之前路径并且去除之前路径后第一个为/
                if (path == m_AllFileAB[i] || (path.Contains(m_AllFileAB[i]) && (path.Replace(m_AllFileAB[i], "")[0] == '/')))
                {
                    return true;
                }
            }

            return false;
        }

        static void SetAssetBundleName(string abName, string path)
        {
            AssetImporter assetImport = AssetImporter.GetAtPath(path);
            AddPathToABDic(path, abName);
            if (assetImport != null)
            {
                assetImport.assetBundleName = abName;
            }
        }

        static void SetAssetBundleName(string abName, List<string> path)
        {
            for (int i = 0; i < path.Count; i++)
            {
                AssetImporter assetImport = AssetImporter.GetAtPath(path[i]);
                if (assetImport != null)
                {
                    assetImport.assetBundleName = abName;
                }
            }
        }

        /// <summary>
        /// 文件夹路径的ab包
        /// </summary>
        /// <param name="path"></param>
        /// <param name="abName"></param>
        static void AddPathToABDic(string path, string abName)
        {
            if (m_DirToABDic.ContainsKey(path))
            {
                Debug.LogError("已经包含此key" + path);
            }
            else
            {
                m_DirToABDic.Add(path, abName);
            }
        }

        static void BuildNew(BuildTarget target)
        {
            string[] allBundles = AssetDatabase.GetAllAssetBundleNames();
            //key值为ab包名，value为ab包内所有资源
            Dictionary<string, string> resPathToAssetBundlePathDic = new Dictionary<string, string>();
            for (int i = 0; i < allBundles.Length; i++)
            {
                string[] allBundlePath = AssetDatabase.GetAssetPathsFromAssetBundle(allBundles[i]);
                for (int j = 0; j < allBundlePath.Length; j++)
                {
                    if (allBundlePath[j].EndsWith(".cs"))
                        continue;

                    Debug.Log("此AB包" + allBundles[i] + "下面的资源文件" + allBundlePath[j]);
                    resPathToAssetBundlePathDic.Add(allBundlePath[j], allBundles[i]);
                }
            }

            WriteABDependencies(resPathToAssetBundlePathDic);
            ClearWasteABUI();

            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath, BuildAssetBundleOptions.ChunkBasedCompression, target);
            if (manifest == null)
            {
                Debug.Log("AssetBundle 打包失败");
            }
            else
            {
                Debug.Log("AssetBundle 打包完毕");
            }
        }

        static void ClearWasteABUI()
        {
            List<string> allFileNames = new List<string>();
            AssetDatabase.GetAllAssetBundleNames().ToList().AddG("values", "values.izip", "values.izip.xxx", "lua", "filter"
                , "console.css", "favicon.ico", "index.html", "AssetDependenciesCfg.bytes"
                , "bigmapnewlocationeffect", "bullythepeople", "headavatabody.allbodys", "magics", "CUDLR",
                "pulloutsword", "redshowtime", "runintomisstao", "steponthejourney", "StreamingAssets", "threefakeheros"
                 )
                .ForEach(s => allFileNames.AddG(s, s + ".meta", s + ".manifest", s + ".manifest.meta"));

            new DirectoryInfo(HSCTC.StreamingAssetsPath).GetFiles("*", SearchOption.AllDirectories)
                .Where(fi => !allFileNames.Contains(fi.Name))
                .ToList().ForEach(fi =>
                {
                    Debug.LogWarning("删除无效AB包: {0}".Eat(fi.Name));
                    File.Delete(fi.FullName);
                });
        }

        static void DeleteAB()
        {
            string[] allBundlesName = AssetDatabase.GetAllAssetBundleNames();
            if (Directory.Exists(Application.streamingAssetsPath))
            {
                DirectoryInfo direction = new DirectoryInfo(Application.streamingAssetsPath);
                FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    if (ContainABName(files[i].Name, allBundlesName) || files[i].Name.EndsWith(".meta") || files[i].Name.Contains("StreamingAssets"))
                    {
                        continue;
                    }
                    else
                    {
                        Debug.Log("此AB包不是此工程中的，删除：" + files[i].Name);
                        string path = Application.streamingAssetsPath + "/" + files[i].Name;
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                    }
                }
            }
        }

        static bool ContainABName(string name, string[] strs)
        {
            for (int i = 0; i < strs.Length; i++)
            {
                if (name.Contains(strs[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private static void WriteABDependencies(Dictionary<string, string> resPathToAssetBundlePathDic)
        {
            AssetBundleData bundleData = new AssetBundleData();
            bundleData.AllABList = new List<ABPath>();
            string path = Application.dataPath + "/BuildSource/";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string xmlPath = path + "AssetDependenciesCfg.xml";
            string bytePath = path + "AssetDependenciesCfg.bytes";
            if (File.Exists(xmlPath))
            {
                File.Delete(xmlPath);
            }

            if (File.Exists(bytePath))
            {
                File.Delete(bytePath);
            }

            Dictionary<string, string>.Enumerator dirToABIt = m_DirToABDic.GetEnumerator();
            while (dirToABIt.MoveNext())
            {
                ABPath abPath = new ABPath();
                abPath.Crc = Crc32.GetCrc32(dirToABIt.Current.Key);
                abPath.AssetBundleName = dirToABIt.Current.Value;
                abPath.DependAssetBundle = new List<string>();
                CheckDirectoryDependencies(dirToABIt.Current.Key, dirToABIt.Current.Value, abPath.DependAssetBundle, resPathToAssetBundlePathDic);
                bundleData.AllABList.Add(abPath);
            }
            string strAssetBundle = string.Empty;
            Dictionary<string, string>.Enumerator it = resPathToAssetBundlePathDic.GetEnumerator();
            while (it.MoveNext())
            {
                ABPath abPath = new ABPath();
                abPath.Crc = Crc32.GetCrc32(it.Current.Key);
                abPath.AssetBundleName = it.Current.Value;
                abPath.DependAssetBundle = new List<string>();
                string[] resDependencies = AssetDatabase.GetDependencies(it.Current.Key);
                for (int i = 0; i < resDependencies.Length; i++)
                {
                    string tempPath = resDependencies[i];
                    if (tempPath == it.Current.Key || tempPath.EndsWith(".cs"))
                    {
                        continue;
                    }

                    if (resPathToAssetBundlePathDic.TryGetValue(tempPath, out strAssetBundle))
                    {
                        if (strAssetBundle == it.Current.Value)
                        {
                            continue;
                        }

                        if (!abPath.DependAssetBundle.Contains(strAssetBundle))
                        {
                            abPath.DependAssetBundle.Add(strAssetBundle);
                        }
                    }
                }
                bundleData.AllABList.Add(abPath);
            }

            SerializeTools.SafeBinarySerilize(bytePath, bundleData);
            SerializeTools.SafeXmlSerialize(xmlPath, bundleData);
        }

        static void CheckDirectoryDependencies(string directory, string currentAB, List<string> dependList, Dictionary<string, string> resPathToAssetBundlePathDic)
        {
            if (Directory.Exists(directory))
            {
                DirectoryInfo direction = new DirectoryInfo(directory);
                FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
                int flieCount = files.Length;
                for (int i = 0; i < flieCount; i++)
                {
                    string path = files[i].FullName.Replace(@"\", "/");
                    path = path.Replace(Application.dataPath.Replace("Assets", ""), "");
                    string[] resDependencies = AssetDatabase.GetDependencies(path);
                    string strAssetBundle = string.Empty;
                    for (int j = 0; j < resDependencies.Length; j++)
                    {
                        string tempPath = resDependencies[j];
                        if (tempPath == path || tempPath.EndsWith(".cs"))
                        {
                            continue;
                        }

                        if (resPathToAssetBundlePathDic.TryGetValue(tempPath, out strAssetBundle))
                        {
                            if (strAssetBundle == currentAB)
                            {
                                continue;
                            }

                            if (!dependList.Contains(strAssetBundle))
                            {
                                dependList.Add(strAssetBundle);
                            }
                        }
                    }
                }
            }
        }
    }
}
#endif
