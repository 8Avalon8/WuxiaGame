using GLib;
using System;
using System.Collections.Generic;
using System.IO;
using HSFrameWork.Common;
using HSFrameWork.XLS2XML;
using System.Reflection;
using System.Collections;
using System.Text;
using System.Linq;
using HSFrameWork.ConfigTable.Trans;
using HSFrameWork.ConfigTable.Inner;

namespace HSFrameWork.ConfigTable.Editor.Trans.Impl
{
    public class TextFactoryProcessException : Exception
    {
        public TextFactoryProcessException(string msg) : base(msg) { }
    }

    public interface TextFactory
    {
        bool ProcessBean(BaseBean bean, ResultArchive ra, ref int transedTextCount, ref int trasTaskCount, Action<string, string[]> dumpBlocks);
    }

    public class TextFactoryBuilder
    {
        public static TextFactory DoWork(IEnumerable<string> regFiles, Func<List<KeyValuePair<string, ITextFinder>>> getTextFinders, Action<string, float> onProgress)
        {
            var list = new List<KeyValuePair<string, ITextFinder>>(getTextFinders())
                .AddG(new KeyValuePair<string, ITextFinder>("Default", new DefaultTextFinder()));
            return new TextFactoryImpl().Init(regFiles, list, onProgress);
        }

        /// <summary>
        /// 工厂-针对不同BeanType的车间-针对不同属性的工作台
        /// </summary>
        private class TextFactoryImpl : TextFinderTeam, TextFactory
        {
#region 设备
            /// <summary>
            /// 属性处理工作台。对应于一个XMLAttribute如 《action type="EFFECTWITHDELAY" value="音效.喝水" /》里面的 type，或者value
            /// </summary>
            class AttrOp
            {
                public List<string> nameSpace = new List<string>(); //这个属性所在的类的XMLNODE，如 story/action
                public string xmlAttrName; //这个属性的XMLAttr的名字
                public ITextFinder finder;
                public string arg;
                public MemberInfo mi;
            }

            /// <summary>
            /// 一个需要处理的类的全XMLNode路径 》所有属性工作台；如 story.action 》{value工作台}
            /// </summary>
            private Dictionary<string, List<AttrOp>> _typeTextListFuncDict = new Dictionary<string, List<AttrOp>>();

            /// <summary>
            /// 一个需要处理的类的全XMLNode路径 》函数列表：{object=>{XMLNode全路径，BeanList},...}
            /// 如：story 》 {
            ///               object=>{"story.action",object.actions}, 
            ///               object=>{"story.femaleAction", object.femalActions}
            ///             }
            /// </summary>
            private Dictionary<string, List<Func<object, KeyValuePair<string, IList>>>> _typeSubBeanListFuncDict = new Dictionary<string, List<Func<object, KeyValuePair<string, IList>>>>();
#endregion

#region 生产
            public bool ProcessBean(BaseBean bean, ResultArchive ra, ref int transedTextCount, ref int trasTaskCount, Action<string, string[]> dumpBlocks)
            {
                string xmlName;
                if (!BeanNodeMap.TryGet(bean.GetType(), out xmlName))
                    return false;

                ProcessBean(xmlName, bean, new List<object>(), ra, ref transedTextCount, ref trasTaskCount, dumpBlocks);
                return true;
            }

            /// <summary>
            /// fullXMLName是bean的XML全路径名称；如story.action
            /// containers是其包含实例递归
            /// </summary>
            private void ProcessBean(string fullXMLName, object bean, List<object> containers, ResultArchive ra, ref int transedTextCount, ref int trasTaskCount, Action<string, string[]> dumpBlocks)
            {
                List<AttrOp> attrOps;
                Mini.Assert("George程序编写错误：ProcessBean.fullXMLName找不到。{0}".Eat(fullXMLName), _typeTextListFuncDict.TryGetValue(fullXMLName, out attrOps));

                foreach (var attrOp in attrOps)
                {
                    //取得实例的这个成员的数值。
                    string str = attrOp.mi.GetFieldOrPropertyValue(bean) as string;
                    if (!str.Visible())
                        continue;

                    //取得对应的小字典的标题
                    string title = GenTransTaskTitle(bean, containers, attrOp.nameSpace, attrOp.xmlAttrName, str.Trim());

                    //拆分，替换，生成翻译任务
                    string newStr;
                    string[] blocks = attrOp.finder.DoWork(bean, attrOp.arg, str, ra.GetDict(title), ref transedTextCount, out newStr);

                    //翻译完成的新字符串
                    if (newStr != null)
                        attrOp.mi.SetFieldOrPropertyValue(bean, newStr);

                    //还需要继续翻译的字符串
                    if (blocks != null && blocks.Length > 0)
                    {
                        trasTaskCount += blocks.Length;
                        dumpBlocks(title, blocks);
                    }
                }

                List<Func<object, KeyValuePair<string, IList>>> subBeanFuncs = _typeSubBeanListFuncDict[fullXMLName];
                containers.Add(bean);
                foreach (var fn in subBeanFuncs)
                {
                    KeyValuePair<string, IList> subBeans = fn(bean);
                    if (subBeans.Value != null)
                        foreach (var b in subBeans.Value)
                            ProcessBean(subBeans.Key, b, containers, ra, ref transedTextCount, ref trasTaskCount, dumpBlocks);
                }
                containers.RemoveAt(containers.Count - 1);
            }
#endregion

#region 建设
            /// <summary>
            /// 工作台：完整属性都需要翻译
            /// </summary>
            private void BuildDesktop4WholeBlock(string fullXMLName, List<string> nameSpace, string attrName, Type beanType, MemberInfo mi)
            {
                BuildDesktopInner(fullXMLName, nameSpace, attrName, beanType, mi, null, GetTextFinder("Default"));
            }

            private void BuildDesktopInner(string fullXMLName, List<string> nameSpace, string attrName, Type beanType, MemberInfo mi, string arg, ITextFinder finder)
            {
                AttrOp op = new AttrOp();
                op.nameSpace = nameSpace;
                op.xmlAttrName = attrName;
                op.mi = mi;
                op.arg = arg;
                op.finder = finder;
                _typeTextListFuncDict[fullXMLName].Add(op);
            }

            /// <summary>
            /// 工作台：属性中的部分字符串需要翻译
            /// </summary>
            private void BuildDesktop4TextFinder(string fullXMLName, List<string> nameSpace, string attrName, Type beanType, MemberInfo mi, string finderXML)
            {
                string finder, arg;
                Utils.ProcessTextFinderDefXML(finderXML, out finder, out arg);

                if (!finder.Visible())
                    throw new TextFactoryProcessException("程序或者REG文件书写错误：在 [{0}]里面找不到TextFinder。".Eat(finderXML));

                BuildDesktopInner(fullXMLName, nameSpace, attrName, beanType, mi, arg, GetTextFinder(finder));
            }

            /// <summary>
            /// 根据beanDef来建造处理该类型bean的车间
            /// </summary>
            private void BuildUnit4BeanType(string fullXMLNameUp, List<string> nameSpaceUp, BeanDef beanDef, Type beanType, FileInfo fi)
            {
                if (beanType == null)
                {   //顶级Bean需要从BeanNodeMap中获得
                    try
                    {
                        beanType = BeanNodeMap.Get(beanDef.Name);
                    }
                    catch (KeyNotFoundException e)
                    {
                        //REG目录下面有过期作废的BeanDef文件，忽略即可
                        HSUtils.LogWarning("[{0}] : {1}".EatWithTID(fi.Name, e.Message));
                        return;
                    }
                }

                string fullXMLName = (fullXMLNameUp.Visible() ? fullXMLNameUp + "." : "") + beanDef.Name.ToLower();

                if (_typeTextListFuncDict.ContainsKey(fullXMLName))
                    return; //REG文件关于BEAN定义有重复；再次忽略。

                HSUtils.Log("{0} 正在加载REG文件 {1} ...", fullXMLName, fi.Name);

                _typeTextListFuncDict.Add(fullXMLName, new List<AttrOp>());
                _typeSubBeanListFuncDict.Add(fullXMLName, new List<Func<object, KeyValuePair<string, IList>>>());

                XMLMemberInfo xmlMembers = beanType.GetXMLMembers();
                List<string> nameSpaceDown = nameSpaceUp == null ? new List<string>() : new List<string>(nameSpaceUp);
                nameSpaceDown.Add(beanDef.Name);

                foreach (var md in beanDef.members)
                {
                    if (!md.NeedTranslate && md.SubBeanDef == null)
                        continue;

                    MemberInfo mi;
                    if (!xmlMembers.TryGetValue(md.Name, md.SubBeanDef == null ? null : md.SubBeanDef.Name, out mi))
                        throw new TextFactoryProcessException("程序或者REG文件书写错误：在类 [{0}] 定义里面找不到 [{1}] 对应的XML属性：[{2}]".Eat(beanType, fi.Name, md.Name));

                    Type memberType = mi.GetFieldOrPropertyType();

                    if (md.SubBeanDef != null)
                    {
                        Type genericArg;
                        if (memberType.IsArray)
                            genericArg = memberType.GetElementType();
                        else if (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(List<>))
                            genericArg = memberType.GetGenericArguments()[0];
                        else
                            throw new TextFactoryProcessException("程序或者REG文件书写错误：在类 [{0}]里面的XML属性：[{1}]不是List<>或者Array".Eat(beanType, md.Name));

                        string subFullXMLName = fullXMLName + "." + md.SubBeanDef.Name.ToLower();
                        _typeSubBeanListFuncDict[fullXMLName].Add(
                            obj => new KeyValuePair<string, IList>(subFullXMLName, mi.GetFieldOrPropertyValue(obj) as IList));

                        BuildUnit4BeanType(fullXMLName, nameSpaceDown, md.SubBeanDef, genericArg, fi);
                    }
                    else //md.NeedTranslate
                    {
                        if (memberType != typeof(string))
                            throw new TextFactoryProcessException("程序或者REG文件书写错误：在类 [{0}]里面的XML属性：[{1}]不是string".Eat(beanType, md.Name));

                        if (md.TextFinder.Visible())
                            BuildDesktop4TextFinder(fullXMLName, nameSpaceDown, md.Name, beanType, mi, md.TextFinder);
                        else
                            BuildDesktop4WholeBlock(fullXMLName, nameSpaceDown, md.Name, beanType, mi);
                    }
                }
            }

            /// <summary>
            /// 建设工厂
            /// </summary>
            public TextFactory Init(IEnumerable<string> regFiles0, IEnumerable<KeyValuePair<string, ITextFinder>> finders, Action<string, float> onProgress)
            {
                foreach (var kv in finders)
                    RegisterTextFinder(kv.Key, kv.Value);

                //文件从新到旧排列。
                var regFiles = regFiles0.Select(f => new FileInfo(f)).OrderByDescending(fi => fi.LastWriteTime).ToArray();
                for (int i = 0; i < regFiles.Length; i++)
                {
                    BuildUnit4BeanType("", null, XMLTool.DeserializeXML<BeanDef>(regFiles[i]), null, regFiles[i]);
                    if (onProgress != null)
                        onProgress(regFiles[i].Name, (1.0f + i) / regFiles.Length);
                }
                return this;
            }
#endregion

            private static string GenTransTaskTitle(object obj, List<object> containers, List<string> nameSpace, string attrName, string orgStr)
            {
                containers.Add(obj);
                Mini.Assert("George程序编写错误: nameSpace.Count != containers.Count", nameSpace.Count == containers.Count);

                StringBuilder sb = new StringBuilder();
                bool first = true;
                for (int j = 0; j < nameSpace.Count; j++)
                {
                    if (!first)
                        sb.Append('.');
                    first = false;

                    sb.Append(nameSpace[j]);
                    BaseBean bb = containers[j] as BaseBean;
                    if (bb != null)
                    {
                        sb.Append('[');
                        sb.Append(bb.PK);
                        sb.Append(']');
                    }
                }
                containers.RemoveAt(containers.Count - 1);

                sb.Append('#');
                sb.Append(attrName);
                sb.Append('#');
                sb.Append(orgStr.MD5G());
                return sb.ToString();
            }
        }
    }
}
