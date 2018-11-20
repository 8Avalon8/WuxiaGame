//从原来的GameShared By Kevin 里面移动过来并整理。变为多项目共享。 GG20180912
//随时准备废弃。
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using GLib;
using System;
using HSFrameWork.Common;

namespace HanSquirrel.ResourceManager.ToBeRemoved
{
    [Obsolete]
    public interface IGameObjectAsyncLoader
    {
        void RemoveAllCachedResources();
        GameObject PreloadGameObjFromResource(string path);
        Sprite PreloadSpriteFromResource(string path);

        /// <summary>
        /// 卸载所有AB及其所有GO
        /// </summary>
        void UnloadAllAssetBundles(bool unloadAllLoadedObjects);
        /// <summary>
        /// 卸载AB
        /// </summary>
        void UnloadAssetBundle(string abName, bool unloadAllLoadedObjects);

        GameObject LoadGameObjectFromAB(string abName, string objName);
        Material LoadMaterialFromAB(string abName, string matName);
        Sprite LoadSpriteFromAB(string abName, string sptName);
        AudioClip LoadAudioClipFromAB(string abName, string audioName);

        void LoadGameObjectFromABAsync(string abName, string objName,
            object checkObj, Action<GameObject, object> _callback);
    }

    /// <summary>
    /// 对于不想派生的项目，可以直接使用这个类。
    /// </summary>
    [Obsolete]
    public class SmartABLoader : SmartABLoaderBase<SmartABLoader, IGameObjectAsyncLoader> { }

    /// <summary>
    /// GG20180912 临时如此，这样让现有部落弯刀的代码都不需要修改。
    /// </summary>
    [Obsolete]
    public class SmartABLoaderBase<T, TAs> : SingletonMB<T, TAs>, IGameObjectAsyncLoader where T : SmartABLoaderBase<T, TAs>, TAs
    {
        #region 基于Resource的GameObject和Sprite管理
        private Dictionary<string, GameObject> ResourceGODict = new Dictionary<string, GameObject>();
        private Dictionary<string, Sprite> ResourceSpriteDict = new Dictionary<string, Sprite>();

        public GameObject PreloadGameObjFromResource(string path)
        {
            return ResourceGODict.GetOrAdd(path, () => Resources.Load<GameObject>(path));
        }

        public Sprite PreloadSpriteFromResource(string path)
        {
            return ResourceSpriteDict.GetOrAdd(path, () => Resources.Load<Sprite>(path));
        }

        public void RemoveAllCachedResources()
        {
            ResourceGODict.Clear();
            ResourceSpriteDict.Clear();
        }
        #endregion

        #region 基本数据结构
        private class ABHot
        {
            public readonly AssetBundle AB;

            public readonly Dictionary<string, GameObject> GODict = new Dictionary<string, GameObject>();
            public readonly Dictionary<string, Sprite> SpriteDict = new Dictionary<string, Sprite>();
            public readonly Dictionary<string, AudioClip> AudioDict = new Dictionary<string, AudioClip>();
            public readonly Dictionary<string, Material> MatDict = new Dictionary<string, Material>();

            public ABHot(AssetBundle ab)
            {
                AB = ab;
                ResetAllMaterialShadersInBundle(ab);
            }

            private static void ResetAllMaterialShadersInBundle(AssetBundle ab)
            {
                Material[] sharedMatList = ab.LoadAllAssets<Material>();
                if (sharedMatList != null)
                {
                    foreach (Material tagMat in sharedMatList)
                    {
                        Shader tagShader = Shader.Find(tagMat.shader.name);
                        if (tagShader != null)
                        {
                            tagMat.shader = tagShader;
                        }
                    }
                }
            }

            public void Release(bool unloadAllLoadedObjects)
            {
                GODict.Clear();
                SpriteDict.Clear();
                AudioDict.Clear();
                MatDict.Clear();
                AB.Unload(unloadAllLoadedObjects);
            }

            public GameObject GetGameObjectCached(string name)
            {
                return GODict.GetOrAdd(name, () => AB.LoadAsset<GameObject>(name));
            }

            public Sprite GetSpriteCached(string spriteName)
            {
                return SpriteDict.GetOrAdd(spriteName, () => AB.LoadAsset<Sprite>(spriteName));
            }

            public AudioClip GetAudioClipCached(string audioName)
            {
                return AudioDict.GetOrAdd(audioName, () => AB.LoadAsset<AudioClip>(audioName));
            }

            public Material GetMaterialCached(string matName)
            {
                return MatDict.GetOrAdd(matName, () => AB.LoadAsset<Material>(matName));
            }
        }
        private Dictionary<string, ABHot> ABHotDict = new Dictionary<string, ABHot>();
        #endregion

        #region Unload
        [Obsolete]
        public void UnloadAssetBundle(string abName, bool unloadAllLoadedObjects)
        {
            ABHot abhot;
            if (ABHotDict.TryGetValue(abName, out abhot))
            {
                ABHotDict.Remove(abName);
                abhot.Release(unloadAllLoadedObjects);
            }
        }

        [Obsolete]
        public void UnloadAllAssetBundles(bool unloadAllLoadedObjects)
        {
            ABHotDict.Values.ForEachG(x => x.Release(true));
            ABHotDict.Clear();
        }
        #endregion

        #region 同步加载
        [Obsolete("请使用ResourceLoader.LoadPrefab*")]
        public GameObject LoadGameObjectFromAB(string abName, string objName)
        {
            return PreloadGameAssetBundle(abName).GetGameObjectCached(objName);
        }

        [Obsolete("请使用ResourceLoader.LoadAsset<Material>")]
        public Material LoadMaterialFromAB(string abName, string matName)
        {
            return PreloadGameAssetBundle(abName).GetMaterialCached(matName);
        }

        [Obsolete("请使用ResourceLoader.LoadAsset<Sprite>")]
        public Sprite LoadSpriteFromAB(string abName, string sptName)
        {
            return PreloadGameAssetBundle(abName).GetSpriteCached(sptName);
        }

        [Obsolete("请使用ResourceLoader.LoadAsset<AudioClip>")]
        public AudioClip LoadAudioClipFromAB(string abName, string audioName)
        {
            return PreloadGameAssetBundle(abName).GetAudioClipCached(audioName);
        }

        private ABHot PreloadGameAssetBundle(string abName)
        {
            ABHot abhot;
            if (!ABHotDict.TryGetValue(abName, out abhot))
            {
                abhot = new ABHot(ResourceLoader.LoadFromHotpatchingFile(abName));
                ABHotDict.Add(abName, abhot);
            }
            return abhot;
        }
        #endregion

        #region 异步加载
        private struct PreloadAssetBundleRequestInner
        {
            public string AbName;
            public string Path;
            public Action<ABHot> Callback;
        }

        private struct PreloadObjectFromAbRequestInner
        {
            public string AbName;
            public string ObjName;
            public object CallbackState;
            public Action<GameObject, object> Callback;
        }
        private Queue<PreloadObjectFromAbRequestInner> _AsysncLoadingObjQueue = new Queue<PreloadObjectFromAbRequestInner>();
        private bool isAsyncTheadBusy = false;

        /// <summary>
        /// 此版本的异步加载在同时被多次调用的时候可能会发生碰撞错误。
        /// </summary>
        [Obsolete("请使用ResourceLoader.LoadPrefabCachedAsync/LoadPrefabCachedAsyncQuick")]
        public void LoadGameObjectFromABAsync(string abName, string objName, object state, Action<GameObject, object> callback)
        {
            ABHot abHot; GameObject go;
            if (ABHotDict.TryGetValue(abName, out abHot) && abHot.GODict.TryGetValue(objName, out go))
            {
                HSUtils.Log("LoadGameObjectFromABAsync({0}, {1}), cached.", abName, objName);
                callback(go, state);
                return;
            }

            _AsysncLoadingObjQueue.Enqueue(new PreloadObjectFromAbRequestInner
            {
                AbName = abName, ObjName = objName,
                Callback = callback, CallbackState = state
            });

            ProcessAsyncLoadingQueue();
        }

        private void AsyncPreloadGameAssetBundle(string abName, Action<ABHot> callback)
        {
            ABHot abhot;
            if (ABHotDict.TryGetValue(abName, out abhot))
            {
                callback(ABHotDict[abName]);
            }
            else
            {
                StartCoroutine(LoadAssetBundleAsync(new PreloadAssetBundleRequestInner
                {
                    AbName = abName,
                    Path = HotPatch.GetABFilePath(abName),
                    Callback = callback
                }));
            }
        }

        private IEnumerator LoadAssetBundleAsync(PreloadAssetBundleRequestInner request)
        {
            HSUtils.Log("Async Loading [{0}] from [{1}]", request.AbName, request.Path);
            AssetBundleCreateRequest sysRequest = AssetBundle.LoadFromFileAsync(request.Path);
            yield return sysRequest;

            if (sysRequest != null && sysRequest.assetBundle != null)
            {
                HSUtils.Log("Load the AB {0} successed.", request.Path);
                ABHot newAb = new ABHot(sysRequest.assetBundle);
                ABHotDict.Add(request.AbName, newAb);

                if (request.Callback != null)
                {
                    request.Callback(newAb);
                }
            }
        }

        private void GetOrLoadGameObjectAsyncFromAB(ABHot abHot, PreloadObjectFromAbRequestInner request)
        {
            GameObject go;
            if (abHot.GODict.TryGetValue(request.ObjName, out go))
            {
                request.Callback(go, request.CallbackState);
                ProcessAsyncLoadingQueue();
            }
            else
            {
                isAsyncTheadBusy = true;
                StartCoroutine(LoadObjFromAssetBundleAsync(abHot, request));
            }
        }

        private void ProcessAsyncLoadingQueue()
        {
            if (isAsyncTheadBusy) return;
            if (_AsysncLoadingObjQueue.Count > 0)
            {
                var request = _AsysncLoadingObjQueue.Dequeue();

                ABHot abHot;
                if (ABHotDict.TryGetValue(request.AbName, out abHot))
                {
                    GetOrLoadGameObjectAsyncFromAB(abHot, request);
                }
                else
                {
                    isAsyncTheadBusy = true;
                    AsyncPreloadGameAssetBundle(request.AbName, delegate (ABHot tagAb)
                    {
                        isAsyncTheadBusy = false;
                        GetOrLoadGameObjectAsyncFromAB(tagAb, request);
                    });
                }
            }
        }

        private IEnumerator LoadObjFromAssetBundleAsync(ABHot abHot, PreloadObjectFromAbRequestInner request)
        {
            HSUtils.Log("Async Loading [{0}] from [{1}]", request.ObjName, abHot.AB.name);
            AssetBundleRequest abRequest = abHot.AB.LoadAssetAsync<GameObject>(request.ObjName);
            yield return abRequest;

            isAsyncTheadBusy = false;
            GameObject go = null;
            if (abRequest != null && abRequest.asset != null && (go = abRequest.asset as GameObject) != null)
            {
                HSUtils.Log("GameObject {0} Loaded.", request.ObjName);
                abHot.GODict.Add(request.ObjName, go);
                request.Callback(go, request.CallbackState);
            }
            else
            {
                HSUtils.LogWarning("GameObject {0} Loaded Failed.", request.ObjName);
            }

            ProcessAsyncLoadingQueue();
        }
        #endregion
    }
}
