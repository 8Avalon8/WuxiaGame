using GLib;
using HSFrameWork;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HSFrameWork.SPojo
{
    using Common.Inner;
    using Inner;

    public abstract partial class Saveable
    {
        /// <summary>
        /// 所有静态函数都从这里调用。
        /// </summary>
        public static class Facade
        {
            /// <summary> 清理所有 HSFrameWork 内部的东西 </summary>
            public static void Reset()
            {
                MaxIDUtils.Reset();
            }

            /// <summary>
            /// 创建所有SPojo的根。不会调用InitBind。
            /// </summary>
            public static T CreateRoot<T>() where T : Saveable, new()
            {
                return CreateRoot<T>(false);
            }

            /// <summary>
            /// 创建所有SPojo的根。
            /// </summary>
            /// <typeparam name="T">根类型</typeparam>
            /// <param name="initBind">是否调用InitBind</param>
            public static T CreateRoot<T>(bool initBind) where T : Saveable, new()
            {
                Reset();
                T root = new T();
                root.Id();  //Root是唯一一个不会被引用的Pojo，因此如果不调用ID，则内部不会生成ID。
                if (initBind)
                {
                    root.InitBind();//by cg:不能调用initbind，否则会出现协议错误？
                }
                return root;
            }

            /// <summary>
            /// 从文件中加载根对象。
            /// </summary>
            public static T LoadRoot<T>(string file, bool noInitBind = false, bool partial = false) where T : Saveable, new()
            {
                Hashtable serverData = file.ReadAllText().hashtableFromJsonG();
                serverData.Remove("runtimedataToken");
                return Facade.LoadRoot<T>(serverData, noInitBind, partial);
            }

            /// <summary>
            /// 从哈希表中加载根对象。
            /// </summary>
            public static T LoadRoot<T>(Hashtable serverData, bool noInitBind = false, bool partial = false) where T : Saveable, new()
            {
                if (!partial)
                    Reset();
                return ArchiveLoader.LoadRoot<T>(serverData, noInitBind);
            }

            /// <summary>
            /// 生成Pojo的增量提交数据。
            /// </summary>
            /// <param name="pojo">需要处理的Saveable</param>
            /// <param name="submitRoot">数据填充进入的哈希表</param>
            /// <param name="depth">是否递归填充成员</param>
            public static void GenerateSubmit(Saveable pojo, Hashtable submitRoot, bool depth = true)
            {
                pojo.GenerateSubmit(submitRoot, depth);
            }
        }

        /// <summary>
        /// 仅仅HSFrameWork开发者或者外部高级开发者调试使用
        /// </summary>
        public static partial class DebugFacade
        {
            public static IEnumerable<string> GlobalAttrNames()
            {
                return SaveablePojo.GlobalAttrNames();
            }

            public static void ResetPojo(Saveable pojo)
            {
                pojo.Reset();
            }

            /// <summary>
            /// 内部开发使用
            /// </summary>
            public static class ArchiveLoaderConfiger
            {
                public static IDisposable FullReleaseSpeed
                {
                    get
                    {
                        var temp = CompareWithMethodV0;
                        var temps = CreateSeqDebugFile;
                        var tempF = ServerDataAfterLoad;
                        CompareWithMethodV0 = false;
                        CreateSeqDebugFile = null;
                        ServerDataAfterLoad = null;
                        return DisposeHelper.Create(() =>
                        {
                            CompareWithMethodV0 = temp;
                            CreateSeqDebugFile = temps;
                            ServerDataAfterLoad = tempF;
                        });
                    }
                }

                /// <summary>用于测试时输出递归创建Pojo的创建顺序。 </summary>
                public static string CreateSeqDebugFile;
                /// <summary>是否自动和最古老版本比对，在很长一段时间内需要自动对比。会在RunTimeDataDetailLog里面打开。终端只能关闭。</summary>
                public static bool CompareWithMethodV0 = false;
                public static Action<string, Saveable, Hashtable, IEnumerable<string>> ServerDataAfterLoad;
                public static Action<string[]> OnDiffFromV0;
                public static long DebugTime;
                public static List<string> WarnInfo { get { return ArchiveLoader.WarnInfo; } } //上次Load时的警告信息
                public static List<string> WarnInfoV1 { get { return ArchiveLoaderLegency.WarnInfo; } } //上次Load时的警告信息
            }

            public static Action<int, int, Hashtable, List<string>> SubmitRuntimeAction { get; set; }

            public static void DisableSubmitLog()
            {
                SubmitLogger.Disabled = true;
                SubmitLogger.Data2String = null;
            }

            public static void EnableSubmitLog(Func<Hashtable, string> Data2String)
            {
                SubmitLogger.Disabled = false;
                SubmitLogger.Data2String = Data2String;
            }

            public static string GetSubmitStatics()
            {
                return SubmitStatics.GetSubmitStatics();
            }

            public static string GetClassName(string saveName)
            {
                return SaveNameUtils.GetClassName(saveName);
            }

            public static Hashtable ProcessServerDataV1<T>(Hashtable serverData)
            {
                return ArchiveLoaderLegency.ProcessServerDataV1<T>(serverData);
            }

            public static string GetSavebleStatics()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("总共有 [{0}] 个SaveablePojo，Nosubmit [{1}]个，Submit [{2}]个。".Eat(Saveable.InstanceCount, Saveable.NoSubmitCount, Saveable.SubmitCount));
                SaveablePojo.InstanceDict.ToList().SortC((kv1, kv2) => kv1.Value.lived.CompareTo(kv2.Value.lived)).ForEach(kv =>
                {
                    sb.AppendLine("{0} [{1}]个，创建 [{2}] 个，销毁 [{3}] 个。".Eat(kv.Key.FullName, kv.Value.lived, kv.Value.created, kv.Value.destoried));
                });
                return sb.ToString();
            }
        }
    }

    namespace Inner
    {
        public static class TestFacade
        {
            /// <summary>
            /// 一些平时运行不到的的代码的覆盖
            /// </summary>
            public static void Dummy()
            {
                SubmitLogger.Disabled = true;
                SubmitLogger.In("");
            }
        }
    }
}