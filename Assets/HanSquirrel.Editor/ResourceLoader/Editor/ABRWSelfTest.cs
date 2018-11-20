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
using NUnit.Framework;
using HSFrameWork.ConfigTable.Editor.Impl;
using HanSquirrel.ResourceManager.Impl;
using HSFrameWork.ConfigTable.Editor;
using HanSquirrel.ResourceManager.Editor.Inner;

namespace HanSquirrel.ResourceManager.Editor.Test
{
    /// <summary>
    /// AB包打包和读包完整测试（100%覆盖每个资源文件）。
    /// </summary>
    //[TestFixture]
    public class ABWriteReadSelfTest
    {
        //[Test]
        public void DoWork(ABBuildArg arg)
        {
            using (ProgressBarAutoHide.Get(100))
                DoWorkInner(arg);
        }

        public void DoWorkInner(ABBuildArg arg)
        {
            List<Tuple<string, string>> allPathAbList = new List<Tuple<string, string>>();
            arg.AbFolderDict.ForEachG(kv =>
            {
                if (!kv.Value.Exists())
                {
                    Debug.LogWarningFormat("预定义的路径 [{0}] 不存在。", kv.Value);
                    return;
                }
                allPathAbList.Add(Tuple.Create(kv.Value, kv.Key.ToLower()));

                if (kv.Value.ExistsAsFolder())
                {
                    allPathAbList.AddRange(
                        AssetBundleEditorV2.EnumAllResourceFiles(kv.Value)
                            .Select(file => Tuple.Create(file, kv.Key.ToLower())));
                    //预定义目录下的每个文件都正确打包，可以完整加载
                }
            });

            allPathAbList.AddRange(AssetDatabase.FindAssets("t:Prefab", arg.PrefabSearchPaths.ToArray())
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(prefabFile => Tuple.Create(prefabFile, string.Empty)));
            //预定义Prefab目录下的每个文件都正确打包，可以完整加载

            allPathAbList.Reverse();
            AssetBundleEditorV2 abeForDepOnly = new AssetBundleEditorV2();
            for (int i = 0; i < allPathAbList.Count; i++)
            {
                if (i % 10 == 0)
                    EditorUtility.DisplayProgressBar("逐个资源端对端测试 {0}/{1}".f(i, allPathAbList.Count), allPathAbList[i].Item1, i * 1.0f / allPathAbList.Count);
                Assert_ResourcePath_In_AB(allPathAbList[i].Item1, arg, allPathAbList[i].Item2, abeForDepOnly);
            }
        }

        private static void Assert_ResourcePath_In_AB(string pathKey, ABBuildArg arg, string abName, AssetBundleEditorV2 abeForDepOnly)
        {
            using (var scc = SmartABCacheCleaner.NewInstance)
            {
                var abm = new AssetBundleManager();
                abm.GetABFilePathDelegate = x => arg.OutputFolder.Sub(x);

                var pair = abm.GetResourceABPair(pathKey);
                Mini.ThrowNullIf(pair, "打包索引中漏掉：[{0}]".f(pathKey));
                if (abName == String.Empty)
                    abName = pair.ABName;
                Assert.That(abName == pair.ABName, "索引文件中的AB包名字和预期不符 [{0}] [{1}] [{2}]".f(abName, pair.ABName, pathKey));
                //索引文件正确（该路径的AB包符合预期）

                var dict0 = abm.CloneCachedABDevOnly();
                Assert.That(dict0.Count == 0);
                var ab = abm.LoadABCachedByPath(pathKey);
                Assert.That(abName == ab.name);
                //加载后的AB包名称等于预设的

                var dict1 = abm.CloneCachedABDevOnly();
                Assert.That(dict1.Count > 0);
                //AB包被缓存

                Assert.That(dict1.Count == (pair.DepdentABNames == null ? 1 : pair.DepdentABNames.Count + 1));
                //加载的AB包个数等于索引中的。

                Assert.That(dict1.All(x => x.Value.RefCount == 1));
                //被缓存的AB包的RefCount都是1

                Assert.That(dict1.Keys.ToList().SortC(), Is.EquivalentTo(pair.AllABNames.ToList().SortC()));
                //加载的AB包和索引中的相同

                Assert.That(dict1.Values.Select(x => x.AB.name).ToList().SortC(), 
                    Is.EquivalentTo(scc.Added().Select(x => x.name).ToList().SortC()), "[{0}] 加载的AB包和系统不同".f(pathKey));
                //加载的AB包和系统中加载的相同

                Assert.That(ab, Is.SameAs(dict1[abName].AB));
                //返回的AB包和缓存的AB包相同

                var pathSetInAb = new HashSet<string>(ab.GetAllAssetNames());
                if (pathKey.ExistsAsFile())
                {
                    Mini.ThrowIfFalse(pathSetInAb.Contains(pathKey.ToLower()),
                        "[{0}] 并未包含在实际AB包 [{1}] 中。".f(pathKey, abName));
                    //文件的确在AB包之中；

                    HashSet<string> unusedABSet = new HashSet<string>(pair.AllABNames);
                    var abFilesDict = dict1.ToDictionary(x => x.Key, x => new HashSet<string>(x.Value.AB.GetAllAssetNames()));
                    abeForDepOnly.GetDepCached(pathKey).Where(AssetBundleEditorV2.IsResourceFile).ForEachG(depFile =>
                    {
                        bool found = false;
                        abFilesDict.ForEachG(d =>
                        {
                            if (d.Value.Contains(depFile.ToLower()))
                            {
                                found = true;
                                unusedABSet.Remove(d.Key);
                            }
                        });
                        Assert.That(found, "{0}所依赖的{1}并没有被加载。".f(pathKey, depFile));
                        //每个文件的依赖文件都已经正确加载了。
                    });
                    Assert.That(unusedABSet.Count == 0,
                        "[{0}] 并没有被 [{1}] 依赖，然而被加载了。".f(unusedABSet.ToList().JoinC(", "), pathKey));
                    //没有加载多余的AB包。
                }
                else
                {
                    AssetBundleEditorV2.EnumAllResourceFiles(pathKey).ForEachG(
                        x => Mini.ThrowIfFalse(pathSetInAb.Contains(x.ToLower()),
                            "[{0}] 并未包含在实际AB包 [{1}] 中。".f(x, abName)));
                    //文件夹中的所有文件的确包含在AB包中。
                }
            }
        }
    }
}
