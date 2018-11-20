using System;
using System.Collections;
using System.Collections.Generic;
using HSFrameWork.Common;
using System.Text;
using GLib;

namespace HSFrameWork.SPojo
{
    using Inner;
    namespace Inner
    {
        public abstract partial class AbstractSaveable<ARGT, ATTRT> : Saveable
        {
            protected bool InvalidStructTypeOrName<T>(ARGT arg)
            {
                ATTRT attr = ArgToAttr(arg);
                var type = typeof(T);
                if (!IsNumberType(type))
                {
                    HSUtils.LogError("程序编写错误：{0}.Get<{1}>({2})：不支持的类型。", GetType().FullName, type.FullName, AttrToString(attr));
                    return true;
                }

                if (attr.Equals(ATTR_NAME_ID))
                {
                    HSUtils.LogError("程序编写错误：{0}.Get<{1}>({2})：不可以使用系统内置属性 {2}。", GetType().FullName, type.FullName, ATTR_NAME_ID_STR);
                    return true;
                }
                return false;
            }

#if HSFRAMEWORK_DEV_TEST
            public void Test_RemoveAttr<T>(ARGT arg) where T : struct
            {
                if (typeof(T).IsEnum)
                    RemoveAttr<int>(arg);
                else
                    RemoveAttr<T>(arg);
            }
#endif

            protected void RemoveAttr<T>(ARGT arg) where T : struct
            {
                if (InvalidStructTypeOrName<T>(arg))
                    return;

                ATTRT attr = ArgToAttr(arg);
                if (_m_data != null && _m_data.ContainsKey(attr))
                {
                    _m_data.Remove(attr);
                    if (_m_data.Count == 0)
                        _m_data = null;

                    SetChanged(attr);
                }
            }

            protected void SaveSimple<T>(ARGT arg, T value) where T : struct
            {
                if (InvalidStructTypeOrName<T>(arg))
                    return;

                ATTRT attr = ArgToAttr(arg);

                object oldValueBox = null;

                if (_m_data != null && m_data.TryGetValue(attr, out oldValueBox) && Mini.SmartEquals(value, GetSimple<T>(arg)))
                {
                    value = default(T);
                    return;
                }

                //因为前面判断过参数正确性，因此此处不需要判断返回值是否为null
                var ob = ValueUtils.Create<T>(value, oldValueBox);
                if (ob != oldValueBox)
                    m_data[attr] = ob;

                if (Traced(attr))
                    HSUtils.Log(ValueUtils.SafeDump(ob, arg.ToString()));

                SetChanged(attr);
                IntSetCount++;
                value = default(T);
            }

            protected T GetSimple<T>(ARGT arg) where T : struct
            {
                return GetSimple(arg, default(T));
            }

            protected T GetSimple<T>(ARGT arg, T defaultValue) where T : struct
            {
                Type type = typeof(T);
                if (!IsNumberType(type))
                {
                    HSUtils.LogError("程序编写错误：{0}.Get<{1}>({2})：不支持的类型。", GetType().FullName, type.FullName, ArgToString(arg));
                    return defaultValue;
                }

                ATTRT attr = ArgToAttr(arg);
                if (attr.Equals(ATTR_NAME_ID))
                {
                    HSUtils.LogError("程序编写错误：{0}.Get<{1}>({2})：不可以使用系统内置属性 {2}。", GetType().FullName, type.FullName, ATTR_NAME_ID_STR);
                    return defaultValue;
                }

                if (_m_data == null || !_m_data.ContainsKey(attr))
                {
                    SaveSimple(arg, defaultValue);
                    return defaultValue;
                }
                else
                {
                    IntGetCount++;
                    return ValueUtils.Get<T>(_m_data[attr]);
                }
            }

            protected void SaveStringBasic(ARGT arg, string value)
            {
                ATTRT attr = ArgToAttr(arg);
                if (attr.Equals(ATTR_NAME_ID))
                {
                    HSUtils.LogError("程序编写错误：{0}.Save({1},{2})：不可以使用系统内置属性 {1}。", GetType().FullName, ATTR_NAME_ID_STR, value);
                    return;
                }

                StringGetCount++;
                object oldBox = null;
                if (_m_data != null && _m_data.TryGetValue(attr, out oldBox) && (ValueUtils.Get<string>(_m_data[attr]) == value))
                    return;

                if (value != null)
                {
                    StringSetCount++;
                    var ob = ValueUtils.Create<string>(value, oldBox);
                    if (ob != oldBox)
                        m_data[attr] = ob;

                    if (Traced(attr))
                        HSUtils.Log(ValueUtils.SafeDump(ob, arg.ToString()));

                    SetChanged(attr);
                }
                else
                {
                    if (_m_data != null && _m_data.ContainsKey(attr))
                    {
                        _m_data.Remove(attr);
                        if (_m_data.Count == 0)
                            _m_data = null;
                        SetChanged(attr);
                    }
                }
            }

            protected string GetStringBasic(ARGT arg)
            {
                return GetStringBasic(arg, null);
            }

            protected string GetStringBasic(ARGT arg, string defaultValue = null)
            {
                ATTRT attr = ArgToAttr(arg);
                if (attr.Equals(ATTR_NAME_ID))
                {
                    HSUtils.LogError("程序编写错误：{0}.Get({1})：不可以使用系统内置属性 {1}。", GetType().FullName, ATTR_NAME_ID_STR);
                    return defaultValue;
                }

                if (_m_data != null && _m_data.ContainsKey(attr))
                {
                    StringGetCount++;
                    //return m_data[attr] as string;
                    return ValueUtils.Get<string>(m_data[attr]);
                }
                else
                {
                    if (defaultValue == null)
                        return null;
                    //只有当defaultValue不是NULL的时候，才去存储。
                    SaveStringBasic(arg, defaultValue);
                    return defaultValue;
                }
            }

            protected void SavePojo(ARGT arg, Saveable pojo)
            {
                ATTRT attr = ArgToAttr(arg);
                if (attr.Equals(ATTR_NAME_ID))
                {
                    HSUtils.LogError("程序编写错误：{0}.SavePojoBasic({1})：不可以使用系统内置属性 {1}。", GetType().FullName, ATTR_NAME_ID_STR);
                    return;
                }

                if (pojo == null)
                {
                    if (_pojoAttrs != null && _pojoAttrs.ContainsKey(attr))
                    {
                        _pojoAttrs.Remove(attr);
                        SetChanged(attr);
                    }
                    return;
                }

                if (_pojoAttrs == null || !_pojoAttrs.ContainsKey(attr) || _pojoAttrs[attr] != pojo)
                    SetChanged(attr);

                if (IsIgnoreSubmit())
                    pojo.IgnoreSubmit();

                pojoAttrs[attr] = pojo;
            }

            protected T1 GetPojoBasic<T1>(ARGT arg, Func<T1> aFactory) where T1 : Saveable, new()
            {
                ATTRT attr = ArgToAttr(arg);
                if (attr.Equals(ATTR_NAME_ID))
                {
                    HSUtils.LogError("程序编写错误：{0}.GetPojo*<{1}>({2})：不可以使用系统内置属性 {2}。", GetType().FullName, typeof(ATTRT).FullName, ATTR_NAME_ID_STR);
                    return null;
                }

                Saveable s;
                if (_pojoAttrs != null && _pojoAttrs.TryGetValue(attr, out s))
                {
                    return (T1)s; //已经添加过了。
                }
                else
                {
                    //目前没有这个属性
                    T1 defaultValue = aFactory();
                    if (defaultValue != null)
                    {
                        defaultValue.InitBind();
                        SavePojo(arg, defaultValue); //在这里面会去根据.Ignoresubmit去处理
                    }
                    return defaultValue;
                }
            }

            /// <summary>
            /// 如果m_data里面没有这个key，则自动创建一个新的并返回。
            /// </summary>
            protected T GetPojoAutoCreate<T>(ARGT attrName) where T : Saveable, new()
            {
                return GetPojoBasic(attrName, () => new T());
            }

            /// <summary>
            /// 如果m_data里面没有这个key，则返回defaultValue。
            /// </summary>
            protected T GetPojo<T>(ARGT attrName, T defaultValue = null) where T : Saveable, new()
            {
                return GetPojoBasic(attrName, () => defaultValue);
            }

            /// <summary>
            /// SaveablePojo List
            /// </summary>
            protected void SaveList<T>(ARGT arg, List<T> value) where T : Saveable
            {
                if (IsIgnoreSubmit())
                {
                    foreach (var v in value)
                        if (v != null)//不知道使用者是否会有null
                            v.IgnoreSubmit();
                }

                SaveListInner(pojoListAttrs, arg, value);
                if (_pojoListAttrs.Count == 0)
                    _pojoListAttrs = null;
            }

            protected void SaveNumList<T>(ARGT arg, List<T> value) where T : struct
            {
                Type type = typeof(T);
                if (!IsNumberType(type))
                {
                    HSUtils.LogError("程序编写错误：{0}.SaveNumList<{1}>({2},{3})：不支持的类型。", GetType().FullName, type.FullName, ArgToString(arg), value);
                    return;
                }

                SaveListInner(simpleListAttrs, arg, value);
                if (_simpleListAttrs.Count == 0)
                    _simpleListAttrs = null;
            }

            protected void SaveStringList(ARGT arg, List<string> value)
            {
                SaveListInner(simpleListAttrs, arg, value);
                if (_simpleListAttrs.Count == 0)
                    _simpleListAttrs = null;
            }

            private void SaveListInner<T>(IDictionary attrDict, ARGT arg, List<T> list)
            {
                ATTRT attr = ArgToAttr(arg);
                if (attr.Equals(ATTR_NAME_ID))
                {
                    HSUtils.LogError("程序编写错误：{0}.SaveList<{1}>({2})：不可以使用系统内置属性 {2}。", GetType().FullName, typeof(T).FullName, ATTR_NAME_ID_STR);
                    return;
                }

                if (list == null)
                {
                    if (attrDict.Contains(attr))
                    {
                        attrDict.Remove(attr);
                        SetChanged(attr);
                        //20180123 GG 此时只有ChangedAttr里面有这个属性，在Submit的时候会将null提交，服务器就会删除该属性。
                    }
                }
                else
                {
                    attrDict[attr] = list;
                    SetChanged(attr);
                    //List在提交的时候会去比对，是否已经改变。
                }
            }

            /// <summary> 如果没有，就会创建一个新的。</summary>
            protected List<T> GetList<T>(ARGT arg) where T : Saveable, new()
            {
                return GetListInner<T>(pojoListAttrs, arg);
            }

            /// <summary> 如果没有，就会创建一个新的。</summary>
            protected List<string> GetStringList(ARGT arg)
            {
                return GetListInner<string>(simpleListAttrs, arg);
            }

            /// <summary> 如果没有，就会创建一个新的。</summary>
            protected List<T> GetNumList<T>(ARGT arg) where T : struct
            {
                Type type = typeof(T);
                if (!IsNumberType(type))
                {
                    HSUtils.LogError("程序编写错误：{0}.GetNumList<{1}>({2})：不支持的类型。", GetType().FullName, type.FullName, ArgToString(arg));
                    return null;
                }

                return GetListInner<T>(simpleListAttrs, arg);
            }

            /// <summary>
            /// 如果没有，就会创建一个新的。
            /// </summary>
            private List<T> GetListInner<T>(IDictionary attrDict, ARGT arg)
            {
                ATTRT attr = ArgToAttr(arg);
                if (attr.Equals(ATTR_NAME_ID))
                {
                    HSUtils.LogError("程序编写错误：{0}.GetList*<{1}>({2})：不可以使用系统内置属性 {2}。", GetType().FullName, typeof(T).FullName, ATTR_NAME_ID_STR);
                    return null;
                }

                if (!attrDict.Contains(attr))  //这个List还没有创建过。
                    attrDict.Add(attr, new List<T>());

                SetChanged(attr); //只要Get一个List，就会添加到更新属性中。在Submit的时候会自动判断是否更新。
                return attrDict[attr] as List<T>;
            }

            //MS对复杂的逻辑判断总是认为没有完全覆盖，因此忽略代码覆盖测试
#if HSFRAMEWORK_RUN_IN_MS_CONSOLE
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
            public bool HasListOrKey(ATTRT attr)
            {
                return (_pojoListAttrs != null && _pojoListAttrs.ContainsKey(attr))
                    || (_m_data != null && _m_data.ContainsKey(attr))
                    || (_simpleListAttrs != null && _simpleListAttrs.ContainsKey(attr))
                    || (_pojoAttrs != null && _pojoAttrs.ContainsKey(attr));
            }

            /// <summary>
            /// 不去看List，只看普通类型和Pojo类型
            /// </summary>
            public bool Has(ATTRT attr)
            {
                return (_m_data != null && _m_data.ContainsKey(attr))
                    || (_pojoAttrs != null && _pojoAttrs.ContainsKey(attr));
            }
        }
    }

    public abstract partial class SaveablePojo : AbstractSaveable<string, int>
    {
        #region 写
        /// <summary>
        /// 保存一个 int/long/float/double（自动加扰）。
        /// </summary>
        protected void Save<T>(string attrName, T value) where T : struct
        {
            SaveSimple(attrName, value);
            value = default(T);
        }

        /// <summary>
        /// 保存一个字符串，如果value为null，此属性会从存档删除。
        /// </summary>
        protected void Save(string attrName, string value)
        {
            SaveStringBasic(attrName, value);
        }
        #endregion

        /// <summary>
        /// 是否存在这个属性。不去看List，只看普通类型和Pojo类型
        /// </summary>
        public bool Has(string attr)
        {
            int attrNameInt = Name2ID(attr, false);
            if (attrNameInt == -1)
                return false;

            return base.Has(attrNameInt);
        }

        /// <summary>
        /// 是否存在这个属性（普通属性或者List）。
        /// </summary>
        public bool HasListOrKey(string attrName)
        {
            int attrNameInt = Name2ID(attrName, false);
            if (attrNameInt == -1)
                return false;
            return base.HasListOrKey(attrNameInt);
        }

        /// <summary>
        /// 如果没有这个KEY，则会将defaultValue加入内部字典，并返回这个defaultValue；
        /// </summary>
        protected T Get<T>(string attrName, T defaultValue) where T : struct
        {
            return base.GetSimple(attrName, defaultValue);
        }

        /// <summary>
        /// 如果实例尚且没有这个成员，则会返回一个default(T)，并加入内部字典
        /// </summary>
        protected T Get<T>(string attrName) where T : struct
        {
            return GetSimple<T>(attrName);
        }

        /// <summary>
        /// 如果实例尚且没有这个成员，则会返回defaultValue。如果defaultValue不是null，则会加入内部字典。
        /// </summary>
        protected string Get(string attrName, string defaultValue = null)
        {
            return GetStringBasic(attrName, defaultValue);
        }


    }
}
