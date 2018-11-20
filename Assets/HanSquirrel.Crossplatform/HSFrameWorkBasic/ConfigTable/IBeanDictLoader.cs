using System;
using System.Collections.Generic;

namespace HSFrameWork.ConfigTable.Inner
{
    public interface IXMLFileList
    {
        string[] XmlFiles { get; }
    }

    /// <summary>
    /// 从XML中加载Pojo，要求无状态。加载过程中出现异常会清空values数据。不会调用InitBind
    /// </summary>
    public interface IBeanDictColdLoader
    {
        /// <summary>
        /// 这里用object cancelToken是因为不能将CancelationToken暴露给运行时
        /// </summary>
        void LoadAll(Dictionary<string, Dictionary<string, BaseBean>> values, object cancelToken, Action<string,float> onProgress);
        void LoadAll(Dictionary<string, Dictionary<string, BaseBean>> values, Action<string, float> onProgress);
    }

    /// <summary>
    /// 从XML中加载或者更新Pojo，有状态，更新过程中出现异常并不会清空values数据。会调用InitBind
    /// </summary>
    public interface IBeanDictWarmUpdater
    {
        /// <summary>
        /// 这里用object cancelToken是因为不能将CancelationToken暴露给运行时
        /// </summary>
        void UpdateChanged(Dictionary<string, Dictionary<string, BaseBean>> values, object cancelToken, Action<string, float> onProgress);
        void UpdateChanged(Dictionary<string, Dictionary<string, BaseBean>> values, Action<string, float> onProgress);
        /// <summary>
        /// 复位后，下次会全部加载
        /// </summary>
        IBeanDictWarmUpdater Reset();
    }

    public interface IBeanDictWarmUpdaterEasy
    {
        void UpdateChanged(Dictionary<string, Dictionary<string, BaseBean>> values);
        IBeanDictWarmUpdater Reset();
    }
}
