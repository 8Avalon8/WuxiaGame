using System;
using System.Collections.Generic;
using BeanDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, HSFrameWork.ConfigTable.BaseBean>>;
using HSFrameWork.ConfigTable.Trans;

namespace HSFrameWork.ConfigTable
{
    /// <summary>
    /// ConfigTable功能的初始化辅助类。
    /// </summary>
    public interface IInitHelper
    {
        /// <summary>
        /// 是否显示内部执行程序的用时统计。
        /// </summary>
        bool ShowExeTimer { get; }
        /// <summary>
        /// 注册的有XML标签的类，到ProtoBuf
        /// </summary>
        IEnumerable<Type> ProtoBufTypes { get; }

        /// <summary>
        /// 配置表Excel转换为XML后，xml的元素名称和类的对应关系
        /// </summary>
        IEnumerable<KeyValuePair<string, Type>> XMLBeanMaps { get; }

        /// <summary>
        /// 加载完整二进制的配置表文件（values）的函数实现
        /// </summary>
        Func<byte[]> LoadConfigTableData { get; }

        /// <summary>
        /// 加载二进制的配置表文件的函数实现(ValueBundle名称)
        /// </summary>
        Func<string, byte[]> LoadConfigTableDataV2 { get; }

        /// <summary>
        /// 在编辑模式下加载配置表字典的函数实现。
        /// </summary>
        Action<BeanDict> ResourceManagerLoadDesignModeDelegate { get; }

        /// <summary>
        /// 在编辑模式下加载ValueBundle的函数实现(ValueBundle名称)。
        /// </summary>
        Action<string, BeanDict> ResourceManagerLoadDesignModeDelegateV2 { get; }

        /// <summary>
        /// 生成语言包的时候需要的文字导出工具类列表。
        /// </summary>
        List<KeyValuePair<string, ITextFinder>> TextFinders { get; }
    }

    /// <summary>
    /// 缺省的abstract <see cref="IInitHelper">IInitHelper</see>实现。
    /// </summary>
    public abstract class DefaultInitHelper : IInitHelper
    {
        public virtual Func<string, byte[]> LoadConfigTableDataV2
        {
            get { return null; }
        }

        public virtual Func<byte[]> LoadConfigTableData
        {
            get { return null; }
        }

        public virtual IEnumerable<Type> ProtoBufTypes
        {
            get { return null; }
        }

        public virtual Action<BeanDict> ResourceManagerLoadDesignModeDelegate
        {
            get { return null; }
        }

        public virtual Action<string, BeanDict> ResourceManagerLoadDesignModeDelegateV2
        {
            get { return null; }
        }

        public virtual bool ShowExeTimer
        {
            get { return false; }
        }

        public virtual IEnumerable<KeyValuePair<string, Type>> XMLBeanMaps
        {
            get
            {
                if (_nodeTypes == null)
                {
                    _nodeTypes = new List<KeyValuePair<string, Type>>();
                    BuildTypeNodes();
                }

                return _nodeTypes;
            }
        }

        private List<KeyValuePair<string, Type>> _nodeTypes;
        protected void AddTypeNode<T>(string node)
        {
            _nodeTypes.Add(new KeyValuePair<string, Type>(node, typeof(T)));
        }
        protected abstract void BuildTypeNodes();

        /// <summary>
        /// 每次调用都会动态建造；如此不会有很多static数据。
        /// </summary>
        public virtual List<KeyValuePair<string, ITextFinder>> TextFinders
        {
            get
            {
                List<KeyValuePair<string, ITextFinder>> textFinders = new List<KeyValuePair<string, ITextFinder>>();
                BuildTextFinders(textFinders);
                return textFinders;
            }
        }

        protected void AddTextFinder<T>(object arg, string name) where T : ITextFinder, new()
        {
            (arg as List<KeyValuePair<string, ITextFinder>>).Add(new KeyValuePair<string, ITextFinder>(name, new T()));
        }

        protected virtual void BuildTextFinders(object arg) { }
    }
}
