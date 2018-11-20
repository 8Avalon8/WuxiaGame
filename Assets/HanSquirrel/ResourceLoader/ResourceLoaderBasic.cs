using GLib;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using HSFrameWork.Common;
using HanSquirrel.ResourceManager.Impl;

namespace HanSquirrel.ResourceManager
{
    /// <summary>
    /// Assert加载的唯一工具类。
    /// </summary>
    public partial class ResourceLoader
    {
        #region 公开接口实现
        /// <summary>
        /// 重新加载 路径-AB包 索引文件
        /// </summary>
        public static bool IsCold()
        {
            return _AssetBundleManager.IsCold();
        }

        /// <summary>
        /// 使用 StartCoroutine来调用，如果该资源已经在缓存，则回调函数会立刻被调用。
        /// 否则会在资源加载完成后从 Coroutine 调用。
        /// 优点：如果在回调返回前 MonoBehaviour被Disable，则回调不会被调用了。
        /// 缺点：资源真正被加载到和回调被调用总是延迟一个Frame（这个是所有Coroutine的特点）。
        /// </summary>
        public static IEnumerator LoadPrefabCachedAsync(string assetPath, object state, Action<GameObject, object> onLoaded)
        {
            return _AssetLoader.LoadPrefabCachedAsync(assetPath, state, onLoaded);
        }

        /// <summary>
        /// 如果该资源已经在缓存，则回调函数会立刻被调用。
        /// 否则在资源获取到之后会立刻被调用。
        /// </summary>
        public static void LoadPrefabCachedAsyncQuick(string assetPath, object state, Action<GameObject, object> onLoaded)
        {
            _AssetLoader.LoadPrefabCachedAsyncQuick(assetPath, state, onLoaded);
        }

        /// <summary>
        /// 加载prefab没有实例化，外面调用请谨慎使用
        /// </summary>
        /// <param name="path">全路径</param>
        public static GameObject LoadPrefabCached(string path)
        {
            return _AssetLoader.LoadPrefabCached(path);
        }

        /// <summary>
        /// 同步加载单个不需要实例化的文件，必须带文件后缀名，.mp3  .png 等等
        /// path绝对和Unity工程下的该资源路径完全大小写一致。path以 "Assets/"开头。
        /// </summary>
        public static T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            if (typeof(T) == typeof(GameObject))
            {
                throw new Exception("LoadAssetAtPath<T>不支持GameObject，请使用LoadPrefab*");
            }

            return _AssetLoader.LoadAsset<T>(path);
        }

        /// <summary>
        /// 加载一个文件夹下面所有特定资源。
        /// 该文件夹必须是AB包定义的根目录。
        /// path绝对和Unity工程下的该资源路径完全大小写一致。
        /// path以 "Assets/"开头，是否以"/"结束都可以。
        /// 是否是图片，会自动判断在编辑器下是否是图集，然后再展开
        /// </summary>
        /// <returns></returns>
        public static T[] LoadAllAsset<T>(string path) where T : UnityEngine.Object
        {
            if (typeof(T) == typeof(GameObject))
            {
                throw new Exception("LoadAssetAtPath<T>不支持GameObject，请使用LoadPrefab*");
            }

            return _AssetLoader.LoadAllAsset<T>(path);
        }

        /// <summary>
        /// 加载图集里面所有图
        /// 该文件夹必须是AB包定义的根目录。
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <param name="name">对应的文件名字</param>
        /// <returns></returns>
        public static Sprite[] LoadAssetSubAstSprite(string path, string name)
        {
            return _AssetLoader.LoadAssetSubAstSprite(path, name);
        }

        /// <summary>
        /// LoadAsset<TextAsset>(path).bytes。
        /// 如果资源不存在，则返回null
        /// </summary>
        public static byte[] LoadBinary(string path)
        {
            var ta = _AssetLoader.LoadAsset<TextAsset>(path);
            return ta == null ? null : ta.bytes;
        }
        #endregion

        #region 私有及开发接口
        private static IHSLogger _ABLogger = HSLogManager.GetLogger("ABM");
        private static IHSLogger _Logger = HSLogManager.GetLogger("AM");
        private static IAssetLoaderInner _AssetLoader;
        static ResourceLoader()
        {
            _AssetLoader = _AssetBundleManager;
#if UNITY_EDITOR
            _AssetLoader = AssetLoaderFileSYS.Instance;
            _ABLogger.Info("资源缺省直接从磁盘文件中读取。");
        }

        /// <summary>
        /// 在编辑模式下，强制从AB包加载资源。
        /// </summary>
        public static bool LoadFromABAlways
        {
            get
            {
                return _AssetLoader is AssetLoaderFileSYS;
            }
            set
            {
                _AssetLoader = value ? _AssetBundleManager : AssetLoaderFileSYS.Instance;
                if (value)
                    _ABLogger.Info("资源修改为从StreamingAsset读取。");
                else
                    _ABLogger.Info("资源修改为从磁盘文件中读取。");
            }
        }
#else
        }
#endif

        /// <summary>
        /// 仅仅内部开发使用。
        /// </summary>
        public static void SetAssetBundleManagerDevOnly(IAssetBundleManager abm)
        {
            _AssetBundleManager = abm;
        }

        private static IAssetBundleManager _AssetBundleManager = new AssetBundleManager();
        #endregion

        #region 过期接口

        /// <summary>
        /// 是否存在该资源。已经废弃。
        /// </summary>
        [Obsolete("CG TODO:这个方法需要干掉")]
        public static bool ExistPath(string path)
        {
            return _AssetBundleManager.ContainsPathKey(path);
        }

        /// <summary>
        /// 已经废弃。
        /// </summary>
        [Obsolete("请不要直接使用AssetBundle")]
        static public AssetBundle LoadFromHotpatchingFile(string file)
        {
            _ABLogger.Info("AB [{0}] 被废止Loading...", file);
            var ret = AssetBundle.LoadFromFile(HotPatch.GetABFilePath(file.ToLower()));
            if (ret != null)
                _ABLogger.Info("AB [{0}] 被废止 Loaded.", file);
            else
                _ABLogger.Error("AB [{0}] 被废止 Load Failed. ", file);
            return ret;
        }

        /// <summary>
        /// 已经废弃。
        /// </summary>
        [Obsolete("请使用LoadAsset<TextAsset>")]
        static public string LoadAssetbundleText(string file, string path = "")
        {
            if (string.IsNullOrEmpty(path))
            {
                path = file; //默认同名
            }

            var ab = AssetBundle.LoadFromFile(HotPatch.GetABFilePath(file));
            var asset = ab.LoadAsset<TextAsset>(path);
            string text = asset.text;
            return text;
        }

        /// <summary>
        /// 已经废弃。从resource和assetbundle取混合集
        /// 注：此函数一般用于初始化，并缓存起来，如果ASSETBUNDLE非常大，则此函数效率会非常低
        /// 同名资源assetbundle将覆盖resource
        /// </summary>
        [Obsolete("请不要使用这个函数")]
        static public IEnumerable<T> LoadAllResourceCombineAssetBundle<T>(string resourceName, string assetbundleName) where T : UnityEngine.Object
        {
            Dictionary<string, bool> visited = new Dictionary<string, bool>();
            var ab = AssetBundle.LoadFromFile(HotPatch.GetABFilePath(assetbundleName));
            var list2 = ab.LoadAllAssets<T>();
            if (list2 != null)
            {
                foreach (var iter in list2)
                {
                    if (!visited.ContainsKey(iter.name))
                    {
                        visited.Add(iter.name, true);
                        yield return (T)iter;
                    }
                }
            }

            var list1 = Resources.LoadAll<T>(resourceName);
            if (list1 != null)
            {
                foreach (var iter in list1)
                {
                    if (!visited.ContainsKey(iter.name))
                    {
                        visited.Add(iter.name, true);
                        yield return (T)iter;
                    }
                }
            }
        }
        #endregion
    }
}
