//GG 20180908 整理完毕
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using GLib;
using HSFrameWork.Common;
using ProtoBuf;
using System;
using System.Collections;

namespace HanSquirrel.ResourceManager.Impl
{
    public interface IAssetLoaderInner
    {
        T LoadAsset<T>(string assetPath) where T : UnityEngine.Object;
        T[] LoadAllAsset<T>(string path) where T : UnityEngine.Object;
        Sprite[] LoadAssetSubAstSprite(string path, string name);
        GameObject LoadPrefabCached(string path);
        bool TryGetPrefabFromCache(string path, out GameObject go);
        IEnumerator LoadPrefabCachedAsync(string pathKey, object state, Action<GameObject, object> callback);
        void LoadPrefabCachedAsyncQuick(string pathKey, object state, Action<GameObject, object> callback);
    }

    /// <summary>
    /// 和 AssetBundleEditorV2对应的 AB包加载模块，在黑盒测试的是可以随意new，正式运行时一般都是单例。
    /// </summary>
    public interface IAssetBundleManager : IAssetLoaderInner
    {
        bool IsCold();

        /// <summary>
        /// 所有ab包中是否存在这个路径的资源
        /// </summary>
        bool ContainsPathKey(string pathKey);

        /// <summary>
        /// 根据assetbundle路径寻找相关的AB包，减小计数。如果计数为0，则卸载该AB包。
        /// unloadAllLoadedObjects请参考 AssetBundle.Unload(bool unloadAllLoadedObjects)
        /// </summary>
        //void UnloadABByPathKey(string pathKey, bool unloadAllLoadedObjects = false);
        //GG 20180911 暂时禁用，因为上层有两级Cache，等有需要的时候再打开

        /// <summary>
        /// 根据AB包名字，确定其对应的打包文件的路径。
        /// 缺省为HotPatch.GetABFilePath。
        /// 可以被设置，主要用于黑盒测试。
        /// 在调用任何热接口之前设置，否则内部会抛异常。
        /// </summary>
        Func<string, string> GetABFilePathDelegate { get; set; }
    }

    public partial class AssetBundleManager : IAssetBundleManager, IDisposable
    {

        #region 公开接口实现

        public Func<string, string> GetABFilePathDelegate
        {
            get
            {
                return _GetABFilePathDelegate;
            }
            set
            {
                if (_Hot)
                    throw new Exception("AssetBundleManager已经加载完成，无法被配置。");
                _GetABFilePathDelegate = value;
            }
        }
        private Func<string, string> _GetABFilePathDelegate = HotPatch.GetABFilePath;
        protected static IHSLogger _Logger = HSLogManager.GetLogger("AM");
        protected static IHSLogger _ABLogger = HSLogManager.GetLogger("ABM");

        public bool IsCold()
        {
            return !_Hot;
        }

#if false
        /// <summary>
        /// 热更之后重新加载之前缓存的ab包
        /// </summary>
        private void ReLoadCachedAB()
        {
            if (_LoadedAbDict.Count > 0)
            {
                _ABLogger.Info("重载热更之前加载的ab包 [{0}]", _LoadedAbDict.Keys.ToList().Join(", "));
                foreach (var kv in _LoadedAbDict)
                {
                    if (kv.Value.AB != null)
                        kv.Value.AB.Unload(false);
                    kv.Value.AB = LoadABByName(kv.Key);
                }
            }
        }
#endif
        public bool ContainsPathKey(string pathKey)
        {
            LoadAssetBundleIndexIfNot();
            return _CrcResourceDict.ContainsKey(Crc32.GetCrc32(pathKey.RemoveDirEndTag()));
        }

        public ResourceABPair GetResourceABPair(string pathKey)
        {
            LoadAssetBundleIndexIfNot();
            pathKey = pathKey.RemoveDirEndTag();

            ResourceABPair item = null;
            uint crc = Crc32.GetCrc32(pathKey);
            //资源不在配表中证明不是在本工程中打包的
            if (!_CrcResourceDict.TryGetValue(crc, out item) || item == null)
            {
                Debug.LogErrorFormat("没有找到该资源: [{0}] [{1}]", crc, pathKey);
                return null;
            }
            return item;
        }

        public T LoadAsset<T>(string pathKey) where T : UnityEngine.Object
        {
            if (typeof(T) == typeof(GameObject))
            {
                throw new Exception("LoadAssetAtPath<T>不支持GameObject，请使用LoadPrefab*");
            }

            _Logger.Trace("{0}[{1}] Loading ", typeof(T).Name, pathKey);
            var ret = LoadAssetInner<T>(pathKey);
            if (ret != null)
                _Logger.Trace("{0}[{1}] Loaded.", typeof(T).Name, pathKey);
            else
                _Logger.Trace("{0}[{1}] Load Failed.", typeof(T).Name, pathKey);
            return ret;
        }

        private T LoadAssetInner<T>(string pathKey) where T : UnityEngine.Object
        {
            AssetBundle ab = LoadABCachedByPath(pathKey);
            return ab == null ? null : ab.LoadAsset<T>(pathKey);
        }

        public bool TryGetPrefabFromCache(string path, out GameObject go)
        {
            return _CachePrefabDict.TryGetValue(path, out go);
        }

        public GameObject LoadPrefabCached(string path)
        {
            GameObject go;
            if (_CachePrefabDict.TryGetValue(path, out go))
            {
                _Logger.Trace("Prefab[{0}] Cached ", path);
                return go;
            }

            if (_LoadingPrefab.ContainsKey(path))
                throw new Exception("应用程序编写错误：Prefab [{0}] 正在异步加载，无法再次同步加载。".f(path));

            _Logger.Trace("Prefab[{0}] Loading ", path);
            go = LoadAssetInner<GameObject>(path);
            _CachePrefabDict.Add(path, go);
            if (go != null)
                _Logger.Trace("Prefab[{0}] Loaded.", path);
            else
                _Logger.Trace("Prefab[{0}] Load Failed.", path);
            return go;
        }

        public T[] LoadAllAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            if (typeof(T) == typeof(GameObject))
            {
                throw new Exception("LoadAllAsset<T>不支持GameObject，请使用LoadPrefab*");
            }

            _Logger.Trace("{0}[{1}] Loading ", typeof(T).Name, assetPath);
            AssetBundle ab = LoadABCachedByPath(assetPath);
            T[] ret = ab == null ? null : ab.LoadAllAssets<T>();

            if (ret == null)
                _Logger.Trace("{0}[{1}] Load Failed.", typeof(T).Name, assetPath);
            else
                _Logger.Trace("{0}[{1}] Loaded. {2}个", typeof(T).Name, assetPath, ret.Length);

            return ret;
        }

        public Sprite[] LoadAssetSubAstSprite(string path, string name)
        {
            _Logger.Trace("Sprite[{0}][{1}] Loading.", path, name);
            AssetBundle ab = LoadABCachedByPath(path);
            Sprite[] ret = ab == null ? null : ab.LoadAssetWithSubAssets<Sprite>(name);

            if (ret == null)
                _Logger.Trace("Sprite[{0}][{1}] Load Failed.", path, name);
            else
                _Logger.Trace("Sprite[{0}][{1}] Loaded {2}个.", path, name, ret.Length);

            return ret;
        }

#if false
        public void UnloadABByPathKey(string pathKey, bool unloadAllLoadedObjects = false)
        {
            if (pathKey.NullOrWhiteSpace())
                return;

            SafeInit();

            ResourceABPair item = null;
            if (_CrcResourceDict.TryGetValue(Crc32.GetCrc32(pathKey), out item))
            {
                UnloadByABName(item.ABName, unloadAllLoadedObjects);
                item.DepdentABNames.ForEachG(x => UnloadByABName(x, unloadAllLoadedObjects));
            }
        }
#endif
        #endregion

        #region 私有及开发接口
        private bool _Hot = false;
        private void LoadAssetBundleIndexIfNot()
        {
            if (_Hot)
                return;
            _Hot = true;
            _ABLogger.Info("★★★ ResourceLoaded自动启动 ★★★");
            var ab = LoadABByName(HSUnityEnv.ResourceABIndexABName);
            if (ab == null)
                throw new Exception("无法加载资源索引AB包 [{0}]".f(HSUnityEnv.ResourceABIndexABName));

            using (DisposeHelper.Create(() => ab.Unload(true)))
            {
                var data = ab.LoadAsset<TextAsset>(HSUnityEnv.ResourceABIndexFile);
                if (data == null || data.bytes == null || data.bytes.Length == 0)
                    throw new Exception("无法从AB包 [{0}] 中加载资源索引文件".f(HSUnityEnv.ResourceABIndexABName, HSUnityEnv.ResourceABIndexFile));

                try
                {
                    _CrcResourceDict = DirectProtoBufTools.Deserialize<Dictionary<uint, ResourceABPair>>(data.bytes);
                    _ABLogger.Info("资源AB包索引文件加载成功。");
                }
                catch (Exception e)
                {
                    throw new Exception("从 [{0}].[{1}] 中加载字典失败.".f(HSUnityEnv.ResourceABIndexABName, HSUnityEnv.ResourceABIndexFile), e);
                }
            }
        }

        public Dictionary<string, ABHot> CloneCachedABDevOnly()
        {
            return new Dictionary<string, ABHot>(_LoadedAbDict);
        }

        /// <summary>
        /// 资源依赖关系配表,根据crc检索资源的信息
        /// </summary>
        private Dictionary<uint, ResourceABPair> _CrcResourceDict;
        /// <summary>
        /// 已经加载的Assetbundle列表，根据路ab包名称检索。
        /// 如果ABHot为null，则表示加载失败。null也缓存就不会持续不断去加载错误的AB包。
        /// </summary>
        protected Dictionary<string, ABHot> _LoadedAbDict = new Dictionary<string, ABHot>();
        /// <summary>
        /// 已经加载的GameObject列表，根据路路径包名称检索。
        /// 如果GameObject为null，则表示加载失败。null也缓存就不会持续不断去加载错误的Prefab。
        /// </summary>
        protected Dictionary<string, GameObject> _CachePrefabDict = new Dictionary<string, GameObject>();


#if false
        /// <summary>
        /// 将已经加载的AB包减小计数。如果计数为0，则卸载该AB包。
        /// </summary>
        private void UnloadByABName(string abName, bool unloadAllLoadedObjects = false)
        {
            ABHot abItem = null;
            if (_LoadedAbDict.TryGetValue(abName, out abItem))
            {
                if (abItem.RefCount <= 1)
                {
                    if (abItem.AB != null)
                        abItem.AB.Unload(unloadAllLoadedObjects);
                    _LoadedAbDict.Remove(abName);
                }
            }
        }
#endif

        /// <summary>
        /// 会加载所有该路径依赖的AB包，并返回这个路径所在的AB包。
        /// </summary>
        public AssetBundle LoadABCachedByPath(string pathKey)
        {
            var item = GetResourceABPair(pathKey);
            if (item == null)
            {
                _ABLogger.Error("AB[{0}] Cannot found.", pathKey);
                return null;
            }

            //加载依赖项
            item.DepdentABNames.ForEachG(dep =>
            {
                //Debug.LogFormat("GetAB [{0}] for [{1}]", dep, item.ABName);
                LoadABCachedByName(dep);
            });
            return LoadABCachedByName(item.ABName);
        }

        private AssetBundle LoadABCachedByName(string abName)
        {
            ABHot item;
            if (_LoadedAbDict.TryGetValue(abName, out item))
            {
                _ABLogger.Trace("AB[{0}] Cached.", abName);
                item.RefCount++;
            }
            else
            {
                item = new ABHot(LoadABByName(abName));
                _LoadedAbDict.Add(abName, item);
            }

            return item.AB;
        }

        private AssetBundle LoadABByName(string abName)
        {
            if (_LoadingAB.ContainsKey(abName))
            {
                throw new Exception("应用程序编写错误：[{0}] 正在异步被加载，无法同时同步加载".f(abName));
            }

            string strFullPath = GetABFilePathDelegate(abName);
            _ABLogger.Trace("AB[{0}] Loading [{1}] ", abName, strFullPath);
            AssetBundle assetBundle = AssetBundle.LoadFromFile(strFullPath);
            if (assetBundle == null)
                _ABLogger.Error("AB[{0}] Load Failed.", abName);
            else
                _ABLogger.Trace("AB[{0}] Loaded.", abName);
            return assetBundle;
        }
        #endregion
    }

    public class ABHot
    {
        public ABHot(AssetBundle ab)
        {
            AB = ab;
            RefCount = 1;
        }

        public AssetBundle AB;
        public int RefCount;
    }

    [ProtoContract]
    public class ResourceABPair
    {
        [ProtoMember(1)]
        public uint Crc { get; set; }

        [ProtoMember(2)]
        public string ABName { get; set; }//该资源所在AssetBundle

        [ProtoMember(3)]
        public List<string> DepdentABNames { get; set; }//该资源所依赖的AssetBundle

        [ProtoMember(4)]
        public string PathKey { get; set; }

        public IEnumerable<string> AllABNames
        {
            get
            {
                if (ABName.Visible())
                    yield return ABName;
                if (DepdentABNames != null)
                {
                    foreach (var dep in DepdentABNames)
                    {
                        if (dep.Visible())
                            yield return dep;
                    }
                }
            }
        }
    }
}
