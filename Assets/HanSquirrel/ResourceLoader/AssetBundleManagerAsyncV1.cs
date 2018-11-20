using System.Collections.Generic;
using UnityEngine;
using HSFrameWork.Common;
using System;
using System.Collections;

namespace HanSquirrel.ResourceManager.Impl
{
    /// <summary>
    /// 这个版本的实现仅仅为了参考。因为直接使用了多个yield的序列，会造成
    /// 执行过程的人为延缓，故此后来被放弃。
    /// </summary>
    [Obsolete]
    public class AssetBundleManagerArchiveV1 : AssetBundleManager
    {
        private class LoadGameObjectRequest
        {
            public ResourceABPair ABPair;
            public object CallbackState;
            public Action<GameObject, object> Callback;
        }
        private List<LoadGameObjectRequest> _AsysncQueue = new List<LoadGameObjectRequest>();
        private bool _IsAsyncTheadBusy = false;

        private class MyMono : SingletonMB<MyMono, MyMono>
        {
            public static int CallDepth = 0;
            private void Update()
            {
                CallDepth = 0;
            }
        }

        private class CallDepthTracer : IDisposable
        {
            public static CallDepthTracer New
            {
                get
                {
                    return new CallDepthTracer();
                }
            }
            CallDepthTracer()
            {
                MyMono.CallDepth++;
            }
            public void Dispose()
            {
                MyMono.CallDepth--;
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

        public new IEnumerator LoadPrefabCachedAsync(string assetPath, object callBackState, Action<GameObject, object> onLoaded)
        {
            MyYieldInstruction myYI = new MyYieldInstruction();
            using (CallDepthTracer.New)
            {
                LoadPrefabCachedAsyncInner(assetPath, null, (go, state) =>
                {
                    myYI.GO = go;
                    myYI.Ended = true;
                });
            }
            yield return myYI;

            using (CallDepthTracer.New)
                onLoaded(myYI.GO, callBackState);
        }

        private void LoadPrefabCachedAsyncInner(string pathKey, object state, Action<GameObject, object> callback)
        {
            using (CallDepthTracer.New)
            {
                GameObject go;
                if (_CachePrefabDict.TryGetValue(pathKey, out go))
                {
                    _Logger.Trace("Prefab [{0}] cached. +{1}", pathKey, MyMono.CallDepth);
                    callback(go, state);
                    return;
                }

                var item = GetResourceABPair(pathKey);
                if (item == null)
                {
                    _Logger.Trace("Prefab [{0}] load failed, NO SUCH PATH. +{1}", pathKey, MyMono.CallDepth);
                    callback(null, state);
                    return;
                }

                _AsysncQueue.Add(new LoadGameObjectRequest
                {
                    ABPair = item,
                    CallbackState = state,
                    Callback = callback
                });
                _Logger.Trace("Prefab [{0}] Queued. +{1}", pathKey, MyMono.CallDepth);

                if (!_IsAsyncTheadBusy)
                {
                    ProcessAsyncLoadingQueue();
                }
                else
                {
                    _Logger.Trace("正在进行异步调用，因此不处理队列。 +{1}", pathKey, MyMono.CallDepth);
                }
                //在异步调用没有结束前，处理队列没有意义，浪费CPU。
                //则等异步调用结束后自然会去处理队列
            }
        }

        /// <summary>
        /// 控制不让 ProcessAsyncLoadingQueue 重入。
        /// 因为在ProcessAsyncLoadingQueue中间可能会调用应用回调，后者可能会继续异步加载。
        /// </summary>
        private bool _ProcessAsyncLoadingQueueBusy = false;
        private void ProcessAsyncLoadingQueue()
        {
            using (CallDepthTracer.New)
            {
                if (_ProcessAsyncLoadingQueueBusy)
                    return;
                _ProcessAsyncLoadingQueueBusy = true;
                _Logger.Trace("▼ Processing Queue.... +{0}", MyMono.CallDepth);

                for (int i = 0; i < _AsysncQueue.Count;)
                {
                    var request = _AsysncQueue[i];

                    if (TryProcessNoLoad(request, _LoadedAbDict, _CachePrefabDict))
                    {   //不去加载就可以直接返回结果。
                        _AsysncQueue.RemoveAt(i);
                    }
                    else if (!_IsAsyncTheadBusy)
                    {
                        _AsysncQueue.RemoveAt(i);
                        _IsAsyncTheadBusy = true;
                        var cor = LoadGameObjectAsync(request, _LoadedAbDict, _CachePrefabDict, GetABFilePathDelegate, () =>
                          {
                              _IsAsyncTheadBusy = false;
                              ProcessAsyncLoadingQueue();
                          });

                        MyMono.Instance.StartCoroutine(cor);

                        /* 因为之前已经做过缓存判断了，故此执行堆栈是这样的：
                         * 1. LoadGameObjectAsync 立刻返回；不会重入到任何一个函数。
                         * 1. StartCoroutine 立刻返回。
                         * 
                         * 1. LoadGameObjectAsync 结束：
                         *    2. 调用这个匿名函数。
                         *    2. ProcessAsyncLoadingQueue
                         *        3. 给上层的回调不会重入到自己。
                         *        3. LoadGameObjectAsync 立刻返回
                         *        3. StartCoroutine 立刻返回
                         *        
                         * 故此不会出现堆栈溢出
                         * 
                         * 比较安全的办法是队列处理都让CoRoutine来做，然而这样会带来延时。
                         * 
                         * 目前这样的方式会让资源以最快的方式加载到：
                         * 在任何一个资源加载到了后，都迅速重新捋一遍所有的请求。因为有些是重复的。
                         * 在这个过程中发生的回调不会重入。
                        */
                    }
                    else
                    {
                        i++;
                    }
                }
                _ProcessAsyncLoadingQueueBusy = false;
                _Logger.Trace("▲ Processing Queue Ended. +{0}", MyMono.CallDepth);
            }
        }

        /// <summary>
        /// 以下三个函数用static的目的在于明确防止互相调用，以便能够从逻辑上分析堆栈溢出的可能性。
        /// </summary>
        private static bool TryProcessNoLoad(LoadGameObjectRequest request, Dictionary<string, ABHot> loadedAbDict, Dictionary<string, GameObject> cachePrefabDict)
        {
            using (CallDepthTracer.New)
            {

                GameObject go;
                if (cachePrefabDict.TryGetValue(request.ABPair.PathKey, out go))
                {
                    //该Prefab已经在内存了，直接返回。
                    _Logger.Trace("AB [{0}] cached. +{1}", request.ABPair.PathKey, MyMono.CallDepth);
                    request.Callback(go, request.CallbackState);
                    return true;
                }

                foreach (var dep in request.ABPair.AllABNames)
                {
                    ABHot abHot;
                    if (loadedAbDict.TryGetValue(dep, out abHot) && abHot == null)
                    {
                        _Logger.Trace("Prefab [{0}] 所依赖的 AB [{1}] 曾经加载失败 +{2}", request.ABPair.PathKey, dep, MyMono.CallDepth);
                        cachePrefabDict.Add(request.ABPair.PathKey, null);
                        request.Callback(null, request.CallbackState);
                        return true;
                    }
                }
                return false;
            }
        }

        private static IEnumerator LoadGameObjectAsync(LoadGameObjectRequest request, Dictionary<string, ABHot> loadedABDict, Dictionary<string, GameObject> cachePrefabDict, Func<string, string> abNameFileMapper, Action onExit)
        {
            MyMono.CallDepth++;
            foreach (var dep in request.ABPair.AllABNames)
            {
                ABHot abHot;
                if (!loadedABDict.TryGetValue(dep, out abHot))
                {
                    string strFullPath = abNameFileMapper(dep);
                    _Logger.Trace("AB [{0}] Async Loading [{1}] +{2}", dep, strFullPath, MyMono.CallDepth);
                    AssetBundleCreateRequest sysRequest = AssetBundle.LoadFromFileAsync(strFullPath);
                    MyMono.CallDepth--;
                    yield return sysRequest;
                    MyMono.CallDepth = 1;
                    _Logger.Trace("AB [{0}] ★★★ 加载异步返回了。", dep, MyMono.CallDepth);

                    if (!AfterAssetBundleLoaded(sysRequest, dep, loadedABDict))
                    {
                        //加载相关AB包失败，返回null
                        _Logger.Error("AB [{0}] 加载失败 +{1}", dep, MyMono.CallDepth);
                        cachePrefabDict.Add(request.ABPair.PathKey, null);
                        request.Callback(null, request.CallbackState);
                        onExit();
                        MyMono.CallDepth--;
                        yield break;
                    }
                }
                else if (abHot == null)
                {
                    _Logger.Trace("Prefab [{0}] 所依赖的 AB [{1}] 曾经无法加载 +{2}", request.ABPair.PathKey, dep, MyMono.CallDepth);
                    cachePrefabDict.Add(request.ABPair.PathKey, null);
                    request.Callback(null, request.CallbackState);
                    onExit();
                    MyMono.CallDepth--;
                    yield break;
                }
            }

            _Logger.Trace("Prefab [{0}] Async Loading from [{1}] +{2}", request.ABPair.PathKey, request.ABPair.ABName, MyMono.CallDepth);
            AssetBundleRequest abRequest = loadedABDict[request.ABPair.ABName].AB.LoadAssetAsync<GameObject>(request.ABPair.PathKey);
            MyMono.CallDepth--;
            yield return abRequest;
            MyMono.CallDepth = 1;
            _Logger.Trace("Prefab [{0}] 加载异步返回了。", request.ABPair.PathKey, MyMono.CallDepth);

            GameObject go = null;
            if (abRequest != null && abRequest.asset != null && (go = abRequest.asset as GameObject) != null)
                _Logger.Trace("Prefab [{0}] Loaded. +{1}", request.ABPair.PathKey, MyMono.CallDepth);
            else
                _Logger.Error("Prefab [{0}] Load Failed. +{1}", request.ABPair.PathKey, MyMono.CallDepth);

            //加载GameObject完成（成功或者失败）
            cachePrefabDict.Add(request.ABPair.PathKey, go);
            request.Callback(go, request.CallbackState);
            onExit();
            MyMono.CallDepth--;
        }

        private static bool AfterAssetBundleLoaded(AssetBundleCreateRequest sysRequest, string abName, Dictionary<string, ABHot> loadedABDict)
        {
            using (CallDepthTracer.New)
            {
                ABHot newAb = null;
                if (sysRequest != null && sysRequest.assetBundle != null)
                {
                    _Logger.Trace("AB [{0}] Loaded. +{1}", abName, MyMono.CallDepth);
                    newAb = new ABHot(sysRequest.assetBundle);
                }
                else
                {
                    _Logger.Error("AB[{0}] Load failed. +{1}", abName, MyMono.CallDepth);
                }
                loadedABDict.Add(abName, newAb);
                return newAb != null;
            }
        }
    }
}

