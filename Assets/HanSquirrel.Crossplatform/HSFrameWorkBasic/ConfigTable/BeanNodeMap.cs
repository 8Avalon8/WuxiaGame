using GLib;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace HSFrameWork.ConfigTable.Inner
{
    /// <summary>
    /// 存储Bean和xmlnode的对应关系；必须在应用层调用BIND来初始化。
    /// </summary>
    public static class BeanNodeMap
    {
        private static OrderedDictionary _Type2Node = new OrderedDictionary();
        private static OrderedDictionary _Node2Type = new OrderedDictionary();

        /// <summary>
        /// 所有注册的类型
        /// </summary>
        public static IEnumerable<Type> Types { get { foreach (var t in _Type2Node.Keys) yield return t as Type; } }

        private static bool Has(Type type)
        {
            return _Type2Node.Contains(type);
        }

        private static bool Has(string nodeName)
        {
            return _Node2Type.Contains(nodeName.ToLower());
        }

        /// <summary>
        /// 如果该type有对应的XMLName则设置并返回true。否则设置为null并返回false。
        /// </summary>
        public static bool TryGet(Type type, out string xmlName)
        {
            if (!Has(type))
            {
                xmlName = null;
                return false;
            }
            else
            {
                xmlName = _Type2Node[type] as string;
                return true;
            }
        }

        /// <summary>
        /// 如果该type有对应的XMLName返回。
        /// 如果不存在则抛出异常。
        /// </summary>
        public static string Get(Type type)
        {
            if (!Has(type))
            {
                throw new KeyNotFoundException("请找程序员：在BeanNodeMap里面没有定义类型{0}".Eat(type));

            }
            return _Type2Node[type] as string;
        }

        /// <summary>
        /// 返回该nodeName对应的Type
        /// 如果不存在则抛出异常。
        /// </summary>
        public static Type Get(string nodeName)
        {
            if (!Has(nodeName))
            {
                throw new KeyNotFoundException("找不到XMLNode[{0}]对应的C#类：程序员忘记在 [XmlPojoBind.cs] 里面定义，或者策划没有删除无效的REG文件和Excel文件。".Eat(nodeName));
            }
            return _Node2Type[nodeName.ToLower()] as Type;
        }

        /// <summary>
        /// 清空。
        /// </summary>
        public static void Reset()
        {
            ColdBind(null);
        }

        /// <summary>
        /// 初始化.
        /// </summary>
        public static void ColdBind(IEnumerable<KeyValuePair<string, Type>> XMLBeanMaps)
        {
            _Type2Node = new OrderedDictionary();
            _Node2Type = new OrderedDictionary();

            if (XMLBeanMaps == null)
                return;

            foreach (var kv in XMLBeanMaps)
                Bind(kv.Key, kv.Value);
        }

        /// <summary>
        /// 增加一个对应关系。
        /// </summary>
        public static void Bind(string nodeName, Type type)
        {
            nodeName = nodeName.ToLower();
            if (_Type2Node.Contains(type))
            {
                throw new Exception("程序编写错误！重复的Type:{0}:{1}".Eat(type.Name, nodeName));
            }

            if (_Node2Type.Contains(nodeName))
            {
                throw new Exception("程序编写错误！重复的nodeName:{0}:{1}".Eat(nodeName, type.Name));
            }

            _Type2Node.Add(type, nodeName);
            _Node2Type.Add(nodeName, type);
        }
    }
}