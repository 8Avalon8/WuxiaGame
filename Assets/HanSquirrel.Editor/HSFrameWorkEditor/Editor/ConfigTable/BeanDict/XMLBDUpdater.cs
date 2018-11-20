using System;
using System.Linq;
using System.IO;
using System.Threading;
using BeanDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, HSFrameWork.ConfigTable.BaseBean>>;
using GLib;
using HSFrameWork.Common;
using HSFrameWork.ConfigTable.Inner;

namespace HSFrameWork.ConfigTable.Editor.Impl
{
    public interface IXMLBDUpdater : IBeanDictWarmUpdater, IXMLFileList
    {
    }

    /// <summary>
    /// 跨平台
    /// </summary>
    public class XMLBDUpdater : IXMLBDUpdater
    {
        public static XMLBDUpdater Instance { get { return _instance; } }
        private static XMLBDUpdater _instance = new XMLBDUpdater();
        protected XMLBDUpdater() { }

        public virtual string[] XmlFiles { get { return Directory.GetFiles(HSCTC.XmlPath, "*.xml"); } }

        public void UpdateChanged(BeanDict values, object cancelToken, Action<string, float> onProgress)
        {
            TE.ThrowIfNotInUI("XMLBDUpdater.UpdateChanged");

            var now = DateTime.Now;

            var updateFiles = XmlFiles.Where(fi => fi.LastWriteTime() > _lastReloadTime).ToArray();

            HSUtils.Log("XMLBDUpdater.UpdateChanged: 总共需要更新{0}个XML文件。".EatWithTID(updateFiles.Length));

            //必须单线程加载。多线程时会出一些奇怪的事情，还没有去检查。
            using (RunTimeConfiger.EnterNoThreadExtentionMode)
            {
                BeanDict tempVs = new BeanDict();
                //在加载的时候需要禁止运行时代码访问ResourceManager，
                using (RunTimeFrozenChecker.TempFrozen("从XML中加载Pojo"))
                    XMLBDLoader.DoWork(tempVs, updateFiles, cancelToken == null ? CancellationToken.None : (CancellationToken)cancelToken, onProgress);

                using (HSUtils.ExeTimer("XMLBDUpdater.UpdateChanged.InitBindAllNew"))
                {
                    int newCount = 0;
                    int updated = 0;
                    foreach (var bean in tempVs.Values.SelectMany(beans => beans.Values))
                    {
                        var _ = XMLBDLoader.AddBean2Values(values, bean) ? ++updated : ++newCount;
                    }

                    foreach (var bean in BeanNodeMap.Types.Select(t => t.FullName).Where(fullName => tempVs.ContainsKey(fullName))
                                                          .SelectMany(fullName => tempVs[fullName].Values))
                        bean.InitBind();

                    HSUtils.Log("▲▲▲▲▲▲▲▲▲▲▲  XML热加载完成：更新了 [{0}] 个Pojo，新加了 [{1}] 个Pojo。".EatWithTID(updated, newCount));
                }
            }
            _lastReloadTime = now;
        }

        public void UpdateChanged(BeanDict value, Action<string, float> onProgress)
        {
            UpdateChanged(value, CancellationToken.None, onProgress);
        }

        public IBeanDictWarmUpdater Reset()
        {
            _lastReloadTime = DateTime.MinValue;
            return this;
        }
        private DateTime _lastReloadTime; //上次加载时间，用于EDIT_MODE下的判断增量更新资源
    }
}
