using GLib;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using BeanDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, HSFrameWork.ConfigTable.BaseBean>>;
using HSFrameWork.Common;
using HSFrameWork.ConfigTable.Inner;

namespace HSFrameWork.ConfigTable.Editor.Impl
{
    public interface IXMLBDLoader : IBeanDictColdLoader, IXMLFileList
    {
    }

    /// <summary>
    /// 跨平台。Stateless! 只提供工具函数，没有任何内部状态
    /// </summary>
    public class XMLBDLoader : IXMLBDLoader
    {
        public void LoadAll(BeanDict value, Action<string, float> onProgress)
        {
            LoadAll(value, CancellationToken.None, onProgress);
        }

        public virtual string[] XmlFiles
        {
            get
            {
                return Directory.GetFiles(HSCTC.XmlPath, "*.xml");
            }
        }
 
        public void LoadAll(BeanDict values, object cancelToken, Action<string, float> onProgress)
        {
            values.Clear();
            try
            {
                DoWork(values, XmlFiles, (CancellationToken)cancelToken, onProgress);
            }
            catch (Exception e)
            {
                HSUtils.LogException(e);
                HSUtils.LogError("XMLBDLoader.LoadFromXMLInner出现异常，已经清空所有数据。");
                values.Clear();
                throw e;
            }
        }

        private class BuildStatus
        {
            public int skipped;
            public int build;
            public int fault;
        }

        internal static void DoWork(BeanDict values, string[] filelist, CancellationToken token, Action<string, float> onProgress)
        {
            using (HSUtils.ExeTimer("XMLBDLoader.DoWork"))
            {
                BuildStatus status = new BuildStatus();
                int count = 0;
                if (TE.NoThreadExtention)
                {
                    HSUtils.Log("XMLBDLoader.DoWork 在单线程工作".EatWithTID());
                    foreach (var file in filelist)
                    {
                        count++;
                        if (onProgress != null)
                            onProgress(file, (count + 0.0f) / filelist.Length);

                        DoOneFile(values, file, status);
                    }
                }
                else
                {
                    HSUtils.Log("XMLBDLoader.DoWork 多线程线程工作模式".EatWithTID());
                    Parallel.ForEach(filelist, Utils.CreatePO(token), file =>
                    {
                        TE.SafeMarkThreadAsPool();

                        Interlocked.Increment(ref count);
                        if (onProgress != null)
                            onProgress(file, (count + 0.0f) / filelist.Length);

                        DoOneFile(values, file, status);
                    });
                }

                HSUtils.Log("▲▲▲▲▲▲▲▲▲▲▲  XML冷加载完成：从缓存加载 [{0}] 个，从XML加载 [{1}] 个，无法加载 [{2}] 个。".EatWithTID(status.skipped, status.build, status.fault));
                HSUtils.Log("▲▲▲▲▲▲▲▲▲▲▲  XML冷加载完成：总共加载 [{0}] 个类，[{1}]个Pojo。".EatWithTID(values.Count, values.Values.Select(beans => beans.Count).Sum()));
            }
        }

        public void Test_DoOneFile(string file)
        {
            foreach (XmlNode node in Utils.LoadXmlFile(file).SelectSingleNode("root").ChildNodes)
            {
                CreateBasePojo(BeanNodeMap.Get(node.Name), node.OuterXml);
            }        
        }

        static void DoOneFile(BeanDict values, string file, BuildStatus status)
        {
            Dictionary<string, BaseBean> beans;

            string obj = HSCTC.XMLObjPath.StandardSub(file.ShortName() + ".obj");

            if (HSCTC.UseXMLObj && File.Exists(obj) && File.GetLastWriteTime(file) == File.GetLastWriteTime(obj))
            {   //如果用旧文件替换，也会重新生成。
                beans = ProtoBufTools.Deserialize<Dictionary<string, BaseBean>>(File.ReadAllBytes(obj));
                Interlocked.Increment(ref status.skipped);
            }
            else
            {
                beans = new Dictionary<string, BaseBean>();
                try
                {
                    HSUtils.Log("Loading: [{0}] ...".EatWithTID(file.ShortName()));
                    foreach (XmlNode node in Utils.LoadXmlFile(file).SelectSingleNode("root").ChildNodes)
                    {
                        var bean = CreateBasePojo(BeanNodeMap.Get(node.Name), node.OuterXml);
                        if (beans.ContainsKey(bean.PK))
                        {
                            throw new Exception("策划EXCEL编写错误：XML文件 [{0}] 中有重复的KEY: [{1}]".Eat(new FileInfo(file).Name, bean.PK));
                        }
                        beans.Add(bean.PK, bean);
                    }

                    if (HSCTC.UseXMLObj)
                    {
                        ProtoBufTools.Serialize(beans, obj);
                        file.Touch(obj);
                    }
                    Interlocked.Increment(ref status.build);
                }
                catch (KeyNotFoundException e)
                {
                    Interlocked.Increment(ref status.fault);
                    //BeanNodeMap.Get会抛出这个异常，我们只用忽略即可。这个经常发生。
                    HSUtils.LogWarning("[{0}] : {1}".EatWithTID(file.ShortName(), e.Message));
                }
            }

            foreach (var bean in beans.Values)
                AddBean2Values(values, bean);
        }

        /// <summary>
        /// 返回true表示之前有，现在只是更新
        /// </summary>
        internal static bool AddBean2Values(BeanDict values, BaseBean obj)
        {
            var fullname = obj.GetType().FullName;
            lock (values)
            {
                if (!values.ContainsKey(fullname))
                    values.Add(fullname, new Dictionary<string, HSFrameWork.ConfigTable.BaseBean>());

                bool update = values[fullname].ContainsKey(obj.PK);
                values[fullname][obj.PK] = obj;
                return update;
            }
        }

        public static BaseBean CreateBasePojo(Type type, string xml)
        {
            return ToolsShared.DeserializeXML(type, xml) as BaseBean;
        }
    }
}
