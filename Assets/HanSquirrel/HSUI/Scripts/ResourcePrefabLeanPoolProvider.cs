/********************************************************************************

** Copyright(c) 2018 汉家松鼠工作室 All Rights Reserved. 

** auth： cg
** date： 2018/7/27 17:31:43 
** desc： 尚未编写描述 

*********************************************************************************/

using HanSquirrel.ResourceManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HSUI
{
    class ResourcePrefabLeanPoolProvider : IUIProvider
    {
        Dictionary<string, GameObject> _cache = new Dictionary<string, GameObject>();

        public void Despawn(IHSUIComponent panel)
        {
            ResourceLoader.DespawnOrDestory(panel.transform);
        }

        public T Spawn<T>(string path = null) where T : MonoBehaviour, IHSUIComponent
        {
            GameObject prefab = LoadWithCache(path);
            if (prefab == null)
            {
                return null;
            }
            var obj = ResourceLoader.Spawn(prefab);
            return obj.GetComponent<T>();
        }

        GameObject LoadWithCache(string path)
        {
            if (_cache.ContainsKey(path))
                return _cache[path];

            var prefab = Resources.Load<GameObject>(path);
            _cache.Add(path, prefab);
            return prefab;
        }
    }
}
