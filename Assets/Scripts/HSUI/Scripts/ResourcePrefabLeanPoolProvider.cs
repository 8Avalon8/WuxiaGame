/********************************************************************************

** Copyright(c) 2018 汉家松鼠工作室 All Rights Reserved. 

** auth： cg
** date： 2018/7/27 17:31:43 
** desc： 尚未编写描述 

*********************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HSUI
{
    class ResourcePrefabProvider : MonoBehaviour,IUIProvider
    {
        Dictionary<string, GameObject> _cache = new Dictionary<string, GameObject>();

        public void Despawn(IHSUIComponent panel)
        {
            Destroy(panel.gameObject);
        }

        public T Spawn<T>(string path = null) where T : MonoBehaviour, IHSUIComponent
        {
            GameObject prefab = LoadWithCache(path);
            if (prefab == null)
            {
                return null;
            }
            var obj = DoClone(prefab);
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

        GameObject DoClone(GameObject prefab,Transform parent = null)
        {
            var clone = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            // clone.name = prefab.name + " " + Cloned++;
            clone.name = prefab.name;
            clone.transform.SetParent(parent, false);
            clone.SetActive(true);
            return clone;
        }
    }
}
