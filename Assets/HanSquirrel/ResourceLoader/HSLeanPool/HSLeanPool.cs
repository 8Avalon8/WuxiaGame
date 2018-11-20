using UnityEngine;
using System.Collections.Generic;
using GLib;
using System.Linq;
using HSFrameWork.Common;
using System.Text;
using System;
using System.Collections;

namespace HanSquirrel.ResourceManager
{
    /// <summary>
    /// 用于配置 ResourceLoader
    /// </summary>
    public class HSLeanPoolConfig
    {
        /// <summary> Prefab的全路径 </summary>
        public string Path;

        /// <summary> 最大的缓存实例个数 </summary>
        public int Cap;
        /// <summary> 预加载的缓存实例个数 </summary>
        public int Preload;

        /// <summary> 是否接收OnSpawn/OnDespawn </summary>
        public NotificationTypeLP NotifyType;

        public HSLeanPoolConfig(string path, int cap, int preload) : this(path, cap, preload, NotificationTypeLP.None) { }

        public HSLeanPoolConfig(string path, int cap, int preload, NotificationTypeLP type)
        {
            Path = path;
            Cap = cap;
            Preload = preload;
            NotifyType = type;
        }
    }

    /// <summary>
    /// 该类GameObject在Spawn和Despawn的时候是否调用 OnSpawn和OnDespawn
    /// 该类必须同时实现 ISpawnDespawnReceivable 才有意义。
    /// </summary>
    public enum NotificationTypeLP
    {
        None,
        SendMessage,
        //BroadcastMessage
    }

    /// <summary>
    /// 如果某个Component需要在Spawn和Despawn的时候收到通知，则需要实现该接口。
    /// 该Component对应的Prefab必须同时配置 NotificationTypeLP.SendMessage
    /// </summary>
    public interface ISpawnDespawnReceivable
    {
        void OnSpawn();
        void OnDespawn();
    }

    public partial class ResourceLoader
    {
        #region 批量操作
        /// <summary> 设置会自动Pool的那些Prefab。仅仅设置，不预加载。</summary>
        public static void ResetPoolConfig(IEnumerable<HSLeanPoolConfig> configs)
        {
            _PathConfigDict = configs.ToDictionary(x => x.Path, x => x);
            _NameConfigDict = configs.ToDictionary(x => x.Path.NameWithoutExt().ToLower(), x => x);
            _PrefabPoolDict.Values.ForEachG(x => x.ResetPropertiesFromConfig());
        }

        /// <summary> 强制创建对应的Pool，无论是否配置过自动Pool。</summary>
        public static void PreLoad(GameObject prefab, int count, string prefabPath = null/*不知道路径*/)
        {
            Mini.ThrowNullIf(prefab, "Attempting to spawn a null prefab");

            var pool = GetOrAdd(prefab);
            pool.Preload = Math.Max(pool.Preload, count);
            pool.PrefabPath = prefabPath;
            pool.DoPreload();
        }

        /// <summary> 强制创建对应的Pool，无论是否配置过自动Pool。 </summary>
        public static void PreLoad(string path, int count)
        {
            GameObject prefab = LoadPrefabCached(path);
            if (prefab == null)
                return;
            PreLoad(prefab, count, path);
        }

        /// <summary>
        /// 将缺省配置和附加配置中的Prefab全部加载，创建好Pool，并且Preload完成。
        /// </summary>
        public static void PreLoad(IEnumerable<HSLeanPoolConfig> additionalPoolConfigs = null)
        {
            using (HSUtils.ExeTimerEnd("Preload"))
                (additionalPoolConfigs == null ? _PathConfigDict.Values :
                   _PathConfigDict.Values.Union(additionalPoolConfigs)).ForEachG(x =>
                {
                    GameObject prefab = LoadPrefabCached(x.Path);
                    if (prefab != null)
                        GetOrAdd(prefab).ResetPropertiesFromConfig(x).DoPreload(false);
                });
        }

        /// <summary>
        /// 异步加载的时候，本Frame执行时间超过这个时间后就会暂停执行，到下一帧继续。缺省为50.
        /// </summary>
        public const int PreloadMaxTimePerFrame = 50;

        /// <summary>
        /// 将缺省配置和附加配置中的Prefab全部加载，创建好Pool，并且Preload完成。
        /// 缺省配置并不会被覆盖。此版本是伪异步版本，性能比较稳定。
        /// </summary>
        public static IEnumerator PreLoadAsync(Action<bool, float> onProgress = null, IEnumerable<HSLeanPoolConfig> additionalPoolConfigs = null, string title = "NA")
        {
            HSUtilsEx.TriggerNewFrame();
            using (HSUtils.ExeTimerEnd("PreloadAsync"))
            {
                var preloadTasks = (additionalPoolConfigs == null ? _PathConfigDict.Values :
                        _PathConfigDict.Values.Union(additionalPoolConfigs)).ToList();

                for (int i = 0; i < preloadTasks.Count; i++)
                {
                    var config = preloadTasks[i];

                    GameObject prefab = LoadPrefabCached(config.Path);
                    if (HSUtilsEx.ElaspsedThisFrame >= PreloadMaxTimePerFrame)
                    {
                        if (onProgress != null)
                            onProgress(false, (i + 0.5f) / preloadTasks.Count);
                        yield return null;
                        HSUtilsEx.TriggerNewFrame();
                    }

                    if (prefab != null)
                    {
                        var pool = GetOrAdd(prefab).ResetPropertiesFromConfig(config);
                        pool.DisablePreloadInUpdate = true;
                        while (pool.TryPreloadOne())
                        {
                            if (HSUtilsEx.ElaspsedThisFrame >= PreloadMaxTimePerFrame)
                            {
                                if (onProgress != null)
                                    onProgress(false, (i + 0.5f + 0.5f * (1.0f * pool.Total / pool.Preload)) / preloadTasks.Count);
                                yield return null;
                                HSUtilsEx.TriggerNewFrame();
                            }
                        }
                        if (onProgress != null)
                            onProgress(false, (i + 1.0f) / preloadTasks.Count);
                        pool.DisablePreloadInUpdate = false;
                    }
                }
                onProgress(true, 1.0f);
            }
        }

        /// <summary> 清理不在配置里面的那些Pool，并不影响已经分配出去的 </summary>
        public static void ClearAllTempPools()
        {
            var trashPools = _PrefabPoolDict.Values
                .Where(x => !_NameConfigDict.ContainsKey(x.Prefab.name.ToLower()))
                .ToHashSetG();
            trashPools.ForEachG(x =>
            {
                _PrefabPoolDict.Remove(x.Prefab);
                _PooledPrefabNameSet.Remove(x.Prefab.name);

                GameObject.Destroy(x.gameObject);
            });

            var trashGameObjects = _GOPoolDict
                    .Where(kv => trashPools.Contains(kv.Value))
                    .Select(kv => kv.Key)
                    .ToList();
            trashGameObjects.ForEach(x => _GOPoolDict.Remove(x));

            _PoolLogger.Info("Pool 删除了[{0}]个，剩余[{1}]个。", trashPools.Count, _PrefabPoolDict.Count);
            _PoolLogger.Info("待回收GO删除了[{0}]个，剩余[{1}]个。", trashGameObjects.Count, _GOPoolDict.Count);
        }
        #endregion

        #region 核心Spawn/DeSpawn
        /// <summary>
        /// 会强制创建对应的Pool，无论是否配置过。
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, string prefabPath)
        {
            Mini.ThrowNullIf(prefab, "Attempting to spawn a null prefab");

            var pool = GetOrAdd(prefab);
            pool.PrefabPath = prefabPath;
            var clone = pool.Spawn(position, rotation, parent);
            _GOPoolDict.Add(clone, pool);
            return clone;
        }

        /// <summary> 如果clone不是本Pool分配的，则会直接被Destroy；否则就会回归到池子中。 </summary>
        public static void DespawnOrDestory(GameObject clone, float delay = 0.0f)
        {
            if (clone == null)
                return;

            LeanPool pool;
            if (_GOPoolDict.TryGetValue(clone, out pool))
            {
                _GOPoolDict.Remove(clone);
                pool.SafeDespawn(clone, delay);
            }
            else
            {
                GameObject.Destroy(clone);
            }
        }
        #endregion

        #region 公开只读接口
        /// <summary>
        /// 只读。对象池个数。（每个对象池对应一个Prefab）
        /// </summary>
        public static int PoolCount { get { return _PrefabPoolDict.Count; } }

        /// <summary>
        /// 只读。当前外部正在使用的GameObject的个数
        /// </summary>
        public static int AllocatedCount { get { return _GOPoolDict.Count; } }

        /// <summary>
        /// 是否通过Config或者Preload或者Spawn已经创建了对应的Pool。
        /// </summary>
        public static bool CanBePooled(string prefabPath)
        {
            var name = prefabPath.NameWithoutExt().ToLower();
            return _PooledPrefabNameSet.Contains(name) || _PathConfigDict.ContainsKey(prefabPath);
        }
        #endregion

        #region 公开测试专用接口
        /// <summary>
        /// 开发测试专用。
        /// </summary>
        public static void AssertEmpty()
        {
            AssertPoolCount(0);
            AssertAllocatedCount(0);
        }

        /// <summary>
        /// 开发测试专用。
        /// </summary>
        public static void AssertPoolCount(int c)
        {
            Mini.ThrowIfFalse(c == PoolCount, "Pool的个数 [{0}]!=[{1}]".f(PoolCount, c));
        }

        /// <summary>
        /// 开发测试专用。
        /// </summary>
        public static void AssertAllocatedCount(int c)
        {
            Mini.ThrowIfFalse(c == AllocatedCount, "分配出去的个数 [{0}]!=[{1}]".f(AllocatedCount, c));
        }

        /// <summary>
        /// 开发测试专用。
        /// </summary>
        public static void AssertNotInPool(GameObject prefab)
        {
            if (_PrefabPoolDict.ContainsKey(prefab))
                throw new Exception("LeanPool里面不应该有Prefab[{0}]".f(prefab.name));
        }

        /// <summary>
        /// 开发测试专用。
        /// </summary>
        public static void AssertInPool(GameObject prefab, int total, int available, int cloned)
        {
            LeanPool pool;
            Mini.ThrowIfFalse(_PrefabPoolDict.TryGetValue(prefab, out pool),
                "LeanPool里面应该有Prefab[{0}]".f(prefab.name));
            Mini.ThrowIfFalse(pool.Total == total, "Prefab[{0}] total[{1}]!=[{2}]".f(prefab.name, pool.Total, total));
            Mini.ThrowIfFalse(pool.Cached == available, "Prefab[{0}] available[{1}]!=[{2}]".f(prefab.name, pool.Cached, available));
        }

        /// <summary>
        /// 开发测试专用。
        /// </summary>
        public static void AssertInAllocated(GameObject go, GameObject prefab)
        {
            LeanPool pool;
            Mini.ThrowIfFalse(_GOPoolDict.TryGetValue(go, out pool), "LeanPool里面应该有GO[{0}]".f(go.name));
            Mini.ThrowIfFalse(pool.Prefab == prefab, "LeanPool里面GO[{0}].prefab[{1}]!={2}".f(go.name, pool.Prefab.name, prefab.name));
        }

        /// <summary>
        /// 取得Pool的基本状态
        /// </summary>
        public static string DumpCurrentPoolStatus()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Pool个数 [{0}]\n", _PrefabPoolDict.Count);
            _PrefabPoolDict.Values.ForEachG(x => x.Dump(sb));
            sb.AppendFormat("当前尚未归还数目： [{0}]", _GOPoolDict.Count);
            return sb.ToString();
        }
        #endregion

        #region 内外杂项
        private static IHSLogger _PoolLogger = HSLogManager.GetLogger("LeanPool");

        private static LeanPool GetOrAdd(GameObject prefab)
        {
            return _PrefabPoolDict.GetOrAdd(prefab, () =>
            {
                _PoolLogger.Trace("Add Pool [{0}]", prefab.name);
                _PooledPrefabNameSet.Add(prefab.name.ToLower());
                var pool = new GameObject(prefab.name + " Pool").AddComponent<LeanPool>();
                pool.Prefab = prefab;
                pool.ResetPropertiesFromConfig();
                pool.transform.SetParent(RootMB.Instance.transform, false);
                return pool;
            });
        }

        private class DelayedDestruction
        {
            public GameObject Clone;
            public float Life;
        }

        private static Dictionary<string, HSLeanPoolConfig> _PathConfigDict = new Dictionary<string, HSLeanPoolConfig>();
        private static Dictionary<string, HSLeanPoolConfig> _NameConfigDict = new Dictionary<string, HSLeanPoolConfig>();

        private class RootMB : SingletonMB<RootMB, RootMB>
        {
            private void Update()
            {
                HSUtilsEx.TriggerNewFrame();
            }

            private new void OnDestroy()
            {
                //保险起见而已。
                _PooledPrefabNameSet.Clear();
                _PrefabPoolDict.Clear();
                _GOPoolDict.Clear();

                base.OnDestroy();
            }
        }

        private static HashSet<string> _PooledPrefabNameSet = new HashSet<string>();
        // 所有当前的Pool
        private static Dictionary<GameObject, LeanPool> _PrefabPoolDict = new Dictionary<GameObject, LeanPool>();

        // 分配出去的GameObject所对应的Pool
        private static Dictionary<GameObject, LeanPool> _GOPoolDict = new Dictionary<GameObject, LeanPool>();
        #endregion

        private class LeanPool : MonoBehaviour
        {
            #region 公开属性
            public GameObject Prefab;

            private string _PrefabPath;
            public string PrefabPath
            {
                get { return _PrefabPath; }
                set
                {
                    if (value == null)
                        return;
                    if (_PrefabPath == null)
                    {
                        if (value.NameWithoutExt().ToLower() != Prefab.name.ToLower())
                            throw new Exception("Prefab[{0}]的文件名和名字[{1}]不同，请修改。".f(value, Prefab.name));
                        _PrefabPath = value;
                    }
                    else if (_PrefabPath != value)
                    {
                        throw new Exception("应用编程错误：Prefab设置两个不同的路径 [{0}] [{1}]".f(_PrefabPath, value));
                    }
                }
            }
            public int Preload;
            public int Capacity;
            public NotificationTypeLP Notification = NotificationTypeLP.None;

            public bool DisablePreloadInUpdate = false;

            public bool PreloadCompleted { get { return Total >= Preload; } }

            /// <summary>
            /// 总共用过的次数
            /// </summary>
            public int Spawned { get; private set; }

            /// <summary>
            /// 总共曾经创建过多少个
            /// </summary>
            public int Cloned { get; private set; }

            /// <summary>
            /// 被销毁次数
            /// </summary>
            public int Destoried { get; private set; }

            /// <summary>
            /// 最大负载
            /// </summary>
            public int MaxAllocated { get; private set; }

            /// <summary>
            /// 目前还有多少活动的（包括外部使用的和Pool里面的）
            /// </summary>
            public int Total { get { return Cloned - Destoried; } }

            /// <summary>
            /// 目前池子里面的
            /// </summary>
            public int Cached { get { return _Cache.Count; } }

            /// <summary>
            /// 外部使用的
            /// </summary>
            public int Allocated { get { return Total - Cached; } }

            #endregion

            #region 公开函数
            public LeanPool ResetPropertiesFromConfig(HSLeanPoolConfig config)
            {
                Capacity = config.Cap;
                Preload = config.Preload;
                Notification = config.NotifyType;
                PrefabPath = config.Path;

                while (Capacity > 0 && _Cache.Count > 0 && Total > Capacity)
                {
                    Destoried++;
                    GameObject.Destroy(_Cache.Pop());
                    //外部还有在用的那些在归还的时候会被销毁。
                }
                return this;
            }

            public void ResetPropertiesFromConfig()
            {
                HSLeanPoolConfig config;
                if ((PrefabPath != null && _PathConfigDict.TryGetValue(PrefabPath, out config)) ||
                    _NameConfigDict.TryGetValue(Prefab.name.ToLower(), out config))
                {
                    ResetPropertiesFromConfig(config);
                }
            }

            public void Dump(StringBuilder sb)
            {
                sb.AppendFormat("[{0}]: 可用[{1}] 被用[{2}] 最大被用[{3}] 被用累计[{4}] 销毁累计[{5}]\r\n",
                    Prefab.name, Cached, Allocated, MaxAllocated, Spawned, Destoried);
            }

            public bool TryPreloadOne()
            {
                if (Total < Preload)
                {
                    DoPreloadOne();
                }
                return Total < Preload;
            }

            private void DoPreloadOne()
            {
                var clone = DoClone(Vector3.zero, Quaternion.identity, null);
                clone.SetActive(false);
                clone.transform.SetParent(transform, false);
                _Cache.Push(clone);
            }

            // Makes sure the right amount of prefabs have been preloaded
            public void DoPreload(bool async = false)
            {
                Mini.ThrowNullIf(Prefab, "George编程错误：Prefab没有初始化就开始Spawn。");
                if (Total < Preload)
                {
                    bool createOne = false;
                    for (var i = Total; i < Preload; i++)
                    {
                        if (async && HSUtilsEx.ElaspsedThisFrame >= PreloadMaxTimePerFrame)
                            break; //Clone很费时间也
                        DoPreloadOne();
                        createOne = true;
                    }
                    if (createOne)
                        _PoolLogger.Trace("Pool[{0}] 总创建[{1}]个，可用[{2}]个。", Prefab.name, Total, Cached);
                }
            }

            // This will return a clone from the cache, or create a new instance
            public GameObject Spawn(Vector3 position, Quaternion rotation, Transform parent)
            {
                Mini.ThrowNullIf(Prefab, "George编程错误：Prefab没有初始化就开始Spawn。");
                GameObject clone = null;
                if (_Cache.Count > 0)
                {
                    clone = _Cache.Pop();
                    Mini.ThrowNullIf(clone, "George编程错误：LeanPool.Cache里面有NULL。");
                    _PoolLogger.Trace("Pool[{0}] 提供一个。", Prefab.name);

                    clone.transform.localPosition = position;
                    clone.transform.localRotation = rotation;
                    clone.transform.SetParent(parent, false);
                    clone.SetActive(true);
                }
                else
                {
                    clone = DoClone(position, rotation, parent);
                }

                Spawned++;
                if (MaxAllocated < Allocated) MaxAllocated = Allocated;
                SafeSendNotificationIfConfiged(clone, "OnSpawn");
                return clone;
            }

            public void SafeDespawn(GameObject clone, float delay = 0.0f)
            {
                if (clone == null)
                    return;

                if (delay > 0.0f)
                {
                    _DelayedDespawnDict.GetOrAdd(clone, () =>
                    {
                        _PoolLogger.Trace("Pool[{0}] Delay Despawn 1。", Prefab.name);
                        return new FloatBox { Value = delay };
                    });
                }
                else
                {
                    SafeSendNotificationIfConfiged(clone, "OnDespawn");
                    clone.SetActive(false);

                    bool returned = false;
                    //add BY CG:2017/1/21 有时候被清除掉LEANPOOL会导致此处调用错误，增加try catch保证鲁棒性
                    try
                    {
                        if (this.transform != null && (Capacity <= 0 || Total <= Capacity))
                        {
                            _PoolLogger.Trace("Pool[{0}] 回收一个。", Prefab.name);
                            clone.transform.SetParent(this.transform, false);
                            _Cache.Push(clone);
                            returned = true;
                        }
                    }
                    catch
                    {
                        _PoolLogger.Warn("Pool[{0}] 回收时异常。", Prefab.name);
                    }

                    if (!returned)
                    {
                        _PoolLogger.Trace("Pool[{0}] 销毁多余的对象。", Prefab.name);
                        GameObject.Destroy(clone);
                        Destoried++;
                    }
                }
            }
            #endregion

            #region 私有
            private class FloatBox
            {
                public float Value;
            }
            private Stack<GameObject> _Cache = new Stack<GameObject>();
            private Dictionary<GameObject, FloatBox> _DelayedDespawnDict = new Dictionary<GameObject, FloatBox>();

            private List<GameObject> _GOTobeRemovedFromDelay = new List<GameObject>();
            void Update()
            {
                if (!DisablePreloadInUpdate)
                    DoPreload(true);

                if (_DelayedDespawnDict.Count == 0)
                    return;

                foreach (var kv in _DelayedDespawnDict)
                {
                    kv.Value.Value -= Time.deltaTime;
                    if (kv.Value.Value <= 0.0f)
                        _GOTobeRemovedFromDelay.Add(kv.Key);
                }

                if (_GOTobeRemovedFromDelay.Count == 0)
                    return;

                _GOTobeRemovedFromDelay.ForEach(x =>
                {
                    _DelayedDespawnDict.Remove(x);
                    SafeDespawn(x);
                });
                _GOTobeRemovedFromDelay.Clear();
            }

            private GameObject DoClone(Vector3 position, Quaternion rotation, Transform parent)
            {
                _PoolLogger.Trace("Pool[{0}] 生成一个。", Prefab.name);
                var clone = Instantiate(Prefab, position, rotation);
                clone.name = Prefab.name + " " + Cloned++;
                clone.transform.SetParent(parent, false);
                clone.SetActive(true);
                return clone;
            }

            private void SafeSendNotificationIfConfiged(GameObject clone, string messageName)
            {
                if (clone == null || Notification != NotificationTypeLP.SendMessage)
                    return;

                _PoolLogger.Trace("GO[{0}] {1}", clone.name, messageName);
                if (messageName == "OnSpawn")
                    clone.GetComponents<ISpawnDespawnReceivable>().ForEachG(x => x.OnSpawn());
                else if (messageName == "OnDespawn")
                    clone.GetComponents<ISpawnDespawnReceivable>().ForEachG(x => x.OnDespawn());
            }
            #endregion
        }
    }
}

