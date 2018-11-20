#if UNITY_EDITOR
using GLib;
using HanSquirrel.ResourceManager.Impl;
using HSFrameWork.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HanSquirrel.ResourceManager
{
    public partial class ResourceLoader
    {
        private class AssetLoaderFileSYS : SingletonI<AssetLoaderFileSYS, IAssetLoaderInner>, IAssetLoaderInner
        {
            public bool TryGetPrefabFromCache(string path, out GameObject go)
            {
                return _CachePrefabDict.TryGetValue(path, out go);
            }

            public IEnumerator LoadPrefabCachedAsync(string assetPath, object state, Action<GameObject, object> onLoaded)
            {
                LoadPrefabCachedAsyncQuick(assetPath, state, onLoaded);
                yield break;
            }

            public void LoadPrefabCachedAsyncQuick(string assetPath, object state, Action<GameObject, object> onLoaded)
            {
                _Logger.Trace("Prefab [{0}] Loading async... ", assetPath);
                var go = LoadPrefabCached(assetPath);
                _Logger.Trace("Prefab [{0}] Loading async completed. ", assetPath);
                onLoaded(go, state);
            }

            public GameObject LoadPrefabCached(string path)
            {
                GameObject go;
                if (_CachePrefabDict.TryGetValue(path, out go))
                {
                    _Logger.Trace("Prefab [{0}] cached ", path);
                }
                else
                {
                    _Logger.Trace("Prefab [{0}] Loading ", path);
                    go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go != null)
                        _Logger.Trace("Prefab [{0}] Loaded. ", path);
                    else
                        _Logger.Error("Prefab [{0}] Loaded Failed. ", path);

                    _CachePrefabDict.Add(path, go);
                }
                return go;
            }
            private Dictionary<string, GameObject> _CachePrefabDict = new Dictionary<string, GameObject>(); //缓存

            public T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
            {
                if (typeof(T) == typeof(GameObject))
                {
                    throw new Exception("LoadAssetAtPath<T>不支持GameObject，请使用LoadPrefab*");
                }

                _Logger.Trace("{0}[{1}] Loading ", typeof(T).Name, assetPath);
                var ret = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (ret != null)
                    _Logger.Trace("{0}[{1}] Loaded.", typeof(T).Name, assetPath);
                else
                    _Logger.Error("{0}[{1}] Load Failed.", typeof(T).Name, assetPath);
                return ret;
            }

            public T[] LoadAllAsset<T>(string path) where T : UnityEngine.Object
            {
                if (typeof(T) == typeof(GameObject))
                {
                    throw new Exception("LoadAllAsset<T>不支持GameObject，请使用LoadPrefab*");
                }

                if (!path.ExistsAsFolder())
                {
                    _Logger.Error("LoadAllAsset错误：路径不存在：[{0}]", path);
                    return null;
                }

                _Logger.Trace("{0}[{1}] Loading ", typeof(T).Name, path);
                List<T> tArray = new List<T>();
                DirectoryInfo direction = new DirectoryInfo(path);
                FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);

                for (int i = 0; i < files.Length; i++)
                {
                    if (files[i].Name.EndsWith(".meta"))
                    {
                        continue;
                    }
                    string fullPath = files[i].FullName.Replace(@"\", "/").Replace(Application.dataPath, "Assets");
                    T t = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(fullPath);
                    if (t != null && typeof(T) == typeof(Sprite))
                    {
                        UnityEditor.TextureImporter textureImporter = UnityEditor.AssetImporter.GetAtPath(fullPath) as UnityEditor.TextureImporter;
                        if (textureImporter != null)
                        {
                            if (textureImporter.spriteImportMode == UnityEditor.SpriteImportMode.Multiple)
                            {
                                UnityEngine.Object[] objects = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(fullPath);
                                for (int j = 0; j < objects.Length; j++)
                                {
                                    if (objects[j].GetType() != typeof(Texture2D) && objects[j] != null)
                                    {
                                        tArray.Add((T)objects[j]);
                                    }
                                }
                            }
                        }
                    }
                    if (!tArray.Contains(t) && t != null) tArray.Add(t);
                }
                _Logger.Trace("{0}[{1}] Loaded {2}个.", typeof(T).Name, path, tArray.Count);
                return tArray.ToArray();
            }

            public Sprite[] LoadAssetSubAstSprite(string path, string name)
            {
                _Logger.Trace("Sprite[{0}][{1}] Loading", path, name);

                path = path + name + ".png";

                List<Sprite> allSpr = new List<Sprite>();
                UnityEditor.TextureImporter textureImporter = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
                UnityEngine.Object[] objects = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
                for (int i = 0; i < objects.Length; i++)
                {
                    if (objects[i].GetType() != typeof(Texture2D) && objects[i] != null)
                    {
                        allSpr.Add(objects[i] as Sprite);
                    }
                }
                _Logger.Trace("Sprite[{0}][{1}] Loaded. {2}个.", path, name, allSpr.Count);
                return allSpr.ToArray();
            }
        }
    }
}
#endif
