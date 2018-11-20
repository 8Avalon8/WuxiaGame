using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Collections;

using BeanDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, HSFrameWork.ConfigTable.BaseBean>>;
using HSFrameWork.Common;
using GLib;

namespace HSFrameWork.ConfigTable
{
    public enum LoadStatus
    {
        NONE, WORKING, OK, NULL_DATA, UNPACK_ERROR, DESERILATION_ERROR, INITBIND_ERROR
    }
}

namespace HSFrameWork.ConfigTable.Inner
{
    /// <summary>
    /// 多线程不安全。
    /// </summary>
    public class ConfigTableBasic
    {
        public static bool V2Mode = false; //临时
        public static string CurrentTag; //临时


        public LoadStatus Status { get; private set; }

        public Func<byte[]> DoLoadData { set; private get; }
        public Func<string, byte[]> DoLoadDataV2 { set; private get; }

        public ConfigTableBasic()
        {
            Status = LoadStatus.NONE;
        }

        #region 加载接口
        private Thread __loadingThread;
        public void StartInitAsync(string tag)
        {
            if (__loadingThread == null)
            {
                byte[] data = null;
                if (InitSyncPart1(tag, ref data))
                {
                    __loadingThread = new Thread(() => InitSyncPart2(data));
                    __loadingThread.Start();
                }
            }
        }

        /// <summary>
        /// 等待异步加载完成
        /// </summary>
        public IEnumerator WaitForAsyncInit(Action<LoadStatus> callback)
        {
            if (_inited)
            {
                callback(Status);
                yield break;
            }

            yield return 0;
            if (__loadingThread == null)
                throw new Exception("使用者程序编写错误：没有开始后台加载。");

            __loadingThread.Join();

            callback(Status);
            yield return 0;
        }

        public LoadStatus ForceReSync(string tag)
        {
            _inited = false;
            Status = LoadStatus.NONE;
            return this.InitSync(tag);
        }

        private bool InitSyncPart1(string tag, ref byte[] data)
        {
            if (_inited || Status != LoadStatus.NONE)
            {
                HSUtils.LogWarning("ResourceManager已经初始化完成，不能重入。");
                return false;
            }

            Status = LoadStatus.WORKING;

            CurrentTag = V2Mode ? tag : null;
            data = V2Mode ? DoLoadDataV2(tag) : DoLoadData();
            if (data == null)
            {
                HSUtils.LogError("加载数据出错：空数据。");
                Status = LoadStatus.NULL_DATA;
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 同步阻塞加载。
        /// </summary>
        public LoadStatus InitSync(string tag)
        {
            byte[] data = null;
            if (!InitSyncPart1(tag, ref data))
            {
                return Status;
            }
            return InitSyncPart2(data);
        }

        private LoadStatus InitSyncPart2(byte[] data)
        {
            data = HSPackToolEx.AutoDeFile(data);
            if (data == null)
            {
                HSUtils.LogError("解压解密数据出错。");
                Status = LoadStatus.UNPACK_ERROR;
                return Status;
            }

            try
            {
                _values = ProtoBufTools.Deserialize<BeanDict>(data);
            }
            catch (Exception e)
            {
                HSUtils.LogException(e);
                Status = LoadStatus.DESERILATION_ERROR;
                return Status;
            }

            try
            {
                int count = 0;
                foreach (var dict in _values)
                {
                    foreach (var item in dict.Value)
                    {
                        item.Value.InitBind();
                        count++;
                    }
                }

                HSUtils.Log("pojos load finished: 总共[{0}]个类，[{1}]个Pojo。[Value{2}]", _values.Count, count, CurrentTag == null ? "" : "_" + CurrentTag);
                _inited = true;
                Status = LoadStatus.OK;
                return Status;
            }
            catch (Exception e)
            {
                HSUtils.LogException(e);
                Status = LoadStatus.INITBIND_ERROR;
                return Status;
            }
        }
        #endregion

        #region 私有
        private bool _inited = false;
        private BeanDict _values = new BeanDict();
        #endregion

        #region 方法
        public string VerboseInfo { get { return "总共[{0}] 个类，[{1}]个Pojo。".f(_values.Count, _values.Values.Select(beans => beans.Count).Sum()); } }
        public bool IsInited() { return _inited; }

        public bool Has<T>(string key) where T : BaseBean
        {
            return Get<T>(key) != null;
        }

        public T Get<T>(string key) where T : BaseBean
        {
            return Get(typeof(T), key) as T;
        }

        public BaseBean Get(string key, string typeName)
        {
            return Get(Type.GetType(typeName), key);
        }

        private BaseBean Get(Type type, string key)
        {
            RunTimeFrozenChecker.CheckIfFrozen("Get", key, type.FullName);
            if (string.IsNullOrEmpty(key)) return null;

            BaseBean bean;
            Dictionary<string, BaseBean> subDict;
            return _values.TryGetValue(type.FullName, out subDict) && subDict.TryGetValue(key, out bean) ? bean : null;
        }


        public IEnumerable<T> GetAll<T>() where T : BaseBean
        {
            RunTimeFrozenChecker.CheckIfFrozen<T>("GetAll");

            Dictionary<string, BaseBean> subDict;
            return _values.TryGetValue(typeof(T).FullName, out subDict) ? subDict.Values.Cast<T>() : Enumerable.Empty<T>();
        }

        //现在禁用
        public T GetRandom<T>() where T : BaseBean
        {
            RunTimeFrozenChecker.CheckIfFrozen<T>("GetRandom");

            string key = typeof(T).FullName;
            return (T)_values[key].Values.ToList()[new System.Random(DateTime.Now.Millisecond).Next(0, _values[key].Count - 1)];
            //有BUG：当没有这个key的时候会空指针错。但是禁用了就不去管了。GG20180202
        }
        #endregion

        #region Untiy编辑模式下专用
        public Action<BeanDict> LoadDesignModeDelegate { get; set; }
        public Action<string, BeanDict> LoadDesignModeDelegateV2 { get; set; }

        /// <summary>
        /// 此方式不甚好，等客户端需要XML更新的时候再一起修改，目前暂且如此。
        /// </summary>
        public void LoadDesignMode()
        {
            if (LoadDesignModeDelegate == null)
                throw new Exception("使用者程序编写错误：ResourceManager.UpdateDelegate没有初始化。");
            LoadDesignModeDelegate(_values);
            _inited = true;
            CurrentTag = null;
        }

        public void LoadDesignModeV2(string tag)
        {
            if (!V2Mode)
            {
                LoadDesignMode();
                return;
            }

            CurrentTag = tag;
            if (LoadDesignModeDelegateV2 == null)
                throw new Exception("使用者程序编写错误：ResourceManager.UpdateDelegate没有初始化。");
            LoadDesignModeDelegateV2(tag, _values);
            _inited = true;
        }

        /// <summary>
        /// 仅仅调试的时候使用
        /// </summary>
        public void VisitValues(Action<BeanDict> visitor)
        {
            visitor(_values);
        }
        #endregion
    }
}
