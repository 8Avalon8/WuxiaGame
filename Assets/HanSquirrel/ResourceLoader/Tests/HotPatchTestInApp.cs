using System.Collections;
using System.Linq;
using GLib;
using HSFrameWork.Common;
using HanSquirrel.ResourceManager;
using System.IO;
using System;
using UnityEngine;
using StrDict = System.Collections.Generic.Dictionary<string, string>;
using System.Text.RegularExpressions;

namespace HanSquirrel.ResourceLoader.Tests
{
    public class HotPatchTestInApp
    {

        //用于测试错误对话框
        //public const string TestHotPatchURLBase = "http://dragondemo.hanjiasongshu.com:10001/hsframeworkdevtesterror/hotpatch/static/";

        //正常测试
        public const string TestHotPatchURLBase = "http://dragondemo.hanjiasongshu.com:10001/hsframeworkdevtest/hotpatch/static/";

        //本CS开发者测试
        //public const string TestHotPatchURLBase = "http://dellnotebook:90/hsframeworkdevtest/hotpatch/static/";
        public const string TestHotPatchIndex = TestHotPatchURLBase + "index.txt";

        public const string PatchesTxt = "ios&android_patches.txt";

        private string _TestcaseIndex, _PatchesBinURL, _PatchesTxtURL;
        private int _CurrentCase;
        private IEnumerator STEP_0_DownloadTestcaseIndexFile(Action<Patches, string> onCompleted)
        {
            using (var www = new WWW(TestHotPatchIndex))
            {
                yield return www;
                if (www.isDone && string.IsNullOrEmpty(www.error))
                {
                    _TestcaseIndex = www.text;
                }
                else
                {
                    Debug.LogErrorFormat("测试无法进行：无法取得 [{0}] [{1}]", TestHotPatchIndex, www.error);
                    onCompleted(null, "测试无法进行：无法取得索引文件。");
                }
            }
        }

        private bool STEP_1_SetupTestCase(Action<Patches, string> onCompleted)
        {
            // "1.1.1/181008/"
            var testcases = Regex.Split(_TestcaseIndex, "\r\n|\r|\n").Where(x => x.Visible()).ToArray();
            if (testcases.Length == 0)
            {
                onCompleted(null, "测试无法进行，索引文件内容为空");
                return false;
            }

            _CurrentCase = HSUnityEnv.LastHotPatchTestCase.ExistsAsFile() ?
                    HSUnityEnv.LastHotPatchTestCase.ReadAllText().SafeToInt32(0) : -1;
            _CurrentCase++;

            if (_CurrentCase < 0 || _CurrentCase >= testcases.Length)
                _CurrentCase = 0;

            _PatchesBinURL = TestHotPatchURLBase + testcases[_CurrentCase] + HSUnityEnv.CurrentBuildTarget + "_patches.bin";
            _PatchesTxtURL = TestHotPatchURLBase + testcases[_CurrentCase] + PatchesTxt;
            return true;
        }

        private string[] _Contents = new string[]
        {
            "劝君莫惜金缕衣，\r\n劝君惜取少年时。\r\n花开堪折直须折，\r\n莫待无花空折枝。",
            "十步杀一人，\r\n千里不留行。\r\n事了拂衣去，\r\n深藏身与名。"
        };

        private IEnumerator STEP_2_DownLoadPatches(Action<Patches, string> onCompleted)
        {
            using (var www = new WWW(_PatchesTxtURL))
            {
                yield return www;
                if (www.isDone && string.IsNullOrEmpty(www.error))
                {
                    var patches = ToolsShared.DeserializeXML<GameVersions>(
                        GameVesionXMLHead +
                        www.text +
                        GameVesionXMLTail).gameVersions[0].patches;

                    patches.content = _Contents[_CurrentCase % _Contents.Length];
                    onCompleted(patches, "测试所用的GameVersion生成完毕");
                }
                else
                {
                    Debug.LogErrorFormat("测试无法进行：无法取得 [{0}] [{1}]", _PatchesTxtURL, www.error);
                    onCompleted(null, "测试无法进行：无法取得 {0}。".f(PatchesTxt));
                    yield break;
                }
            }
        }

        public IEnumerator PrepareFakeHotPatch(Action<Patches, string> onCompleted)
        {
            bool stopped = false;
            yield return STEP_0_DownloadTestcaseIndexFile((a, b) =>
            {
                onCompleted(a, b);
                stopped = true;
            });

            if (stopped) yield break;

            if (!STEP_1_SetupTestCase(onCompleted))
                yield break;

            yield return STEP_2_DownLoadPatches(onCompleted);
        }

        public IEnumerator CheckAfterHotPatched(Action<bool, string> callback)
        {
            byte[] dictBytes;
            using (var www = new WWW(_PatchesBinURL))
            {
                yield return www;
                if (www.isDone && string.IsNullOrEmpty(www.error))
                {
                    dictBytes = www.bytes;
                }
                else
                {
                    Debug.LogErrorFormat("测试无法进行：无法取得 [{0}] [{1}]", _PatchesBinURL, www.error);
                    callback(false, "测试无法进行：无法取得 {0}。".f(PatchesTxt));
                    yield break;
                }
            }

            StrDict md5DictShould;
            try
            {
                md5DictShould = DirectProtoBufTools.Deserialize<StrDict>(dictBytes);
            }
            catch
            {
                Debug.LogErrorFormat("测试无法进行：无法反序列化 {0}。".f(_PatchesBinURL));
                callback(false, "测试无法进行：无法反序列化服务端的MD5Dict。");
                yield break;
            }

            yield return CheckResult(md5DictShould, callback);
        }

        private IEnumerator CheckResult(StrDict md5DictShould, Action<bool, string> callback)
        {
            foreach (var ab in new DirectoryInfo(HSUnityEnv.HotPatchFolder)
                                .GetFiles()
                                .Where(x => x.Extension.ToLower() != ".md5"))
            {
                string svrMD5;
                if (!md5DictShould.TryGetValue(ab.Name.ToLower(), out svrMD5))
                {
                    Debug.LogErrorFormat("自测错误。本地多了 {0}".f(ab.Name));
                    callback(false, "自测错误。本地多了 {0}".f(ab.Name));
                    yield break;
                }

                var localMD5 = MD5Utils.Encrypt(ab.ReadAllBytes()).ToUpper();
                if (localMD5 != svrMD5)
                {
                    Debug.LogErrorFormat("自测错误。{0} 和服务端不一致：[{1}]VS[{2}]。".f(ab.Name, localMD5, svrMD5));
                    callback(false, "自测错误。{0} 和服务端不一致。".f(ab.Name));
                    yield break;
                }

                if (!CheckLoaded(ab.Name, localMD5, callback))
                    yield break;

                md5DictShould.Remove(ab.Name.ToLower());
            }

            if (md5DictShould.Any(kv => !CheckLoaded(kv.Key, kv.Value, callback)))
                yield break;

            HSUnityEnv.LastHotPatchTestCase.WriteAllText(_CurrentCase.ToString());
            callback(true, "自测通过");
        }

        private bool CheckLoaded(string abName, string shouldMD5, Action<bool, string> callback)
        {
            var byteLoaded = BinaryResourceLoader.LoadBinary(ABNameToResourceKey(abName));
            if (byteLoaded == null)
            {
                Debug.LogErrorFormat("自测错误。客户端无法读取 [{0}]".f(abName));
                callback(false, "自测错误。客户端无法读取 [{0}]".f(abName));
                return false;
            }

            var loadedMD5 = MD5Utils.Encrypt(byteLoaded).ToUpper();
            if (loadedMD5 != shouldMD5)
            {
                Debug.LogErrorFormat("自测错误。{0}加载的和期望不一致：[{1}]VS[{2}]。".f(abName, loadedMD5, shouldMD5));
                callback(false, "自测错误。{0}加载的期望的不一致。".f(abName));
                return false;
            }
            return true;
        }

        public static string ABNameToResourceKey(string abName)
        {
            return "Assets/StreamingAssets/" + abName;
        }

        public const string GameVesionXMLHead =
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<gameversions>
    <gameversion v = ""0.1"" >
";

        public const string GameVesionXMLTail =
@"    </gameversion>
</gameversions>";
    }
}
