using System;
using System.Collections.Generic;
using System.Text;
using GLib;
using System.Linq;
using System.Collections;
using HSFrameWork.Common;

namespace HSFrameWork.SPojo
{
    using Inner;
    namespace Inner
    {
        /// <summary>
        /// 内部开发使用。各种实例个数统计。
        /// </summary>
        public class LifeCount
        {
            public int created;
            public int destoried;
            public int noSubmit;
            public int lived { get { return created - destoried; } }
        }
    }
    public abstract partial class Saveable
    {
        #region 性能跟踪Debug
        /// <summary>
        /// 内部开发使用。整数设置次数。
        /// </summary>
        public static ulong IntSetCount { get; set; }
        /// <summary>
        /// 内部开发使用。整数获取次数。
        /// </summary>
        public static ulong IntGetCount { get; set; }
        /// <summary>
        /// 内部开发使用。字符串设置次数。
        /// </summary>
        public static ulong StringSetCount { get; set; }
        /// <summary>
        /// 内部开发使用。字符串获取次数。
        /// </summary>
        public static ulong StringGetCount { get; set; }

        /// <summary>
        /// 内部开发使用，类型对应的实例个数统计
        /// </summary>
        public static Dictionary<Type, LifeCount> InstanceDict = new Dictionary<Type, LifeCount>();

        /// <summary>
        /// 总共的SPojo实例个数
        /// </summary>
        public static int InstanceCount { get; private set; }
        /// <summary>
        /// 不与服务端同步的纯本地SPojo的实例个数
        /// </summary>
        public static int NoSubmitCount { get; private set; }
        /// <summary>
        /// 与服务端同步的SPojo的实例个数
        /// </summary>
        public static int SubmitCount { get { return InstanceCount - NoSubmitCount; } }

        private void IgnoreSubmitCalledOne()
        {
            lock (InstanceDict)
            {
                InstanceDict[GetType()].noSubmit++;
                NoSubmitCount++;
            }
        }

        private void AddInstance(bool noSubmit)
        {
            lock (InstanceDict)
            {
                Type type = GetType();
                if (!InstanceDict.ContainsKey(type))
                    InstanceDict[type] = new LifeCount { created = 1 };
                else
                    InstanceDict[type].created++;

                InstanceCount++;
                if (noSubmit)
                {
                    InstanceDict[type].noSubmit++;
                    NoSubmitCount++;
                }
            }
        }

        private void SubInstance(bool noSubmit)
        {
            lock (InstanceDict)
            {
                Type type = GetType();
                InstanceDict[type].destoried++;

                InstanceCount--;

                if (noSubmit)
                {
                    NoSubmitCount--;
                    InstanceDict[type].noSubmit--;
                }
            }
        }
        #endregion

        /// <summary>
        /// 取得详细信息。如果重载ToString，则在Debug的时候会痛苦万分，因为编辑器会去自动调用ToString()
        /// </summary>
        public string Dump()
        {
            return DumpInner(null);
        }

        /// <summary>
        /// 将所有内容导出为HashTable
        /// </summary>
        public Hashtable Save()
        {
            Hashtable mainData = new Hashtable();
            StringBuilder sb1 = new StringBuilder();
            Dump(0, true, null, mainData);
            return mainData;
        }

        private string DumpInner(Hashtable mainData)
        {
            StringBuilder sb = new StringBuilder();

            Dump(0, true, sb, mainData);

            return sb.ToString();
        }

        /// <summary>
        /// 取得详细信息。内部开发使用。
        /// </summary>
        public abstract void Dump(int level, bool printHead, StringBuilder sb, Hashtable mainData);
    }


    namespace Inner
    {
        public abstract partial class AbstractSaveable<ARGT, ATTRT> : Saveable
        {
            private HashSet<ATTRT> _tracedAttr;

            private bool Traced(ATTRT attr)
            {
                return _tracedAttr != null && _tracedAttr.Contains(attr);
            }

            public void TraceAttr(ARGT arg)
            {
                if (_tracedAttr == null)
                    _tracedAttr = new HashSet<ATTRT>();
                _tracedAttr.Add(ArgToAttr(arg));
                HSUtils.Log(DumpAttrIfSafe(arg));
            }

            public string DumpAttrIfSafe(ARGT arg)
            {
                object oldValueBox = null;
                ATTRT attr = ArgToAttr(arg);
                if (_m_data != null && m_data.TryGetValue(attr, out oldValueBox))
                {
                    return ValueUtils.SafeDump(oldValueBox, arg.ToString());
                }
                else
                    return arg.ToString() + "不存在";
            }

            public void NoTraceAttr(ARGT arg)
            {
                if (_tracedAttr != null)
                    _tracedAttr.Remove(ArgToAttr(arg));
            }

            public override void Dump(int level, bool printHead, StringBuilder sb, Hashtable mainData)
            {
                if (sb != null)
                {
                    if (printHead)
                    {
                        sb.Append('\t', level);
                        sb.AppendLine(SaveNameG);
                    }

                    if (_changedAttrs != null && _changedAttrs.Count > 0)
                    {
                        sb.Append('\t', level + 1);
                        sb.Append("▲ChangedAttrs: [ ");
                        sb.Append(string.Join(", ", _changedAttrs.Select(attr => AttrToString(attr)).ToArray()));
                        sb.AppendLine(" ]");
                    }
                }


                Hashtable data = null;
                if (!IsIgnoreSubmit() && mainData != null)
                {
                    data = new Hashtable();
                }

                DumpInner(sb, level, mainData, data);

                if (data != null)
                {
                    mainData[SaveNameG] = data;
                    //因为一个Saveable可能会被多个Saveable引用，故此Add会异常。
                    //Dump为String的时候可以重复，这样便于开发者查看。
                }
            }

            private void DumpInner(StringBuilder sb, int level, Hashtable mainData, Hashtable data)
            {
                if (_m_data != null)
                {
                    var attrDict = SafeGetAttrTypeDict();
                    List<string> simpleValues = new List<string>();
                    foreach (ATTRT attr in _m_data.Keys)
                    {
                        if (data != null) data.Add(AttrToString(attr), _m_data[attr].ToString());
                        if (sb != null) simpleValues.Add("{0}{1} : {2}".Eat(IsChanged(attr) ? "★" : "", AttrToString(attr), _m_data[attr]));
                    }

                    if (sb != null && simpleValues.Count > 0)
                    {
                        sb.Append('\t', level + 1);
                        sb.Append("{ ");
                        sb.Append(string.Join(", ", simpleValues.ToArray()).Replace("\n", "$$$NL$$")); //有些字符串里面有回车
                        sb.AppendLine(" }");
                    }
                }

                if (_pojoAttrs != null)
                {
                    foreach (var attr in _pojoAttrs.Keys)
                    {
                        if (sb != null)
                        {
                            sb.Append('\t', level + 1);
                            if (IsChanged(attr)) sb.Append('★');
                            sb.Append(AttrToString(attr));
                            if (_pojoAttrs[attr].SaveNameG != null)
                            {
                                sb.Append(" - ");
                                sb.Append(_pojoAttrs[attr].SaveNameG);
                            }

                            if (_LastSubmitted != null && _LastSubmitted.ContainsKey(attr))
                            {   //存档版本
                                sb.Append(" *");
                                sb.Append(_LastSubmitted[attr].ToString());
                            }
                            sb.AppendLine();
                        }

                        if (data != null) data.Add(AttrToString(attr), _pojoAttrs[attr].Id().ToString());
                        _pojoAttrs[attr].Dump(level + 1, false, sb, mainData);
                    }
                }

                if (_simpleListAttrs != null)
                {
                    foreach (var attr in _simpleListAttrs.Keys)
                    {
                        if (sb != null)
                        {
                            sb.Append('\t', level + 1);
                            if (IsChanged(attr)) sb.Append('★');
                            sb.Append(AttrToString(attr));

                            if (_LastSubmitted != null && _LastSubmitted.ContainsKey(attr))
                            {   //存档版本
                                sb.AppendLine();
                                sb.Append('\t', level + 2);
                                sb.Append("*");
                                sb.AppendLine(_LastSubmitted[attr] as string);
                                sb.Append('\t', level + 2);
                            }
                            else
                            {
                                sb.Append(": ");
                            }


                            List<string> simpleValues = new List<string>();
                            foreach (var item in _simpleListAttrs[attr])
                            {
                                simpleValues.Add(item.ToString());
                            }

                            sb.Append("[ ");
                            sb.Append(string.Join(", ", simpleValues.ToArray()).Replace("\n", "$$$NL$$"));
                            sb.AppendLine(" ]");
                        }

                        if (data != null) data.Add(AttrToString(attr), _simpleListAttrs[attr].toJson());
                    }
                }

                if (_pojoListAttrs != null)
                {
                    foreach (var attr in _pojoListAttrs.Keys)
                    {
                        if (sb != null)
                        {
                            sb.Append('\t', level + 1);
                            if (IsChanged(attr)) sb.Append('★');
                            sb.Append(AttrToString(attr));
                            sb.AppendLine("[]");

                            if (_LastSubmitted != null && _LastSubmitted.ContainsKey(attr))
                            {   //存档版本；
                                sb.Append('\t', level + 2);
                                sb.Append("* ");
                                sb.AppendLine(_LastSubmitted[attr] as string);
                            }
                        }

                        List<int> idList = new List<int>();
                        foreach (SaveablePojo item in _pojoListAttrs[attr])
                        {
                            if (data != null)
                                idList.Add(item.Id());
                            item.Dump(level + 2, true, sb, mainData);
                        }

                        if (data != null)
                        {
                            data.Add(AttrToString(attr), idList.toJson());
                        }
                    }
                }
            }
        }
    }
}
