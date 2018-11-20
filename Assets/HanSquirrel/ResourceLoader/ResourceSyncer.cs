using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using HSFrameWork.Common;
using GLib;
using HanSquirrel.ResourceLoader.Tests;

namespace HanSquirrel.ResourceManager
{
    /// <summary>
    /// IResourceSyner运行需要的外部实现，实际上就是热更界面。
    /// </summary>
    public interface IResourceSynerContext
    {
        /// <summary>
        /// 显示热更主界面
        /// </summary>
        void ShowHotFixPanel(string msg, string hotcontent, string confirmText, string cancelText, Action confirmCallback, Action cancelCallback = null);
        /// <summary>
        /// 显示确认-取消对话框
        /// </summary>
        void ShowConfirmPanel(string msg, Action confirmCallback, Action cancelCallback = null, string confirmText = "确定", string cancelText = "取消");
        /// <summary>
        /// 显示确认对话框
        /// </summary>
        void ShowMessageBox(string title, string msg, Color color, Action callback = null, string buttonText = "确认");
        /// <summary>
        /// 启动协程
        /// </summary>
        Coroutine StartCoroutine(IEnumerator routine);
        /// <summary>
        /// 本地当前的热更版本
        /// </summary>
        int LocalPatchVersion { get; set; }
        /// <summary>
        /// 加载文本资源
        /// </summary>
        string LoadText(string key, string source, params object[] paras);
    }

    /// <summary>
    /// 热更新资源同步器
    /// 
    /// 用于同步及下载热更新包
    /// 说明：依次对比传入的更新文件与本地缓存文件的md5
    /// ，如果不一致，则下载并覆盖。
    /// 
    /// 本地缓存不会计算文件的md5，只对比其md5索引文件（xxxx.md5)
    /// </summary>
    public interface IResourceSyncer
    {
        /// <summary> 是否正在下载 </summary>
        bool IsDownloading { get; }
        /// <summary> 整体进度(0-1) </summary>
        float Progress { get; }
        /// <summary> 当前工作状态 </summary>
        string Message { get; }

        /// <summary> 已经耗费的时间 </summary>
        int Elapsed { get; }

        /// <summary>
        /// 下载速度 KB/S
        /// </summary>
        float Speed { get; }

        /// <summary>
        /// 总共已经下载的字节数
        /// </summary>
        int TotalDownloadedSize { get; }

        /// <summary>
        /// 总共需要下载的字节数(KB)
        /// </summary>
        float TotalSizeKB { get; }

        /// <summary>
        /// 总共需要下载的文件个数
        /// </summary>
        int TotalFileCount { get; }

        /// <summary>
        /// 当前下载文件的大小
        /// </summary>
        float CurrentFileSizeKB { get; }

        /// <summary>
        /// 当前文件已经下载的大小
        /// </summary>
        int CurrentFileDownloadedSize { get; }

        /// <summary>
        /// 当前正在下载的文件名称
        /// </summary>
        string CurrentFile { get; }

        /// <summary>
        /// 在 DoWork 之前调用。会根据当前的设置来修改gv的paches。
        /// </summary>
        IEnumerator SmartModifyGameVersionInfo(IResourceSynerContext ui, GameVersionInfo gv, Action onCompleted);

        /// <summary>
        /// 开始工作。在工作成功完成后会调用callback。如果出现错误会通过 ui 来调用界面提示用户。
        /// 如果错误无法重试，则界面只能提示用户退出程序。
        /// </summary>
        void DoWork(IResourceSynerContext ui, GameVersionInfo gv, Action callback);

        /// <summary>
        /// 仅仅开发测试使用。不懂误用。
        /// </summary>
        IEnumerator DoWorkDevOnly(GameVersionInfo gv);
    }

    /// <summary>
    /// IResourceSyner的工厂类。
    /// </summary>
    public static class ResourceSyncerFactory
    {
        /// <summary>
        /// 开发者内部使用，不懂勿用
        /// </summary>
        public static bool UnitTestMode = false;

        /// <summary>
        /// 开发者内部使用，不懂勿用
        /// </summary>
        public static string UnitTestPlatform;

        /// <summary>
        /// 开发者内部使用。可以调整下以便看到进度条增长。
        /// </summary>
        public const float WaitBeforeEachDownload = 0.0f;

        /// <summary>
        /// 创建一个新的实例。每个实例只能用一次。
        /// </summary>
        public static IResourceSyncer CreateNew()
        {
            return new ResourceSyncer() as IResourceSyncer;
        }

        private sealed class ResourceSyncer : IResourceSyncer
        {
            #region 公开接口实现
            public IEnumerator SmartModifyGameVersionInfo(IResourceSynerContext ui, GameVersionInfo gv, Action onCompleted)
            {
                _Context = ui;
                if (Resources.Load<TextAsset>(HSUnityEnv.ForceHotPatchTestInAppTagKey) != null)
                {
                    if (gv.patches == null || gv.patches.files == null || gv.patches.files.Length == 0)
                    {//仅仅在有标记，并且服务端下发的patches为空的时候才会 强制测试HotPatch
                        HSUtils.Log("启动HotFix自动测试。");
                        _GameVersionInfoBK = gv;
                        _HotPatchTestInApp = new HotPatchTestInApp();
                        UnitTestMode = true;
                        UnitTestPlatform = HSUnityEnv.CurrentBuildTarget;
                        yield return _HotPatchTestInApp.PrepareFakeHotPatch((patches, message) =>
                        {
                            if (patches == null)
                            {
                                _Context.ShowMessageBox("严重错误", "[{0}]: HotFix自测失败，请联系程序员。按确定键关闭程序。".f(message), Color.white, HSUtilsEx.ExitApp);
                            }
                            else
                            {
                                _GameVersionInfoBK.patches = patches;
                                onCompleted();
                            }
                        });
                        yield break;
                    }
                    else
                    {
                        HSUtils.LogWarning("因为目前有真正的HotFix，因此无法进行强制HotFix测试。");
                    }
                }

                //GG 20181011 之前的逻辑
                if (Application.isEditor)
                {
                    if (UnitTestMode)
                        HSUtils.LogWarning("目前是单元测试模式，因此会在编辑模式下热更。");
                    else
                    {
                        HSUtils.LogWarning("目前有真正的HotFix。编辑模式下缺省不支持。");
                        gv.patches = null;
                    }
                }
                onCompleted();
            }

            public string Message { get { return _MessageDelegate == null ? "" : _MessageDelegate(); } }
            public bool IsDownloading { get { return _Stopwatch.IsRunning; } }
            public float Progress { get { return TotalSizeKB == 0 ? 0 : TotalDownloadedSize / 1024.0f / TotalSizeKB; } }

            public int Elapsed { get { return (int)_Stopwatch.Elapsed.TotalMilliseconds; } }

            public float Speed { get { return Elapsed > 0 ? TotalDownloadedSize * 1.0f / Elapsed : 0.0f; } }
            public float TotalSizeKB { get; private set; }
            public int TotalFileCount { get; private set; }

            public int TotalDownloadedSize { get { return _DownloadedFileSize + CurrentFileDownloadedSize; } }
            public int CurrentFileDownloadedSize { get { return currentWWW == null ? 0 : currentWWW.bytesDownloaded; } }

            public float CurrentFileSizeKB { get; private set; }
            public string CurrentFile { get; private set; }

            public void DoWork(IResourceSynerContext ui, GameVersionInfo gv, Action callback)
            {
                ui.StartCoroutine(DoWorkInner(ui, gv, callback));
            }

            public IEnumerator DoWorkDevOnly(GameVersionInfo gv)
            {
                return DoWorkInner(null, gv, null);
            }

            public IEnumerator DoWorkInner(IResourceSynerContext ui, GameVersionInfo gv, Action callback)
            {
                _SSH.Start_ExceptionIfStarted("应用编程错误：ResourceSyncer只能用一次");
                _Context = ui;
                _version = gv.version;
                _patches = gv.patches;
                _callback = callback;
                S0_ReadPackedMd5();
                //同步临时缓存目录
                yield return SyncAssetbundles();
            }
            #endregion

            #region 私有
            private StartStopHelper _SSH = new StartStopHelper();
            private WWW currentWWW;
            private int _DownloadedFileSize;
            private IResourceSynerContext _Context;
            private Action _callback = null;
            private Patches _patches = null;
            private string _version;
            private Dictionary<string, string> _PackedMd5 = new Dictionary<string, string>();

            //读取打包时MD5码
            private void S0_ReadPackedMd5()
            {
                _PackedMd5.Clear();
                TextAsset md5 = Resources.Load<TextAsset>("ABMD5");
                if (md5 == null)
                {
                    Debug.LogWarning("找不到ABMD5.txt，请确定StreamingAsset下没有任何资源。");
                    return;
                }

                string[] all = md5.text.Split('\n');
                for (int i = 0; i < all.Length; i++)
                {
                    string temp = all[i];
                    if (temp.Split('/').Length > 1)
                    {
                        _PackedMd5.Add(temp.Split('/')[0], temp.Split('/')[1].Replace("\r", ""));
                    }
                }
            }

            private List<Patch> _tobeDownloadPatches = new List<Patch>();

            /// <summary>
            /// 同步此版本下的ASSETBUNDLE热更新资源
            /// </summary>
            private IEnumerator SyncAssetbundles()
            {
                HotPatch.DeleteAllWastePatches(_patches);
                if (_patches == null || _patches.files == null || _patches.files.Length == 0)
                {
                    if (_Context != null) _Context.LocalPatchVersion = 0;
                    return SmartCallback();
                }

                _tobeDownloadPatches.Clear();
                //统计需要下载的文件
                foreach (var patch in _patches.files)
                {
                    if (Application.platform == RuntimePlatform.Android && patch.platform.Contains("android"))
                        S2_AddDownloadList(patch); // 安卓只下载路径包含“android”的热更
                    else if (Application.platform == RuntimePlatform.IPhonePlayer && patch.platform.Contains("ios"))
                        S2_AddDownloadList(patch); // iOS只下载路径包含“ios”的热更
                    else if (UnitTestMode && patch.platform.Contains(UnitTestPlatform))
                        S2_AddDownloadList(patch); // 单元测试模式
                }

                TotalFileCount = _tobeDownloadPatches.Count;
                if (TotalFileCount == 0)
                {
                    Debug.LogWarning("当前可下载的文件数为0.");
                    if (_Context != null) _Context.LocalPatchVersion = _patches.version;
                    return SmartCallback();
                }
                else
                {//有更新包
                    TotalSizeKB = _tobeDownloadPatches.Sum(x => x.size);

                    if (_Context == null)
                    {   //表示正在无界面单元测试
                        return S5_StartDownload(() => { throw new Exception("下载HotPatch失败"); });
                    }

                    _Context.ShowHotFixPanel(
                        _Context.LoadText("ResourceSyncer_HasHotFix", "当前补丁（V{0}），有更新（{1}），请下载\r\n更新包大小为（{2}）",
                                _Context.LocalPatchVersion, _patches.version,
                                ((int)(TotalSizeKB * 1024)).ToMBKBB()),
                        _patches.content,
                        _Context.LoadText("ResourceSyncer_HasHotFix_Confirm", "下载"),
                        _Context.LoadText("ResourceSyncer_HasHotFix_Cancel", "退出"),
                        S3_CheckNetAndStartDownload,
                        HSUtilsEx.ExitApp);
                }
                return null;
            }

            private void S2_AddDownloadList(Patch patch)
            {
                string filePath = HSUnityEnv.InHotPatchFolder(patch.name);
                string md5FilePath = HSUnityEnv.InHotPatchFolder(patch.name + ".md5");
                //缓存中有文件
                if (File.Exists(filePath) && File.Exists(md5FilePath))
                {
                    //检测md5
                    string md5 = File.ReadAllText(md5FilePath);
                    if (md5 == patch.md5)
                    {
                        HSUtils.Log(patch.name + " 缓存md5检测一致，跳过下载");
                    }
                    else
                    {
                        HSUtils.Log(patch.name + " 缓存md5不一致，删除原文件并重新下载");
                        File.Delete(filePath);
                        File.Delete(md5FilePath);
                        _tobeDownloadPatches.Add(patch); //重新下载
                    }
                }
                else//缓存中没有文件,先判断是否与streamAssets目录有相同文件。打包时创建了所有AB包的MD5码，读取对比
                {
                    string apkABMd5 = "";
                    if (_PackedMd5.TryGetValue(patch.name, out apkABMd5))
                    {
                        //服务器md5码大写，本地小写，进行转换
                        if (apkABMd5.ToUpper() == patch.md5)
                        {
                            HSUtils.Log(patch.name + "与SteamAssets目录下md5检测一致，跳过下载");
                        }
                        else
                        {
                            HSUtils.Log(patch.name + "与SteamAssets目录下不一致，需要下载");
                            _tobeDownloadPatches.Add(patch);
                        }
                    }
                    else
                    {
                        HSUtils.Log(patch.name + "原有StreamAssets目录下无此Ab包，需要下载");
                        _tobeDownloadPatches.Add(patch);
                    }
                }
            }

            //检测网络是否在wifi环境下，并且开始下载
            private void S3_CheckNetAndStartDownload()
            {
                //wifi环境直接开始下载
                if (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
                {
                    S4_DoStartDownload();
                }
                else if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork)
                {
                    //否则给提示
                    _Context.ShowConfirmPanel(
                        _Context.LoadText("ResourceSyncer_ContinueDownload", "当前不是在wifi环境下，是否继续下载"),
                        S4_DoStartDownload,
                        HSUtilsEx.ExitApp);
                }
            }

            private void S4_DoStartDownload()
            {
                _Context.StartCoroutine(S5_StartDownload(() =>
                {
                    _Context.ShowConfirmPanel(_Context.LoadText("ResourceSyncer_DownloadError_Title", "错误") + "\r\n"
                        + _Context.LoadText("ResourceSyncer_DownloadError", "下载资源错误，请检查网络"),
                        S4_DoStartDownload,
                        HSUtilsEx.ExitApp,
                        _Context.LoadText("ResourceSyncer_RETRY", "重试"),
                        _Context.LoadText("ResourceSyncer_HasHotFix_Cancel", "退出"));
                }));
            }

            private IEnumerator S5_StartDownload(Action failCallback)
            {
                if (!HSUnityEnv.PersistentDataPath.ExistsAsFolder())
                {
                    if (_Context == null) throw new Exception("存储路径读取失败");
                    _Context.ShowMessageBox("", _Context.LoadText("ResourceSyncer_SaveError", "存储路径读取失败，您需要重启手机，再运行游戏"), Color.red);
                    yield break;
                }

                _Stopwatch.Start();
                for (int i = _tobeDownloadPatches.Count - 1; i >= 0; i--)
                {
                    var patch = _tobeDownloadPatches[i];
                    if (_Context != null)
                        _MessageDelegate = () => _Context.LoadText("ResourceSyncer_Downloading", "正在下载更新包{0}，速度{1:0.0}KB。已经下载{2:0.0}%", patch.name, Speed, Progress * 100);
                    HSUtils.Log("开始下载" + patch.getUrl());
                    if (WaitBeforeEachDownload > 0.0f)
                        yield return new WaitForSeconds(WaitBeforeEachDownload);

                    CurrentFile = patch.name;
                    CurrentFileSizeKB = patch.size;
                    using (DisposeHelper.Create(() =>
                    {
                        currentWWW = null;
                        CurrentFile = null;
                        CurrentFileSizeKB = 0;
                    }))
                    using (currentWWW = new WWW(patch.getUrl()))
                    {
                        yield return currentWWW;
                        if (currentWWW.isDone && string.IsNullOrEmpty(currentWWW.error))
                        {
                            HSUtils.Log(currentWWW.url + " 下载完毕");
                            HSUnityEnv.InHotPatchFolder(patch.name).WriteAllBytes(currentWWW.bytes);
                            HSUnityEnv.InHotPatchFolder(patch.name + ".md5").WriteAllText(patch.md5);
                            _DownloadedFileSize += currentWWW.bytesDownloaded;
                            _tobeDownloadPatches.RemoveAt(i);
                        }
                        else
                        {
                            _Stopwatch.Stop();
                            HSUtils.LogError("[{0}] 下载失败 [{1}]", currentWWW.url, currentWWW.error);
                            failCallback();
                            yield break;
                        }
                    }
                }

                if (_Context != null)
                    _Context.LocalPatchVersion = _patches.version;
                yield return SmartCallback();
            }

            private IEnumerator SmartCallback()
            {
                _Stopwatch.Stop();
                _MessageDelegate = () => _Context.LoadText("ResourceSyncer_Downloaded", "更新补丁下载完成。");
                if (_HotPatchTestInApp != null)
                    yield return _HotPatchTestInApp.CheckAfterHotPatched(OnHotPatchTestCheckCompleted);
                else if (_callback != null)
                    _callback();
            }

            private void OnHotPatchTestCheckCompleted(bool result, string message)
            {
                if (!result)
                {
                    _Context.ShowMessageBox("严重错误", "[{0}]: HotFix自测失败，请联系程序员。按确定键退出游戏。".f(message), Color.white, HSUtilsEx.ExitApp);
                }
                else
                {
                    Debug.Log("hotFit自测通过，删除所有测试HotFix文件。");
                    _GameVersionInfoBK.patches = null;
                    HotPatch.ColdInit(_GameVersionInfoBK);
                    _GameVersionInfoBK = null;
                    HotPatch.ClearAllHotPatchFiles();

                    _HotPatchTestInApp = null;
                    UnitTestMode = false;
                    UnitTestPlatform = null;
                    if (_callback != null)
                        _callback();
                }
            }

            /// <summary>
            /// 仅仅在启动强制热更测试的时候才会不为NULL
            /// </summary>
            private static HotPatchTestInApp _HotPatchTestInApp;
            /// <summary>
            /// 仅仅在启动强制热更测试的时候才会不为NULL
            /// </summary>
            private GameVersionInfo _GameVersionInfoBK;
            private System.Diagnostics.Stopwatch _Stopwatch = new System.Diagnostics.Stopwatch();
            private Func<string> _MessageDelegate;
            #endregion
        }
    }
}