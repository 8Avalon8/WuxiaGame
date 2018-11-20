using GLib;
using HSFrameWork.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace HSFrameWork.SPojo
{
    using Inner;

    public abstract partial class Saveable
    {
        /// <summary>
        /// 最原始版本的SaveManager中使用的加载。以及稍微优化版本。用作测试对比。
        /// </summary>
        private static class ArchiveLoaderLegency
        {
            #region GG第一版代码
            public static List<string> WarnInfo; //每次Load前会自动新建
            /// <summary>
            /// George第一版的代码：和最老版理论上相同，加上调试信息。
            /// </summary>
            public static Hashtable ProcessServerDataV1<T>(Hashtable serverData)
            {
                int rootId = ArchiveLoader.GetRootId<T>(serverData);
                if (rootId == -1)
                    return null;

                HashSet<string> validNames;
                validNames = new HashSet<string>(); //记录有效的saveName
                WarnInfo = new List<string>();

                //遍历游离的SaveName
                using (HSUtils.ExeTimer("GetValidSaveNames"))
                {
                    if (DebugFacade.ArchiveLoaderConfiger.CreateSeqDebugFile == null)
                    {
                        GetValidSaveNames(typeof(T), rootId, validNames, serverData, null, 0);
                    }
                    else
                    {
                        using (StreamWriter sw = File.AppendText(DebugFacade.ArchiveLoaderConfiger.CreateSeqDebugFile))
                        {
                            sw.WriteLine("_______________老版本遍历RumtimeData的序列_______________");
                            GetValidSaveNames(typeof(T), rootId, validNames, serverData, sw, 0);
                        }
                    }
                }

                Hashtable invalidData = new Hashtable();
                foreach (string saveName in serverData.Keys)
                    if (!validNames.Contains(saveName))
                        invalidData[saveName] = serverData[saveName];

                if (DebugFacade.ArchiveLoaderConfiger.ServerDataAfterLoad != null)
                    DebugFacade.ArchiveLoaderConfiger.ServerDataAfterLoad("[{0}] 老版本，原有 [{1}]个。".Eat(rootId, serverData.Count), null, invalidData, WarnInfo);
                return invalidData;
            }

            /// <summary>
            /// 生成有效的SaveName列表
            /// </summary>
            private static void GetValidSaveNames(Type type, int key, ICollection<string> validNames, Hashtable serverData, StreamWriter swDebug, int level)
            {
                string saveName = SaveNameUtils.GetSaveName(type, key);

                if (validNames.Contains(saveName))//已经遍历过这个saveName了。
                    return;

                if (!serverData.Contains(saveName))
                {
                    WarnInfo.Add("老版本发现空引用: [{0}]".Eat(saveName));
                    //服务端没有这个数据
                    return;
                }

                validNames.Add(saveName);
                if (swDebug != null) swDebug.WriteLine(new string('\t', level) + saveName);

                Hashtable rawData = serverData[saveName] as Hashtable;
                foreach (var property in type.GetProperties())
                {
                    var propertyType = property.PropertyType;
                    if (propertyType.IsValueType || propertyType == typeof(string) || !rawData.Contains(property.Name))
                    {   // 这个属性是普通数值                  这个属性是string               服务端数据没有这个属性
                        continue;
                    }
                    else if (propertyType.IsSubclassOf(typeof(Saveable)))
                    {   //这个属性是 Pojo
                        if (swDebug != null) swDebug.WriteLine(new string('\t', level + 1) + property.Name);
                        GetValidSaveNames(propertyType, Convert.ToInt32(rawData[property.Name]), validNames, serverData, swDebug, level + 2);
                    }
                    else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        Type subT = propertyType.GetGenericArguments()[0];
                        if (subT.IsValueType || subT == typeof(string))
                        {  //这个属性是 List<int>或者List<string>等
                            continue;
                        }
                        else if (subT.IsSubclassOf(typeof(Saveable)))
                        {   //这个属性是 List<Saveable>
                            if (swDebug != null) swDebug.WriteLine(new string('\t', level + 1) + property.Name + "[]");
                            foreach (var subID in (rawData[property.Name] as string).arrayListFromJson())
                            {
                                //这个属性是 List<pojo>
                                GetValidSaveNames(subT, Convert.ToInt32(subID), validNames, serverData, swDebug, level + 2);
                            }
                        }
                    }
                }
            }
            #endregion

            #region 最原始代码，一行都没有改过
            /// <summary>
            /// 最原始的代码，几乎一行没有修改
            /// </summary>
            public static string[] GetInvalidKeysV0(Type rootType, int rootId, Hashtable data)
            {
                List<string> validKeys = new List<string>(); //记录有效数据

                //遍历游离的KEY
                GenerateRemoveKey(rootType, -1, rootId.ToString(), validKeys, (key) =>
                {
                    return data.Contains(key);
                }, (key) =>
                {
                    return data[key] as Hashtable;
                });

                //需要删key，则需要先删除完成后，再进行后续事情
                if (validKeys.Count < data.Count)
                {
                    string[] invalidKeys = new string[data.Count - validKeys.Count];
                    int i = 0;
                    foreach (string key in new ArrayList(data.Keys))
                    {
                        if (!validKeys.Contains(key))
                        {
                            invalidKeys[i++] = key;
                        }
                    }
                    return invalidKeys;
                }
                else
                {
                    return null;
                }
            }

            private delegate bool HasKeyCallback(string key);

            private delegate Hashtable GetDataCallback(string key);

            private static List<string> GenerateRemoveKey(Type t, int __NOUSE___, string id, List<string> keys, HasKeyCallback hasKeyCallback, GetDataCallback getDataCallback)
            {
                return GenerateRemoveKey2(t, -1, id, keys, hasKeyCallback, getDataCallback);
            }

            private static List<string> GenerateRemoveKey2(Type t, int __NOUSE___, string id, List<string> keys, HasKeyCallback hasKeyCallback, GetDataCallback getDataCallback)
            {
                string saveName = SaveNameUtils.GetSaveName(t, id);

                if (!hasKeyCallback(saveName) || keys.Contains(saveName))
                {
                    return keys;
                }

                Hashtable data = getDataCallback(saveName);

                keys.Add(saveName);

                foreach (var item in t.GetProperties())
                {
                    if (!data.Contains(item.Name) || item.PropertyType.IsValueType || item.PropertyType == typeof(string))
                    {
                        continue;
                    }

                    if (item.PropertyType.IsSubclassOf(typeof(Saveable)))
                    {
                        GenerateRemoveKey(item.PropertyType, -1, data[item.Name].ToString(), keys, hasKeyCallback, getDataCallback);
                    }
                    else if (item.PropertyType.IsGenericType)
                    {
                        Type subT = item.PropertyType.GetGenericArguments()[0];
                        if (subT.IsValueType || subT == typeof(string))
                        {
                            continue;
                        }

                        foreach (var subID in (data[item.Name] as string).arrayListFromJson())
                        {
                            GenerateRemoveKey(subT, -1, subID.ToString(), keys, hasKeyCallback, getDataCallback);
                        }

                    }

                }

                return keys;
            }
            #endregion
        }
    }
}