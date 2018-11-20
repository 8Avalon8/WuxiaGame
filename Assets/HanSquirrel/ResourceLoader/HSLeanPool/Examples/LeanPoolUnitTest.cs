#if UNITY_EDITOR
using HSFrameWork.Common;
using System.Collections.Generic;
using UnityEngine;

namespace HanSquirrel.ResourceManager.Test
{
    public class LeanPoolUnitTest
    {
        /// <summary>
        /// ConStr.BaseSideBarToggle
        /// </summary>
        private static string _PrefabPathKey;
        public static void DoWork(string pathKey)
        {
            _PrefabPathKey = pathKey;
            using (HSUtils.ExeTimer("LeanPool测试"))
            {
                Debug.LogWarning("LeanPool开始测试。");
                Debug.Log(ResourceLoader.DumpCurrentPoolStatus());

                Clear();
                SimpleSpawn();
                Clear();
                SetConfigClear();
                Clear();
                ForcellyPreload();
                Clear();
                ResetCapToSmaller();
                Clear();

                Debug.LogWarning("LeanPool测试通过。");
            }
        }

        private static void Clear()
        {
            ResourceLoader.ResetPoolConfig(new HSLeanPoolConfig[] { });
            ResourceLoader.ClearAllTempPools();
            ResourceLoader.AssertEmpty();
        }

        private static void ResetCapToSmaller()
        {
            ResourceLoader.PreLoad(_PrefabPathKey, 9);
            var p = ResourceLoader.LoadPrefabCached(_PrefabPathKey);
            ResourceLoader.AssertInPool(p, 9, 9, 9);
            ResourceLoader.AssertPoolCount(1);
            ResourceLoader.AssertAllocatedCount(0);

            ResourceLoader.ResetPoolConfig(new HSLeanPoolConfig[]
            {
                new HSLeanPoolConfig(_PrefabPathKey, 5,0)
            });
            ResourceLoader.AssertInPool(p, 5, 5, 9);
            ResourceLoader.AssertPoolCount(1);
            ResourceLoader.AssertAllocatedCount(0);

            List<GameObject> gos = new List<GameObject>();
            for (int i = 0; i < 5; i++)
            {
                ResourceLoader.AssertInPool(p, 5, 5 - i, 9);
                gos.Add(ResourceLoader.CreatePrefabInstance(_PrefabPathKey));
                ResourceLoader.AssertInPool(p, 5, 4 - i, 9);
                ResourceLoader.AssertPoolCount(1);
                ResourceLoader.AssertAllocatedCount(i + 1);
            }

            for (int i = 0; i < 5; i++)
            {
                ResourceLoader.AssertInPool(p, 5 + i, 0, 9 + i);
                gos.Add(ResourceLoader.CreatePrefabInstance(_PrefabPathKey));
                ResourceLoader.AssertInPool(p, 6 + i, 0, 10 + i);
                ResourceLoader.AssertPoolCount(1);
                ResourceLoader.AssertAllocatedCount(i + 6);
            }

            for (int i = 0; i < 5; i++)
            {
                ResourceLoader.DespawnOrDestory(gos[i]);
                ResourceLoader.AssertInPool(p, 9 - i, 0, 14);
                ResourceLoader.AssertPoolCount(1);
                ResourceLoader.AssertAllocatedCount(9 - i);
            }

            for (int i = 5; i < 10; i++)
            {
                ResourceLoader.DespawnOrDestory(gos[i]);
                ResourceLoader.AssertInPool(p, 5, i-4, 14);
                ResourceLoader.AssertPoolCount(1);
                ResourceLoader.AssertAllocatedCount(9 - i);
            }

            ResourceLoader.ClearAllTempPools();
            ResourceLoader.AssertInPool(p, 5, 5, 14);
            ResourceLoader.AssertPoolCount(1);
            ResourceLoader.AssertAllocatedCount(0);
        }

        private static void ForcellyPreload()
        {
            ResourceLoader.PreLoad(_PrefabPathKey, 9);
            var p = ResourceLoader.LoadPrefabCached(_PrefabPathKey);
            ResourceLoader.AssertInPool(p, 9, 9, 9);
            ResourceLoader.AssertPoolCount(1);
            ResourceLoader.AssertAllocatedCount(0);

            List<GameObject> gos = new List<GameObject>();
            for (int i = 0; i < 9; i++)
            {
                ResourceLoader.AssertInPool(p, 9, 9 - i, 9);
                gos.Add(ResourceLoader.CreatePrefabInstance(_PrefabPathKey));
                ResourceLoader.AssertInPool(p, 9, 8 - i, 9);
                ResourceLoader.AssertPoolCount(1);
                ResourceLoader.AssertAllocatedCount(i + 1);
            }

            for (int i = 0; i < 5; i++)
            {
                ResourceLoader.AssertInPool(p, 9 + i, 0, 9 + i);
                gos.Add(ResourceLoader.CreatePrefabInstance(_PrefabPathKey));
                ResourceLoader.AssertInPool(p, 10 + i, 0, 10 + i);
                ResourceLoader.AssertPoolCount(1);
                ResourceLoader.AssertAllocatedCount(10 + i);
            }

            for (int i = 0; i < gos.Count; i++)
            {
                ResourceLoader.DespawnOrDestory(gos[i]);
                ResourceLoader.AssertInPool(p, 14, i + 1, 14);
                ResourceLoader.AssertPoolCount(1);
                ResourceLoader.AssertAllocatedCount(13 - i);
            }

            ResourceLoader.ClearAllTempPools();
            ResourceLoader.AssertEmpty();
        }

        private static void SetConfigClear()
        {
            ResourceLoader.ResetPoolConfig(new HSLeanPoolConfig[]
            {
                new HSLeanPoolConfig("BaseSideBarToggle", 0,0),
            });
            ResourceLoader.AssertEmpty();

            var p = MultiOps();

            ResourceLoader.ClearAllTempPools();

            ResourceLoader.AssertInPool(p, 10, 10, 10);
            ResourceLoader.AssertPoolCount(1);
            ResourceLoader.AssertAllocatedCount(0);
        }

        private static void SimpleSpawn()
        {
            ResourceLoader.AssertEmpty();

            var p = MultiOps();

            ResourceLoader.ClearAllTempPools();

            ResourceLoader.AssertEmpty();
            ResourceLoader.AssertNotInPool(p);
        }

        private static GameObject MultiOps()
        {
            var p1 = ResourceLoader.LoadPrefabCached(_PrefabPathKey);
            ResourceLoader.AssertNotInPool(p1);

            List<GameObject> olist = new List<GameObject>();
            for (int i = 0; i < 10; i++)
            {
                olist.Add(ResourceLoader.Spawn(p1));
                ResourceLoader.AssertInPool(p1, i + 1, 0, i + 1);
                ResourceLoader.AssertPoolCount(1);
                ResourceLoader.AssertAllocatedCount(i + 1);
            }

            for (int i = 0; i < 10; i++)
            {
                ResourceLoader.DespawnOrDestory(olist[i]);

                ResourceLoader.AssertPoolCount(1);
                ResourceLoader.AssertInPool(p1, 10, i + 1, 10);
                ResourceLoader.AssertAllocatedCount(9 - i);
            }

            List<GameObject> olist2 = new List<GameObject>();
            for (int i = 0; i < 10; i++)
            {
                ResourceLoader.AssertInPool(p1, 10, 10 - i, 10);
                olist2.Add(ResourceLoader.Spawn(p1));
                ResourceLoader.AssertInPool(p1, 10, 9 - i, 10);
                ResourceLoader.AssertPoolCount(1);
                ResourceLoader.AssertAllocatedCount(i + 1);
            }

            for (int i = 0; i < 10; i++)
            {
                ResourceLoader.DespawnOrDestory(olist[i]);
            }
            return p1;
        }
    }
}
#endif
