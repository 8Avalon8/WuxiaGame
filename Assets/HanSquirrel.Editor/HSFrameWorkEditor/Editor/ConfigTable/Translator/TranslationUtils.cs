using GLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using System.Linq;

namespace HSFrameWork.ConfigTable.Editor.Trans.Impl
{
    /// <summary>
    /// 通用独立工具类。
    /// </summary>
    public static class Utils
    {
        public static void RemoveEmptyEnds(List<string> strs)
        {
            for (int i = strs.Count - 1; i >= 0; i--)
            {
                if (strs[i].Visible())
                    break;
                strs.RemoveAt(i);
            }
        }

        public static string[] UniformG(this IEnumerable<string> strs)
        {
            return strs.Select(s => s.Trim()).Distinct().Where(s => s.Visible()).ToArray();
        }

        private static Regex _textFinderDefRgx = new Regex(@"^([\w_]+)(.*)$", RegexOptions.IgnoreCase);
        /// <summary>
        /// 单词后面的全部是ARG
        /// </summary>
        public static void ProcessTextFinderDefXML(string xml, out string finder, out string arg)
        {
            if (!xml.Visible())
            {
                finder = arg = null;
                return;
            }

            Match match = _textFinderDefRgx.Match(xml);
            if (match.Success)
            {
                finder = match.Groups[1].Value;
                arg = match.Groups[2].Value;
            }
            else
            {
                finder = arg = null;
            }
        }
    }

    public class XMLMemberInfo
    {
        public Dictionary<string, MemberInfo> Attributes = new Dictionary<string, MemberInfo>();
        public Dictionary<string, MemberInfo> Elements = new Dictionary<string, MemberInfo>();
        public Dictionary<string, MemberInfo> Arrays = new Dictionary<string, MemberInfo>();

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public bool TryGetValue(string attrName, string subBeanName, out MemberInfo mi)
        {
            mi = null;
            if (subBeanName == null)
            {
                return attrName == null ? false : Attributes.TryGetValue(attrName, out mi);
            }
            else if (Elements.TryGetValue(subBeanName, out mi))
                return true;
            else
            {
                return attrName == null ? false : Arrays.TryGetValue(attrName, out mi);
            }
        }
    }

    public static class ReflectionExt
    {
        public static XMLMemberInfo GetXMLMembers(this Type type)
        {
            XMLMemberInfo info = new XMLMemberInfo();
            type.FindMembers(MemberTypes.Field | MemberTypes.Property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                (mi, obj) =>
            {
                {

                    XmlAttributeAttribute[] attrs = mi.GetCustomAttributes(typeof(XmlAttributeAttribute), true) as XmlAttributeAttribute[];
                    if (attrs.Length > 0)
                    {
                        string attrName = attrs[0].AttributeName.Visible() ? attrs[0].AttributeName : mi.Name;
                        info.Attributes.Add(attrName, mi);
                        return false;
                    }
                }

                {
                    XmlElementAttribute[] elements = mi.GetCustomAttributes(typeof(XmlElementAttribute), true) as XmlElementAttribute[];
                    if (elements.Length > 0)
                    {
                        string attrName = elements[0].ElementName.Visible() ? elements[0].ElementName : mi.Name;
                        info.Elements.Add(attrName, mi);
                        return false;
                    }
                }

                {
                    XmlArrayAttribute[] arrays = mi.GetCustomAttributes(typeof(XmlArrayAttribute), true) as XmlArrayAttribute[];
                    if (arrays.Length > 0)
                    {
                        string attrName = arrays[0].ElementName.Visible() ? arrays[0].ElementName : mi.Name;
                        info.Arrays.Add(attrName, mi);
                        return false;
                    }
                }

                return false;
            }, null);

            return info;
        }


        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public static object GetFieldOrPropertyValue(this MemberInfo mi, object obj)
        {
            switch (mi.MemberType)
            {
                case MemberTypes.Field:
                    return (mi as FieldInfo).GetValue(obj);
                case MemberTypes.Property:
                    return (mi as PropertyInfo).GetValue(obj, null);
                default:
                    return null;
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public static void SetFieldOrPropertyValue(this MemberInfo mi, object obj, object v)
        {
            switch (mi.MemberType)
            {
                case MemberTypes.Field:
                    (mi as FieldInfo).SetValue(obj, v);
                    break;
                case MemberTypes.Property:
                    (mi as PropertyInfo).SetValue(obj, v, null);
                    break;
                default:
                    throw new Exception("SetFieldOrPropertyValue：程序编写错误：无效调用");
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public static Type GetFieldOrPropertyType(this MemberInfo mi)
        {
            switch (mi.MemberType)
            {
                case MemberTypes.Field:
                    return (mi as FieldInfo).FieldType;
                case MemberTypes.Property:
                    return (mi as PropertyInfo).PropertyType;
                default:
                    throw new Exception("George程序编写错误");
            }
        }
    }
}