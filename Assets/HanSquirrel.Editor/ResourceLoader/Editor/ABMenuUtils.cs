using GLib;
using UnityEditor;
using UnityEngine;
using NUnit.Framework;
using HSFrameWork.Common;
using AiUnity.NLog.Core.Common;
using System.Threading;
using HSFrameWork.ConfigTable.Editor;
using HanSquirrel.ResourceManager.Editor.Inner;

namespace HanSquirrel.ResourceManager.Editor
{
    /// <summary>
    /// 和AB包相关的一些菜单项
    /// </summary>
    public class ABMenuUtils
    {
        /// <summary>
        /// 显示当前PrefabPool的使用状况。
        /// </summary>
        [MenuItem("Tools♥/码农专用/显示Pool状况")]
        public static void DisplayPool()
        {
            Debug.Log(ResourceLoader.DumpCurrentPoolStatus());
        }
    }
}

namespace HanSquirrel.ResourceManager.Editor.Test
{
    /// <summary>
    /// 和AB包相关的一些菜单项
    /// </summary>
    public class ABMenuTestUtils
    {
        //[MenuItem("码农专用/测试CE")]
        public static void TestCE()
        {
            var orgData = HSCTC.ValuesFile.ReadAllBytes();
            byte[] appData;

            var testFolder = "Assets/StreamingAssets/George/".CreateDir();

            var cefile = testFolder + "ce.bin";
            using (HSUtils.ExeTimerEnd("CE"))
                BinaryResourceLoader.SaveCEBinary(HSCTC.ValuesFile, cefile);
            using (HSUtils.ExeTimerEnd("DE-CE"))
                appData = BinaryResourceLoader.LoadCEBinary(cefile);
            Assert.That(appData.EqualsG(orgData));

            var cev0File = testFolder + "cev0.bin";
            using (HSUtils.ExeTimerEnd("CEV0"))
                BinaryResourceLoader.LZMADESSave(HSCTC.ValuesFile, cev0File);
            using (HSUtils.ExeTimerEnd("DE-CEV0"))
                appData = BinaryResourceLoader.LoadDeDESDeLZMA(cev0File);
            Assert.That(appData.EqualsG(orgData));

            var cev0File1 = testFolder + "cev0_1.bin";
            using (HSUtils.ExeTimerEnd("CEV0"))
                BinaryResourceLoader.LZMADESSave(HSCTC.ValuesFile.ReadAllBytes(), cev0File1);
            using (HSUtils.ExeTimerEnd("DE-CEV0"))
                appData = BinaryResourceLoader.LoadDeDESDeLZMA(cev0File1);
            Assert.That(appData.EqualsG(orgData));

            var desFile = testFolder + "des.bin";
            using (HSUtils.ExeTimerEnd("des"))
                BinaryResourceLoader.DESSave(HSCTC.ValuesFile.ReadAllBytes(), desFile);
            using (HSUtils.ExeTimerEnd("de-des"))
                appData = BinaryResourceLoader.LoadDeDES(desFile);
            Assert.That(appData.EqualsG(orgData));

            var lzmaFile = testFolder + "lzma.bin";
            using (HSUtils.ExeTimerEnd("lzma"))
                BinaryResourceLoader.LZMASave(HSCTC.ValuesFile.ReadAllBytes(), lzmaFile);
            using (HSUtils.ExeTimerEnd("de-lzma"))
                appData = BinaryResourceLoader.LoadDeLZMA(lzmaFile);
            Assert.That(appData.EqualsG(orgData));
        }

        //[MenuItem("码农专用/测试WWW本地")]
        public static void WWWLoadLocal()
        {
            var www = new WWW("file://" + NLogConfigFile.ConfigFile);
            using (HSUtils.ExeTimer("Loading "))
            {
                while (!www.isDone)
                {
                    Thread.Sleep(10);
                }
            }
        }

        //[MenuItem("码农专用/LoadFromResources")]
        public static void LoadFromResources()
        {
            var a = Resources.Load<TextAsset>(HSUnityEnv.ForceHotPatchTestInAppTagKey);
            Debug.Log(a == null ? "NULL" : "OK");
            a = Resources.Load<TextAsset>("HSFrameWork/hotpatchtestinapp");
            Debug.Log(a == null ? "NULL" : "OK");
            a = Resources.Load<TextAsset>("Version");
            Debug.Log(a == null ? "NULL" : "OK");
        }

        //[MenuItem("码农专用/从AB包的各种加载")]
        public static void LoadObjFromAB()
        {
            using (SmartABCacheCleaner.NewInstance)
            {
                AssetBundle assetBundle = AssetBundle.LoadFromFile(HSCTC.StreamingAssetsPath.Sub("audios"));
                Debug.Log("Bundle: " + assetBundle.name + " : " + string.Join(", ", assetBundle.GetAllAssetNames()));

                var a1 = assetBundle.LoadAsset<AudioClip>("assets/buildsource/audios/jhx/battle1.mp3");
                Assert.That(a1 != null);

                var a2 = assetBundle.LoadAsset<AudioClip>("battle1");
                Assert.That(a2 != null);

                Assert.That(assetBundle.LoadAsset<AudioClip>("Atk00") != null);
            }
        }

        //[MenuItem("码农专用/清理有名字的AB包")]
        public static void UnloadABNamed()
        {
            DisplayAllLoadedABs();
            SmartABCacheCleaner.UnloadNamed();
            DisplayAllLoadedABs();
        }

        //[MenuItem("码农专用/显示AB包内容")]
        public static void DisplayABInfo()
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(HSCTC.InDebug("TestOutput").Sub("testbundle"));
            //AssetBundle assetBundle = AssetBundle.LoadFromFile(HSCTC.StreamingAssetsPath.Sub("teampvepanel"));
            Debug.Log("Bundle: " + assetBundle.name + " : " + string.Join(", ", assetBundle.GetAllAssetNames()));
        }

        //[MenuItem("码农专用/列出当前加载AB包")]
        public static void DisplayAllLoadedABs()
        {
            AssetBundle[] bundles = Resources.FindObjectsOfTypeAll<AssetBundle>();
            Debug.Log("number of bundles " + bundles.Length);

            for (int i = 0; i < bundles.Length; i++)
            {
                Debug.Log("Bundle: " + bundles[i].name + " : " + string.Join(", ", bundles[i].GetAllAssetNames()));
            }

            Resources.UnloadUnusedAssets();
        }

        //[MenuItem("码农专用/取得依赖")]
        public static void GetDeps()
        {
            foreach (var x in AssetDatabase.GetDependencies("Assets/3D/Model/NvModle/Materials/Avata-D10-body.mat"))
                Debug.Log(x);
        }

        //[MenuItem("码农专用/测试任意打包")]
        public static void TestBuild()
        {
            AssetBundleBuild[] buildMap = new AssetBundleBuild[1];

            buildMap[0].assetBundleName = "testbundle";
            buildMap[0].assetNames = new string[1];
            buildMap[0].assetNames[0] = "Assets/BuildSource/Audios";

            var manifest = BuildPipeline.BuildAssetBundles(HSCTC.InDebug("TestOutput").CreateDir(), buildMap, BuildAssetBundleOptions.None, BuildTarget.Android);
            if (manifest == null)
                Debug.LogError("AssetBundle 打包失败");
            else
                Debug.Log("AssetBundle 打包完毕");
        }

        //[MenuItem("码农专用/直接打包Values")]
        public static void TestBuildValues()
        {
            var abName = "testbundle";
            var valuePath = "Assets/UnitTestAssets/ABMAudioTest/BuildSource/values.bytes";
            var streamAssetPath = HSCTC.InDebug("TestOutput").CreateDir();
            AssetBundleBuild[] buildMap = new AssetBundleBuild[1];

            buildMap[0].assetBundleName = abName;
            buildMap[0].assetNames = new string[1];
            buildMap[0].assetNames[0] = valuePath;

            using (SmartABCacheCleaner.NewInstance)
            {
                var manifest = BuildPipeline.BuildAssetBundles(streamAssetPath, buildMap, BuildAssetBundleOptions.None, BuildTarget.Android);
                if (manifest == null)
                {
                    Debug.LogError("AssetBundle 打包失败");
                    return;
                }
            }
            Debug.Log("AssetBundle 打包完毕");

            using (SmartABCacheCleaner.NewInstance)
            {
                AssetBundle assetBundle = AssetBundle.LoadFromFile(streamAssetPath.Sub(abName));
                if (assetBundle == null)
                {
                    Debug.LogError("AssetBundle 加载失败");
                    return;
                }

                var values = assetBundle.LoadAsset<TextAsset>(valuePath);
                if (values == null)
                {
                    Debug.LogError("LoadAsset 失败");
                    return;
                }

                Assert.That(values.bytes.EqualsG(valuePath.ReadAllBytes()));
            }
            Debug.Log("★★★★ 恭喜。测试通过");
        }

    }
}
