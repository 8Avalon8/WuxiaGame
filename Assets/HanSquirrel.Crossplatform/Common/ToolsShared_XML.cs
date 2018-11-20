using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Text;
using GLib;

namespace HSFrameWork.Common
{
    public partial class ToolsShared
    {
        /// <summary>
        /// 将obj序列化为XML
        /// </summary>
        public static string SerializeXML(object obj)
        {
            StringBuilder sb = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(sb, _xmlWriterSettings))
                GetXmlSerializer(obj.GetType()).Serialize(writer, obj, _emptyXmlNamespace);
            return sb.ToString();
        }

        /// <summary>
        /// 将xmlObj反序列化为type
        /// </summary>
        public static object DeserializeXML(Type type, string xmlObj)
        {
            try
            {
                using (StringReader reader = new StringReader(xmlObj))
                {
                    return GetXmlSerializer(type).Deserialize(reader);
                }
            }
            catch (Exception e)
            {
                HSUtils.LogException(e);
                HSUtils.LogError("xml 解析错误:" + xmlObj);
                return null;
            }
        }

        /// <summary>
        /// 将xmlObj反序列化为type
        /// </summary>
        public static T DeserializeXML<T>(string xmlObj)
        {
            return (T)DeserializeXML(typeof(T), xmlObj);
        }

        private static readonly object _Lockobj = new object();
        private static readonly Dictionary<Type, XmlSerializer> _xmlSerializerMap = new Dictionary<Type, XmlSerializer>();
        private static XmlSerializer GetXmlSerializer(Type type)
        {
            lock (_Lockobj)
                return _xmlSerializerMap.GetOrAdd(type, () => new XmlSerializer(type));
        }

        private static XmlWriterSettings _xmlWriterSettings;
        private static XmlSerializerNamespaces _emptyXmlNamespace;
        static ToolsShared()
        {
            _xmlWriterSettings = new XmlWriterSettings();
            _xmlWriterSettings.OmitXmlDeclaration = true;
            _xmlWriterSettings.Encoding = Encoding.UTF8;
            _xmlWriterSettings.NewLineChars = "\n";

            _emptyXmlNamespace = new XmlSerializerNamespaces();
            _emptyXmlNamespace.Add("", "");
        }
    }
}