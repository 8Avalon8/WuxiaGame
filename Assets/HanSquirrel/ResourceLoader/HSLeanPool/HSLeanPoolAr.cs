using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;
using System.Diagnostics;

namespace HanSquirrel.ResourceManager
{
    public partial class ResourceLoader
    {
        #region 纯粹异步版本，性能不稳定。
        /// <summary>
        /// 此版本是纯粹的异步版本。
        /// 将缺省配置和附加配置中的Prefab全部加载，创建好Pool，并且Preload完成。
        /// 缺省配置并不会被覆盖。
        /// </summary>
        public static IEnumerator PreloadAsyncV0(Action<bool, float> onProgress = null, IEnumerable<HSLeanPoolConfig> additionalPoolConfigs = null, string title = "NA")
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            List<HSLeanPoolConfig> preloadTasks =
                (additionalPoolConfigs == null ? _PathConfigDict.Values :
                        _PathConfigDict.Values.Union(additionalPoolConfigs)).ToList();
            List<LeanPool> loadingPools = new List<LeanPool>();

            int taskCount = preloadTasks.Count;
            bool firstCall = true;
            while (true)
            {
                if (RefreshPreloadTasks(ref firstCall, ref taskCount, preloadTasks, loadingPools, onProgress))
                {
                    _Logger.Info("★★★★ PreloadAsync[{0}] completed. 总花费[{1}ms]", title, stopWatch.Elapsed.TotalMilliseconds);
                    yield break;
                }
                else
                    yield return null;
            }
        }

        private static bool RefreshPreloadTasks(ref bool firstCall, ref int taskCount, List<HSLeanPoolConfig> preloadTasks, List<LeanPool> loadingPools, Action<bool, float> onProgress)
        {
            for (int i = preloadTasks.Count - 1; i >= 0; i--)
            {
                GameObject prefab = null;
                if (_AssetLoader.TryGetPrefabFromCache(preloadTasks[i].Path, out prefab))
                {
                    if (prefab != null)
                    {
                        loadingPools.Add(GetOrAdd(prefab).ResetPropertiesFromConfig(preloadTasks[i]));
                    }
                    preloadTasks.RemoveAt(i);
                }
                else if (firstCall)
                {
                    _AssetLoader.LoadPrefabCachedAsyncQuick(preloadTasks[i].Path, null, null);
                }
            }

            for (int i = loadingPools.Count - 1; i >= 0; i--)
            {
                if (loadingPools[i].PreloadCompleted)
                    loadingPools.RemoveAt(i);
            }

            bool completed = preloadTasks.Count == 0 && loadingPools.Count == 0;

            if (firstCall)
                taskCount = preloadTasks.Count + loadingPools.Count;
            else if (onProgress != null)
                onProgress(completed, 1.0f - (preloadTasks.Count + loadingPools.Count) * 1.0f / taskCount);

            firstCall = false;
            return completed;
        }
        #endregion
    }
}