using UnityEngine;

namespace HanSquirrel.ResourceManager
{
    public partial class ResourceLoader
    {
        #region 快捷函数
        /// <summary> 强制创建对应的Pool，无论是否配置过。 </summary>
        public static GameObject Spawn(string prefabPath)
        {
            GameObject prefab = LoadPrefabCached(prefabPath);
            if (prefab == null)
                return null;

            return Spawn(prefab, Vector3.zero, Quaternion.identity, null, prefabPath);
        }

        /// <summary> 强制创建对应的Pool，无论是否配置过。 </summary>
        public static GameObject Spawn(GameObject prefab, string prefabPath = null)
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, null, prefabPath);
        }

        /// <summary> 强制创建对应的Pool，无论是否配置过。 </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, string prefabPath = null)
        {
            return Spawn(prefab, position, rotation, null, prefabPath);
        }

        /// <summary> 强制创建对应的Pool，无论是否配置过。 </summary>
        public static T SpawnComponent<T>(T component, string prefabPath = null) where T : Component
        {
            return SpawnComponent(component, Vector3.zero, Quaternion.identity, null, prefabPath);
        }

        /// <summary> 强制创建对应的Pool，无论是否配置过。 </summary>
        public static T SpawnComponent<T>(T component, Vector3 position, Quaternion rotation, string prefabPath = null) where T : Component
        {
            return SpawnComponent(component, position, rotation, null, prefabPath);
        }

        /// <summary> 强制创建对应的Pool，无论是否配置过。 </summary>
        public static T SpawnComponent<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent, string prefabPath = null) where T : Component
        {
            var gameObject = prefab != null ? prefab.gameObject : null;
            var clone = Spawn(gameObject, position, rotation, parent, prefabPath);
            return clone != null ? clone.GetComponent<T>() : null;
        }

        /// <summary>
        /// 在父对象Disable的时候来归还该Component
        /// </summary>
        public static void DespawnWhenDisable(Component clone)
        {
            DespawnOrDestory(clone, 0.1f);
        }

        /// <summary> 如果clone不是本Pool分配的或者本Pool已满，则会直接被Destroy；否则就会回归到池子中。 </summary>
        public static void DespawnOrDestory(Component clone, float delay = 0.0f)
        {
            if (clone != null) DespawnOrDestory(clone.gameObject, delay);
        }
        #endregion

        /// <summary>
        /// 创建实例化的GameObject；
        /// 如果已经有对应的Pool或者配置需要在Pool里面，则从Pool里面取。
        /// 如果要强制使用Pool，则调用Spawn函数
        /// </summary>
        public static GameObject CreatePrefabInstance(string path)
        {
            GameObject prefab = LoadPrefabCached(path);
            if (prefab == null)
                return null;

            return CanBePooled(path) ? Spawn(prefab, path) : GameObject.Instantiate(prefab);
        }

        /// <summary>
        /// 创建实例化的GameObject返回泛型类型；
        /// 如果已经有对应的Pool或者配置需要在Pool里面，则从Pool里面取。
        /// 如果要强制使用Pool，则调用Spawn函数
        /// </summary>
        public static T CreatePrefabInstance<T>(string prefabPath) where T : Component
        {
            var go = CreatePrefabInstance(prefabPath);
            return go == null ? default(T) : go.GetComponent<T>();
        }
    }
}
