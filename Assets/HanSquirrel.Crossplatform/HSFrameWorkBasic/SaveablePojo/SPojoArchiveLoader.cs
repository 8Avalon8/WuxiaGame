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
        /// 此类仅仅依赖于Saveable，无配置有状态；使用方法：Load
        /// </summary>
        private static partial class ArchiveLoader
        {
            private class PojoDict
            {
                private Dictionary<Type, Dictionary<int, Saveable>> _pojos = new Dictionary<Type, Dictionary<int, Saveable>>();
                public Saveable SafeGet(Type type, int id)
                {
                    return (_pojos.ContainsKey(type) && _pojos[type].ContainsKey(id)) ? _pojos[type][id] : null;
                }

                public void Add(Saveable pojo)
                {
                    Type type = pojo.GetType();
                    if (!_pojos.ContainsKey(type))
                        _pojos[type] = new Dictionary<int, Saveable>();
                    _pojos[type][pojo.Id()] = pojo;
                }

                public void InitBind(Saveable rootPojo)
                {
                    foreach (var subdict in _pojos.Values)
                    {
                        foreach (var pojo in subdict.Values)
                        {
                            if (pojo != rootPojo)
                                pojo.InitBind();
                        }
                    }

                    if (rootPojo != null)
                        rootPojo.InitBind();
                }
            }

            public static int GetRootId<T>(Hashtable serverData)
            {
                string rootType = typeof(T).ToString();
                foreach (string key in serverData.Keys)
                {
                    SaveNameUtils.TypeIdPair ti = SaveNameUtils.SplitSaveName(key);
                    if (ti == null)
                    {
                        HSUtils.LogError("存档数据KEY格式不对：{0}", key);
                        return -1;
                    }

                    if (rootType == ti.type)
                    {
                        return ti.id;
                    }
                }

                HSUtils.LogError("存档数据格式不对，找不到类：[{0}]。", rootType);
                return -1;
            }

            public static List<string> WarnInfo; //每次Load前会自动新建

            /// <summary> 会将serverData中已经创建的数据移除。因此函数返回时，serverData剩余的就是游离节点。在仅仅测试加载的时候需要设置NoInitBind。 </summary>
            public static T LoadRoot<T>(Hashtable serverData, bool NoInitBind = false) where T : Saveable, new()
            {
                DebugFacade.ArchiveLoaderConfiger.DebugTime = 0;
                int rootId = GetRootId<T>(serverData);
                if (rootId == -1)
                    return null;

                int serverDataCount = serverData.Count;
                string[] invalidKeys = null;
                if (DebugFacade.ArchiveLoaderConfiger.CompareWithMethodV0)
                    using (var dbtime1 = HSUtils.ExeTimerEnd("DebugTime1"))
                    {
                        invalidKeys = ArchiveLoaderLegency.GetInvalidKeysV0(typeof(T), rootId, serverData);
                        DebugFacade.ArchiveLoaderConfiger.DebugTime += dbtime1.DisposeEx();
                    }

                T rootPojo;
                PojoDict pojoDict;
                WarnInfo = new List<string>();
                using (HSUtils.ExeTimerEnd("★★★★ CreateSaveablePojoRoot"))
                {
                    pojoDict = new PojoDict();
                    if (DebugFacade.ArchiveLoaderConfiger.CreateSeqDebugFile == null)
                    {
                        rootPojo = GetOrCreatePojo(typeof(T), rootId, pojoDict, serverData, null, 0, WarnInfo) as T;
                    }
                    else using (StreamWriter sw = File.AppendText(DebugFacade.ArchiveLoaderConfiger.CreateSeqDebugFile))
                        {
                            sw.WriteLine("_______________新版本遍历RumtimeData的序列_______________");
                            rootPojo = GetOrCreatePojo(typeof(T), rootId, pojoDict, serverData, sw, 0, WarnInfo) as T;
                        }
                }
                using (var dbtime = HSUtils.ExeTimerEnd("DebugTime2"))
                {
                    if (DebugFacade.ArchiveLoaderConfiger.ServerDataAfterLoad != null)
                        DebugFacade.ArchiveLoaderConfiger.ServerDataAfterLoad("[{0}] 新版本，原有 [{1}] 个。".Eat(rootId, serverDataCount), rootPojo, serverData, WarnInfo);

                    DisplayCompareResult(serverDataCount, serverData, invalidKeys);
                    DebugFacade.ArchiveLoaderConfiger.DebugTime += dbtime.DisposeEx();
                }

                if (!NoInitBind)
                    using (HSUtils.ExeTimerEnd("★★★★ pojoDict.InitBind"))
                        pojoDict.InitBind(rootPojo);
                return rootPojo;
            }

            /// <summary>
            /// 递归创建所有Saveable：pojoDict是字典用于查询是否已经创建过了；创建了的serverData会被删除。
            /// </summary>
            private static Saveable GetOrCreatePojo(Type type, int id, PojoDict pojoDict, Hashtable serverData, StreamWriter swDebug, int level, ICollection<string> warningInfo)
            {
                Saveable pojo = pojoDict.SafeGet(type, id);
                if (pojo != null)
                    return pojo as Saveable;

                string saveName = SaveNameUtils.GetSaveName(type, id);
                if (!serverData.Contains(saveName))
                {
                    warningInfo.Add("新版本发现空引用: [{0}]".Eat(saveName));
                    return null;
                }

                if (swDebug != null)
                    swDebug.WriteLine(new string('\t', level) + saveName);

                var ret = Saveable.CreateNoInitBind(id, type, serverData[saveName] as Hashtable, pojoDict.Add, (type1, id1, swDebug1, level1) => GetOrCreatePojo(type1, id1, pojoDict, serverData, swDebug1, level1, warningInfo), swDebug, level, warningInfo);
                serverData.Remove(saveName);

                return ret;
            }

            //因为没有去找到新老算法不一致的数据，故此。。。
#if HSFRAMEWORK_RUN_IN_MS_CONSOLE
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
            private static void DisplayCompareResult(int serverDataCount, Hashtable serverData, string[] invalidKeys)
            {
                if (DebugFacade.ArchiveLoaderConfiger.CompareWithMethodV0)
                {
                    if (serverData.Keys.SafeSetEquals<string>(invalidKeys))
                        HSUtils.Log("★★★★ 最新最老遍历算法结果相同: 总共{0}，剩余{1}", serverDataCount, invalidKeys == null ? 0 : invalidKeys.Length);
                    else
                    {
                        HSUtils.LogError("★★★★ 最新最老遍历算法结果不同。");
                        if (DebugFacade.ArchiveLoaderConfiger.OnDiffFromV0 != null)
                            DebugFacade.ArchiveLoaderConfiger.OnDiffFromV0(invalidKeys);
                        throw new Exception("程序有BUG，无法继续进行。");
                    }
                }
            }
        }
    }
}