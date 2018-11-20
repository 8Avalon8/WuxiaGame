using HSFrameWork.Common;
using System.Xml.Serialization;

namespace HSFrameWork.ConfigTable
{
    /// <summary>
    /// 配置数据基类
    /// 所有excel表配置的数据都需要继承此类
    /// </summary>
    public abstract class BaseBean
    {
        /// <summary>
        /// 主键。一种类型的PK应该不同。可以为null。
        /// </summary>
        [XmlIgnore]
        abstract public string PK { get; }

        /// <summary>
        /// 初始化绑定
        /// 在调用完xml序列化后会调用本函数
        /// </summary>
        virtual public void InitBind() { }

        /// <summary>
        /// 从XML反序列化出一个新对象并调用InitBind
        /// </summary>
        public static T Create<T>(string xml) where T : BaseBean
        {
            T rst = ToolsShared.DeserializeXML<T>(xml);
            rst.InitBind();
            return rst;
        }

        /// <summary>
        /// 显示名称。
        /// </summary>
        [XmlIgnore]
        public virtual string DisplayName{ get; set; }
    }
}
