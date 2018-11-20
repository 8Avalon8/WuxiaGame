using System;
using System.Collections;
using System.Collections.Generic;
using BeanDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, HSFrameWork.ConfigTable.BaseBean>>;

namespace HSFrameWork.ConfigTable
{
    using Inner;
    /// <summary>
    /// 配置表工具类终端唯一接口
    /// </summary>
    public class ConfigTable
    {
        /// <summary>
        /// 实际的单例，仅仅内部开发测试使用。
        /// </summary>
        public static readonly ConfigTableBasic Instance = new ConfigTableBasic();

        /// <summary>
        /// 设置加载二进制配置表文件的接口。
        /// </summary>
        public static Func<byte[]> DoLoadData
        {
            set
            {
                Instance.DoLoadData = value;
            }
        }

        /// <summary>
        /// 设置加载二进制配置表文件的接口（ValueBundleName）。
        /// </summary>
        public static Func<string, byte[]> DoLoadDataV2
        {
            set
            {
                Instance.DoLoadDataV2 = value;
            }
        }

        /// <summary>
        /// 异步加载配置表，tag为ValueBundleTag
        /// </summary>
        static public void StartInitAsync(string tag = null)
        {
            Instance.StartInitAsync(tag);
        }

        /// <summary>
        /// 强制重新加载二进制配置表
        /// </summary>
        /// <param name="tag">ValueBundleTag</param>
        static public LoadStatus ForceReSync(string tag = null)
        {
            return Instance.ForceReSync(tag);
        }

        /// <summary>
        /// 等待配置表异步加载完成
        /// </summary>
        static public IEnumerator WaitForAsyncInit(Action<LoadStatus> callback)
        {
            return Instance.WaitForAsyncInit(callback);
        }

        /// <summary>
        /// 同步加载配置表
        /// </summary>
        /// <param name="tag">ValueBundleTag</param>
        /// <returns>返回配置表加载状态</returns>
        static public LoadStatus InitSync(string tag = null)
        {
            return Instance.InitSync(tag);
        }

        /// <summary>
        /// 判断配置表是否初始化完成
        /// </summary>
        static public bool IsInited()
        {
            return Instance.IsInited();
        }

        /// <summary>
        /// 是否存在这个数据
        /// </summary>
        static public bool Has<T>(string key) where T : BaseBean
        {
            return Instance.Has<T>(key);
        }

        /// <summary>
        /// 从配置表中获取此类型对应的id的数据
        /// </summary>
        /// <typeparam name="T">BasePoje类型</typeparam>
        /// <param name="id">配置表中对应的id</param>
        /// <returns>配置表中对应数据的数据类</returns>
        static public T Get<T>(int id) where T : BaseBean
        {
            return Instance.Get<T>(id.ToString());
        }

        /// <summary>
        /// 从配置表中获取此类型对应的key的数据
        /// </summary>
        /// <typeparam name="T">BasePoje类型</typeparam>
        /// <param name="key">配置表中对应的key</param>
        /// <returns>配置表中对应数据的数据类</returns>
        static public T Get<T>(string key) where T : BaseBean
        {
            return Instance.Get<T>(key);
        }

        /// <summary>
        /// 从配置表中获取此类型所有的数据
        /// </summary>
        /// <typeparam name="T">BasePoje类型</typeparam>
        /// <returns>配置表中此类型数据的所有数据类集</returns>
        public static IEnumerable<T> GetAll<T>() where T : BaseBean
        {
            return Instance.GetAll<T>();
        }

        /// <summary>
        /// 随机取得一个类型为 T 的数据
        /// </summary>
        static public T GetRandom<T>() where T : BaseBean
        {
            return Instance.GetRandom<T>();

        }

        /// <summary>
        /// 从配置表中获取对应类型及对应key的数据
        /// 返回BaseBean，需要使用对应类型的变量则需要进行强转
        /// </summary>
        /// <param name="key">配置表中对应的key</param>
        /// <param name="typeName">BasePoje类型 为字符串，需要带上命名空间</param>
        /// <returns>配置表中对应数据的数据类</returns>
        static public BaseBean Get(string key, string typeName)
        {
            return Instance.Get(key, typeName);
        }

        /// <summary>
        /// 设置编辑模式下加载配置表字典的函数
        /// </summary>
        public static Action<BeanDict> LoadDesignModeDelegate { set { Instance.LoadDesignModeDelegate = value; } }
    
        /// <summary>
        /// 设置编辑模式下加载配置表字典的函数（valueBundleTag）
        /// </summary>
        public static Action<string, BeanDict> LoadDesignModeDelegateV2 { set { Instance.LoadDesignModeDelegateV2 = value; } }

        /// <summary>
        /// 在编辑模式下加载配置表字典
        /// </summary>
        public static void LoadDesignMode()
        {
            Instance.LoadDesignMode();
        }

        /// <summary>
        /// 在编辑模式下加载配置表字典（ValueBundleTag）
        /// </summary>
        public static void LoadDesignModeV2(string tag)
        {
            Instance.LoadDesignModeV2(tag);
        }

        /// <summary>
        /// 内部开发使用。访问配置表字典。
        /// </summary>
        public static void VisitValues(Action<BeanDict> visitor)
        {
            Instance.VisitValues(visitor);
        }
    }
}
