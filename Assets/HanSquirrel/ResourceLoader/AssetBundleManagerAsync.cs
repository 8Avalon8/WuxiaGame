//GG 20180908 整理完毕
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using GLib;
using HSFrameWork.Common;

namespace HanSquirrel.ResourceManager.Impl
{
    public partial class AssetBundleManager
    {
        private enum LoadStatus
        {
            NONE,
            LOADING,
            LOADED
        }

        private class LoadGameObjectRequest
        {
            public ResourceABPair ABPair;
            public object CallbackState;
            public Action<GameObject, object> Callback;
            public LoadStatus ABLoadStatus = LoadStatus.NONE;
            public LoadStatus PrefabLoadStatus = LoadStatus.NONE;
        }

        private class MyMono : SingletonMB<MyMono, MyMono>
        {
            public event Action UpdateEvent;
            private void Update()
            {
                if (UpdateEvent != null)
                    UpdateEvent.Invoke();
            }
        }

        public void Dispose()
        {
            if (_RefreshBound)
            {
                MyMono.Instance.UpdateEvent -= RefreshLoadingStatus;
                _RefreshBound = false;
            }
        }

        private class MyYieldInstruction : CustomYieldInstruction
        {
            public override bool keepWaiting
            {
                get
                {
                    return !Ended;
                }
            }
            public bool Ended = false;
            public GameObject GO;
        }

        public IEnumerator LoadPrefabCachedAsync(string assetPath, object callBackState, Action<GameObject, object> onLoaded)
        {
            MyYieldInstruction myYI = new MyYieldInstruction();
            LoadPrefabCachedAsyncQuick(assetPath, null, (go, state) =>
            {
                myYI.GO = go;
                myYI.Ended = true;
            });
            yield return myYI;

            onLoaded(myYI.GO, callBackState);
        }

        private bool _RefreshBound = false;
        public void LoadPrefabCachedAsyncQuick(string assetPath, object callBackState, Action<GameObject, object> onLoaded)
        {
            GameObject go;
            if (_CachePrefabDict.TryGetValue(assetPath, out go))
            {
                _Logger.Trace("Prefab[{0}] cached.", assetPath.NameWithoutExt());
                onLoaded(go, callBackState);
                return;
            }

            var item = GetResourceABPair(assetPath);
            if (item == null)
            {
                _Logger.Trace("Prefab[{0}] load failed, NO SUCH PATH.", assetPath.NameWithoutExt());
                onLoaded(null, callBackState);
                return;
            }

            var request = new LoadGameObjectRequest
            {
                ABPair = item,
                CallbackState = callBackState,
                Callback = onLoaded
            };

            if (!ProcessRequest(request, true))
            {
                _Logger.Trace("Prefab[{0}] Queued.", assetPath.NameWithoutExt());
                _AsysncQueue.Add(request);
                if (!_RefreshBound)
                {
                    _RefreshBound = true;
                    MyMono.Instance.UpdateEvent += RefreshLoadingStatus;
                    _Logger.Debug("▼ 有异步资源加载任务，启动自动刷新。");
                }
            }
        }

        private List<LoadGameObjectRequest> _AsysncQueue = new List<LoadGameObjectRequest>();
        private Dictionary<string, AssetBundleCreateRequest> _LoadingAB = new Dictionary<string, AssetBundleCreateRequest>();
        private Dictionary<string, AssetBundleRequest> _LoadingPrefab = new Dictionary<string, AssetBundleRequest>();

        private bool ProcessRequest(LoadGameObjectRequest request, bool checkAB)
        {
            GameObject go;
            if (_CachePrefabDict.TryGetValue(request.ABPair.PathKey, out go))
            {
                //该Prefab已经在内存了，直接返回。
                _Logger.Trace("Prefab[{0}] cached.", request.ABPair.PathKey.NameWithoutExt());
                if (request.Callback != null)
                    request.Callback(go, request.CallbackState);
                return true;
            }

            if (request.ABLoadStatus != LoadStatus.LOADED && checkAB)
            {
                //AB包还没有加载，假设都加载完成了
                request.ABLoadStatus = LoadStatus.LOADED;
                foreach (var dep in request.ABPair.AllABNames)
                {
                    ABHot abHot;
                    if (!_LoadedAbDict.TryGetValue(dep, out abHot))
                    {   //没有加载
                        request.ABLoadStatus = LoadStatus.LOADING;
                        if (!_LoadingAB.ContainsKey(dep))
                        {   //没有正在加载
                            _ABLogger.Trace("AB[{0}] Async Loading ...", dep);
                            var requestAB = AssetBundle.LoadFromFileAsync(GetABFilePathDelegate(dep));
                            if (requestAB == null)
                            {   //加载失败
                                _ABLogger.Error("AB[{0}] Load failed.", dep);
                                _LoadedAbDict.Add(dep, null);
                                _CachePrefabDict.Add(request.ABPair.PathKey, null);
                                if (request.Callback != null)
                                    request.Callback(null, request.CallbackState);
                                return true;
                            }
                            else
                            {   //正在加载
                                _LoadingAB.Add(dep, requestAB);
                            }
                        }
                    }
                    else if (abHot == null)
                    {   //加载失败
                        _Logger.Trace("Prefab[{0}] 所依赖的 AB[{1}] 加载失败。", request.ABPair.PathKey.NameWithoutExt(), dep);
                        _CachePrefabDict.Add(request.ABPair.PathKey, null);
                        if (request.Callback != null)
                            request.Callback(null, request.CallbackState);
                        return true;
                    }
                }
            }

            if (request.ABLoadStatus == LoadStatus.LOADED && request.PrefabLoadStatus == LoadStatus.NONE)
            {
                request.PrefabLoadStatus = LoadStatus.LOADING;
                if (!_LoadingPrefab.ContainsKey(request.ABPair.PathKey))
                {
                    _Logger.Trace("Prefab[{0}] Async Loading from [{1}]", request.ABPair.PathKey.NameWithoutExt(), request.ABPair.ABName);
                    _LoadingPrefab.Add(request.ABPair.PathKey, _LoadedAbDict[request.ABPair.ABName].AB.LoadAssetAsync<GameObject>(request.ABPair.PathKey));
                }
            }
            return false;
        }

        private void RefreshLoadingStatus()
        {
            bool newABLoaded = false;
            foreach (var kv in _LoadingAB.Where(x => x.Value.isDone).ToList())
            {
                _LoadingAB.Remove(kv.Key);
                newABLoaded = true;
                ABHot newAb = null;
                if (kv.Value.assetBundle != null)
                {
                    _ABLogger.Trace("AB[{0}] Loaded.", kv.Key);
                    newAb = new ABHot(kv.Value.assetBundle);
                }
                else
                {
                    _ABLogger.Error("AB[{0}] Load failed.", kv.Key);
                }
                _LoadedAbDict.Add(kv.Key, newAb);
            }

            bool newPrefabLoaded = false;
            foreach (var kv in _LoadingPrefab.Where(x => x.Value.isDone).ToList())
            {
                _LoadingPrefab.Remove(kv.Key);
                newPrefabLoaded = true;
                GameObject go = null;
                if (kv.Value.asset != null && (go = kv.Value.asset as GameObject) != null)
                    _Logger.Trace("Prefab[{0}] Loaded.", kv.Key.NameWithoutExt());
                else
                    _Logger.Error("Prefab[{0}] Load Failed.", kv.Key.NameWithoutExt());

                //加载GameObject完成（成功或者失败）
                _CachePrefabDict.Add(kv.Key, go);
            }

            if (newABLoaded || newPrefabLoaded)
                _AsysncQueue.RemoveAll(x => ProcessRequest(x, newABLoaded));

            if (_AsysncQueue.Count == 0)
            {
                _RefreshBound = false;
                MyMono.Instance.UpdateEvent -= RefreshLoadingStatus;
                _Logger.Debug("▲ 异步资源加载任务全部完成，不再刷新。");
            }
        }
    }
}
