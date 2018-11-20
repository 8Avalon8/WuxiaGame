#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine.TestTools;
using GLib;
using HSFrameWork.Common;
using NUnit.Framework;
using HanSquirrel.ResourceManager;
using System.IO;
using HSFrameWork.Common.Inner;

namespace HanSquirrel.ResourceLoader.Tests
{
    /// <summary>
    /// 仅仅只是测试ResourceSyner和HotPatch的客户端功能，HotPatchTool的服务端功能可用
    /// </summary>
    public class HotPatchTestFixture
    {
        public const string SimuPubFolder = "data/static/hanjiasongshu/";
        public const string SimuURLBase = "data/static/";
        public const string Version = "888";
        public const string ABNameA = "HotPatchTest_A";
        public const string ABNameB = "HotPatchTest_B";

        [UnitySetUp]
        public IEnumerator Setup()
        {
            HSUnityEnv.HotPatchFolder.CreateDir();
            ClearTestAbFromStreamingAssets();
            HotPatch.ClearAllHotPatchFiles();
            HSBooterShared.ColdBind(null);
            ResourceSyncerFactory.UnitTestMode = true;
            No_TestAB_InStreamingAssets();
            yield break;
        }

        [UnityTearDown]
        public IEnumerator TireDown()
        {
            ClearTestAbFromStreamingAssets();
            SimuPubFolder.SafeClearDirectory(HSUtils.Log);
            SimuURLBase.SafeDelFolder();
            HotPatch.ClearAllHotPatchFiles();
            HSBooterShared.ColdBind(null);
            ResourceSyncerFactory.UnitTestMode = false;
            ResourceSyncerFactory.UnitTestPlatform = null;
            HSUtils.Log("Tiring down");
            yield break;
        }

        [UnityTest]
        public IEnumerator AllCaseCombined()
        {
            using (HSUtils.ExeTimer("测试android的HotPatch"))
            {
                ResourceSyncerFactory.UnitTestPlatform = "android";
                yield return AddNewAB_CanHotPatched();
                yield return CurrentAB_CanHotPatched();
            }

            using (HSUtils.ExeTimer("测试ios的HotPatch"))
            {
                ResourceSyncerFactory.UnitTestPlatform = "ios";
                yield return AddNewAB_CanHotPatched();
                yield return CurrentAB_CanHotPatched();
            }
        }

        private IEnumerator CurrentAB_CanHotPatched()
        {
            SimuPubFolder.Sub(Version).SafeClearDirectory(HSUtils.Log);
            HotPatch.ClearAllHotPatchFiles();

            HSUtils.Log("☆☆☆ 初始状态，没有 A包和B包 。");
            CheckHotPatchFolder();
            CheckNoFinalAB(ABNameA);
            CheckNoFinalAB(ABNameB);

            HSUtils.Log("★★★ 将A包写入StreamingAsset。");
            var dataAndroidA0 = RandomBytes();
            var dataIOSA0 = RandomBytes();
            WriteABToStreamingAssets(ABNameA, dataAndroidA0, dataIOSA0);

            HSUtils.Log("☆☆☆ 检查终端StreamingAsset有A包存在，无Hotpatch。");
            CheckHotPatchFolder();
            CheckFinalAB(ABNameA, dataAndroidA0, dataIOSA0);

            HSUtils.Log("★★★ 更新A包。");
            var dataAndroidA1 = RandomBytes();
            var dataIOSA1 = RandomBytes();
            WriteABToStatic(Version, ABNameA, dataAndroidA1, dataIOSA1);
            yield return FullCycle(Version);

            HSUtils.Log("☆☆☆ 检查 A包被更新。");
            CheckHotPatchFolder(ABNameA);
            CheckFinalAB(ABNameA, dataAndroidA1, dataIOSA1);
            CheckNoFinalAB(ABNameB);

            HSUtils.Log("★★★ 将B包写入StreamingAsset。");
            var dataAndroidB0 = RandomBytes();
            var dataIOSB0 = RandomBytes();
            WriteABToStreamingAssets(ABNameB, dataAndroidB0, dataIOSB0);

            HSUtils.Log("☆☆☆ 检查A包热更，B包原始");
            CheckHotPatchFolder(ABNameA);
            CheckFinalAB(ABNameA, dataAndroidA1, dataIOSA1);
            CheckFinalAB(ABNameB, dataAndroidB0, dataIOSB0);

            HSUtils.Log("★★★ 更新 B 包。");
            var dataAndroidB1 = RandomBytes();
            var dataIOSB1 = RandomBytes();
            WriteABToStatic(Version, ABNameB, dataAndroidB1, dataIOSB1);
            yield return FullCycle(Version);

            HSUtils.Log("☆☆☆ 检查A包热更，B包热更");
            CheckHotPatchFolder(ABNameA, ABNameB);
            CheckFinalAB(ABNameA, dataAndroidA1, dataIOSA1);
            CheckFinalAB(ABNameB, dataAndroidB1, dataIOSB1);

            HSUtils.Log("★★★ 服务端删除 A 包，客户端更新");
            RemoveABFromStatic(Version, ABNameA);
            yield return FullCycle(Version);

            HSUtils.Log("☆☆☆ 检查客户端A原始，B热更");
            CheckHotPatchFolder(ABNameB);
            CheckFinalAB(ABNameA, dataAndroidA0, dataIOSA0);
            CheckFinalAB(ABNameB, dataAndroidB1, dataIOSB1);

            HSUtils.Log("★★★ 服务端删除B包，客户端更新");
            RemoveABFromStatic(Version, ABNameB);
            yield return FullCycle(Version);

            HSUtils.Log("☆☆☆ 客户端AB包都是原始");
            CheckHotPatchFolder();
            CheckFinalAB(ABNameA, dataAndroidA0, dataIOSA0);
            CheckFinalAB(ABNameB, dataAndroidB0, dataIOSB0);

            HSUtils.Log("★★★ 从StreamingAssets删除A包");
            RemoveABFromStreamingAssets(ABNameA);

            HSUtils.Log("☆☆☆ 客户端只有B包在StreamingAsset下");
            CheckHotPatchFolder();
            CheckNoFinalAB(ABNameA);
            CheckFinalAB(ABNameB, dataAndroidB0, dataIOSB0);

            HSUtils.Log("★★★ 从StreamingAssets删除B包");
            RemoveABFromStreamingAssets(ABNameB);

            HSUtils.Log("☆☆☆ 客户端AB包都没有了");
            CheckHotPatchFolder();
            CheckNoFinalAB(ABNameA);
            CheckNoFinalAB(ABNameB);

            HSUtils.Log("AddNewAB_CanHotPatched 测试通过");
        }

        private IEnumerator AddNewAB_CanHotPatched()
        {
            SimuPubFolder.Sub(Version).SafeClearDirectory(HSUtils.Log);
            HotPatch.ClearAllHotPatchFiles();

            HSUtils.Log("☆☆☆ 初始状态，没有 A包和B包 。");
            CheckHotPatchFolder();
            CheckNoFinalAB(ABNameA);
            CheckNoFinalAB(ABNameB);

            HSUtils.Log("★★★ 更新A包。");
            var dataAndroidA0 = RandomBytes();
            var dataIOSA0 = RandomBytes();
            WriteABToStatic(Version, ABNameA, dataAndroidA0, dataIOSA0);
            yield return FullCycle(Version);

            HSUtils.Log("☆☆☆ 检查终端仅仅有A包存在 。");
            CheckHotPatchFolder(ABNameA);
            CheckFinalAB(ABNameA, dataAndroidA0, dataIOSA0);
            CheckNoFinalAB(ABNameB);

            HSUtils.Log("★★★ 更新A包。");
            var dataAndroidA1 = RandomBytes();
            var dataIOSA1 = RandomBytes();
            WriteABToStatic(Version, ABNameA, dataAndroidA1, dataIOSA1);
            yield return FullCycle(Version);

            HSUtils.Log("☆☆☆ 检查 A包被更新。");
            CheckHotPatchFolder(ABNameA);
            CheckFinalAB(ABNameA, dataAndroidA1, dataIOSA1);
            CheckNoFinalAB(ABNameB);

            HSUtils.Log("★★★ 下发一个 abNameB。");
            var dataAndroidB0 = RandomBytes();
            var dataIOSB0 = RandomBytes();
            WriteABToStatic(Version, ABNameB, dataAndroidB0, dataIOSB0);
            yield return FullCycle(Version);

            HSUtils.Log("☆☆☆ 检查A包和B包都存在。");
            CheckHotPatchFolder(ABNameA, ABNameB);
            CheckFinalAB(ABNameA, dataAndroidA1, dataIOSA1);
            CheckFinalAB(ABNameB, dataAndroidB0, dataIOSB0);

            HSUtils.Log("★★★ 更新 B 包。");
            var dataAndroidB1 = RandomBytes();
            var dataIOSB1 = RandomBytes();
            WriteABToStatic(Version, ABNameB, dataAndroidB1, dataIOSB1);
            yield return FullCycle(Version);

            HSUtils.Log("☆☆☆ 再次检查 A包和B包内容。");
            CheckHotPatchFolder(ABNameA, ABNameB);
            CheckFinalAB(ABNameA, dataAndroidA1, dataIOSA1);
            CheckFinalAB(ABNameB, dataAndroidB1, dataIOSB1);

            HSUtils.Log("☆☆☆ 服务端删除 A 包，客户端不变");
            RemoveABFromStatic(Version, ABNameA);
            CheckHotPatchFolder(ABNameA, ABNameB);
            CheckFinalAB(ABNameA, dataAndroidA1, dataIOSA1);
            CheckFinalAB(ABNameB, dataAndroidB1, dataIOSB1);

            HSUtils.Log("★★★ 服务端删除 A 包，客户端更新");
            yield return FullCycle(Version);

            HSUtils.Log("☆☆☆ 检查客户端没有这个A包了");
            CheckHotPatchFolder(ABNameB);
            CheckNoFinalAB(ABNameA);
            CheckFinalAB(ABNameB, dataAndroidB1, dataIOSB1);

            HSUtils.Log("☆☆☆ 服务端删除B包，客户端不变");
            RemoveABFromStatic(Version, ABNameB);
            CheckHotPatchFolder(ABNameB);
            CheckNoFinalAB(ABNameA);
            CheckFinalAB(ABNameB, dataAndroidB1, dataIOSB1);

            HSUtils.Log("★★★ 服务端删除B包，客户端更新");
            yield return FullCycle(Version);

            HSUtils.Log("☆☆☆ 客户端AB包都没有了");
            CheckHotPatchFolder();
            CheckNoFinalAB(ABNameA);
            CheckNoFinalAB(ABNameB);

            HSUtils.Log("AddNewAB_CanHotPatched 测试通过");
        }

        private void CheckHotPatchFolder(params string[] files)
        {
            Assert.That(
                new DirectoryInfo(HSUnityEnv.HotPatchFolder).GetFiles()
                    .Select(fi => fi.Name).ToList().SortC(),
                Is.EquivalentTo(files
                    .SelectMany(x => new[] { x, x + ".md5" }).ToList().SortC()));
        }

        private void CheckNoFinalAB(string abName)
        {
            Assert.That(null == BinaryResourceLoader.LoadBinary(HotPatchTestInApp.ABNameToResourceKey(abName), false));
        }

        private void CheckFinalAB(string abName, byte[] dataAndroid, byte[] dataIOS)
        {
            if (ResourceSyncerFactory.UnitTestPlatform == "android")
                Assert.That(dataAndroid.EqualsG(BinaryResourceLoader.LoadBinary(HotPatchTestInApp.ABNameToResourceKey(abName))));
            else if (ResourceSyncerFactory.UnitTestPlatform == "ios")
                Assert.That(dataIOS.EqualsG(BinaryResourceLoader.LoadBinary(HotPatchTestInApp.ABNameToResourceKey(abName))));
            else
                Assert.That(false);
        }

        private void ClearTestAbFromStreamingAssets()
        {
            HSUnityEnv.StreamingAssetsFolder.Sub(ABNameA).Delete();
            HSUnityEnv.StreamingAssetsFolder.Sub(ABNameB).Delete();
        }

        private void RemoveABFromStreamingAssets(string abName)
        {
            HSUnityEnv.StreamingAssetsFolder.Sub(abName).Delete();
        }

        private void WriteABToStreamingAssets(string abName, byte[] dataAndroid, byte[] dataIOS)
        {
            if (ResourceSyncerFactory.UnitTestPlatform == "android")
                HSUnityEnv.StreamingAssetsFolder.Sub(abName).WriteAllBytes(dataAndroid);
            else if (ResourceSyncerFactory.UnitTestPlatform == "ios")
                HSUnityEnv.StreamingAssetsFolder.Sub(abName).WriteAllBytes(dataIOS);
            else
                Assert.That(false);
        }

        private void WriteABToStatic(string version, string abName, byte[] dataAndroid, byte[] dataIOS)
        {
            SimuPubFolder.Sub(version).Sub("android").Sub(abName).WriteAllBytes(dataAndroid);
            SimuPubFolder.Sub(version).Sub("ios").Sub(abName).WriteAllBytes(dataIOS);
        }

        private void RemoveABFromStatic(string version, string abName)
        {
            SimuPubFolder.Sub(version).Sub("android").Sub(abName).Delete();
            SimuPubFolder.Sub(version).Sub("ios").Sub(abName).Delete();
        }


        System.Random random = new System.Random();
        private byte[] RandomBytes()
        {
            byte[] data = new byte[256];
            random.NextBytes(data);
            return data;
        }

        /// <summary>
        /// 检查确保没有测试的AB包在StreamingAssets下面。
        /// </summary>
        private void No_TestAB_InStreamingAssets()
        {
            Assert.That(BetterStreamingAssets.GetFiles(".", "HotPatchTest_*").Count() == 0);
        }

        private IEnumerator FullCycle(string version)
        {
            HotPatchTools.GenHotPatchIndexFile(SimuPubFolder.Sub(version),
                "file://" + Path.GetFullPath(SimuURLBase).Replace("\\", "/"));

            var gv = ToolsShared.DeserializeXML<GameVersions>(
                HotPatchTestInApp.GameVesionXMLHead +
                SimuPubFolder.Sub(version).Sub("ios&android_patches.txt").ReadAllText() +
                HotPatchTestInApp.GameVesionXMLTail).gameVersions[0];
            HotPatch.ColdInit(gv);
            return ResourceSyncerFactory.CreateNew().DoWorkDevOnly(gv);
        }
    }
}
#endif
