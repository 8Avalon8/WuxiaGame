using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using GLib;
using HSFrameWork.Common;
using Hanjiasongshu;
using System.Text;

namespace HSFrameWork.SPojo
{
    using HSFrameWork.SPojo.Inner;

    public abstract partial class Saveable
    {
        protected abstract void TryObscureData();

        protected abstract void UpdateFromSave(Hashtable newDict);

        protected abstract void CreateSaveableMemberFromMData(Func<Type, int, StreamWriter, int, Saveable> GetOrCreateOtherPojo, StreamWriter swDebug, int level, ICollection<string> warningInfo);

        /// <summary>
        /// 仅仅在从存档中加载的时候才会调用
        /// </summary>
        private static Saveable CreateNoInitBind(int id, Type type, Hashtable data, Action<Saveable> Oncreated, Func<Type, int, StreamWriter, int, Saveable> GetOrCreateOtherPojo, StreamWriter swDebug, int level, ICollection<string> warningInfo)
        {
            if (!data.ContainsKey(ATTR_NAME_ID_STR))
            {
                if (warningInfo != null)
                    warningInfo.Add("[{0}:{1}] 的数据里面没有ID {2}.".Eat(type.FullName, id, ATTR_NAME_ID_STR));
                return null;
            }

            if (id != Convert.ToInt32(data[ATTR_NAME_ID_STR]))
            {
                if (warningInfo != null)
                    warningInfo.Add("[{0}:{1}] 中的ID和自身不等。".Eat(type.FullName, id));
                return null;
            }
            data[ATTR_NAME_ID_STR] = id;
            MaxIDUtils.ResetMaxID(type, id);

            Saveable pojo;
            pojo = Activator.CreateInstance(type) as Saveable;
            pojo.UpdateFromSave(data); //仅仅更新 m_data
            Oncreated(pojo);
            pojo.CreateSaveableMemberFromMData(GetOrCreateOtherPojo, swDebug, level, warningInfo); //最重要的一步。会递归创建所有引用的POJO
            pojo.TryObscureData();
            return pojo;
        }
    }

    namespace Inner
    {
        public abstract partial class AbstractSaveable<ARGT, ATTRT> : Saveable
        {
            #region 反射缓存
            protected enum AttrTypeEnum
            {
                SIMPLE,
                POJO,
                SIMPLE_LIST,
                STRING_LIST,
                POJO_LIST
            }

            protected class AttrTypeEx
            {
                public bool isNumber;
                public Type type;
                public Type listItemType;
                public AttrTypeEnum typeEnum;
                public AttrTypeEx(Type t, AttrTypeEnum te, Type gt, bool isNumber)
                {
                    type = t;
                    typeEnum = te;
                    listItemType = gt;
                    this.isNumber = isNumber;
                }
            }

            /// <summary>
            /// 用于缓存反射结果。
            /// </summary>
            private static Dictionary<Type, Dictionary<ATTRT, AttrTypeEx>> _attrTypeDict = new Dictionary<Type, Dictionary<ATTRT, AttrTypeEx>>();

            protected Dictionary<ATTRT, AttrTypeEx> SafeGetAttrTypeDict()
            {
                Type type = GetType();
                if (!_attrTypeDict.ContainsKey(type))
                {
                    List<string> wi = new List<string>();
                    BuildAttrTypeDictIfNone(type, wi);
                    if (wi.Count != 0)
                        foreach (var s in wi)
                            HSUtils.LogError(s);
                }
                return _attrTypeDict[type];
            }

            private Dictionary<ATTRT, AttrTypeEx> SafeGetAttrTypeDict(ICollection<string> warningInfo)
            {
                BuildAttrTypeDictIfNone(GetType(), warningInfo);
                return _attrTypeDict[GetType()];
            }

            private void BuildAttrTypeDictIfNone(Type type, ICollection<string> warningInfo)
            {
                if (_attrTypeDict.ContainsKey(type))
                    return;

                Dictionary<ATTRT, AttrTypeEx> attrTypes = _attrTypeDict.GetOrAdd(type);

                foreach (var propertyInfo in type.GetProperties())
                {
                    var attrName = propertyInfo.Name;
                    ATTRT attr = StringToAttr(attrName);
                    var propertyType = propertyInfo.PropertyType;

                    if (attrTypes.ContainsKey(attr))
                    {   //这种情况下，会引发BUG
                        if (warningInfo != null)
                            warningInfo.Add("程序编写错误：抱歉，目前SPOJO底层不支持有两个相同名字的属性。{0}.{1}".Eat(type.FullName, attrName));
                        continue;
                    }

                    if (IsNumberType(propertyType))
                    {   // 这个属性是普通数值                  这个属性是string
                        attrTypes[attr] = new AttrTypeEx(propertyType, AttrTypeEnum.SIMPLE, null, true);
                    }
                    else if (propertyType == typeof(string))
                    {
                        attrTypes[attr] = new AttrTypeEx(propertyType, AttrTypeEnum.SIMPLE, null, false);
                    }
                    else if (propertyType.IsEnum)
                    {   //注意：这里会将enum变为int。saveable唯一判断enum的地方。使用SPojo存储Enum的属性其实现必须使用Get<int>/Set<int>函数。
                        attrTypes[attr] = new AttrTypeEx(typeof(int), AttrTypeEnum.SIMPLE, null, true);
                    }
                    else if (propertyType.IsSubclassOf(typeof(Saveable)))
                    {   //这个属性是 Pojo
                        attrTypes[attr] = new AttrTypeEx(propertyType, AttrTypeEnum.POJO, null, false);
                    }
                    else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        Type genericArg = propertyType.GetGenericArguments()[0];
                        if (genericArg.IsValueType)
                            attrTypes[attr] = new AttrTypeEx(propertyType, AttrTypeEnum.SIMPLE_LIST, genericArg, false);
                        else if (genericArg == typeof(string))
                            attrTypes[attr] = new AttrTypeEx(propertyType, AttrTypeEnum.STRING_LIST, genericArg, false);
                        else if (genericArg.IsSubclassOf(typeof(SaveablePojo)))
                            attrTypes[attr] = new AttrTypeEx(propertyType, AttrTypeEnum.POJO_LIST, genericArg, false);
                        //其他类型的，我们不支持，也不用去理会。派生类有自由使用任何东西。
                    }
                }
            }
            #endregion

            #region m_data更新
            private bool RemoveIfHave(IDictionary old, IDictionary newOne)
            {
                if (old != null)
                {
                    foreach (ATTRT attr in new ArrayList(old.Keys)) //创建一个新的ArrayList是因为在遍历中需要删除元素
                        if (newOne.Contains(AttrToString(attr)))
                            old.Remove(attr);

                    return old.Count == 0;
                }
                else
                {
                    return true;
                }
            }

            protected override void TryObscureData()
            {
                var attrDict = SafeGetAttrTypeDict();
                AttrTypeEx attrTypeEx;
                foreach (ATTRT attr in new ArrayList(_m_data.Keys))
                {
                    if (!attr.Equals(ATTR_NAME_ID_INT) && attrDict.TryGetValue(attr, out attrTypeEx))
                    {
                        if (attrTypeEx.isNumber)
                            _m_data[attr] = ValueUtils.CreateNumber(attrTypeEx.type, _m_data[attr]);
                        else if (attrTypeEx.typeEnum == AttrTypeEnum.SIMPLE)//string
                        {
                            _m_data[attr] = ValueUtils.Create<string>(_m_data[attr] as string);
                        }
                    }
                }
            }

            protected void RemoveChangedAttrsIfHave(IDictionary newOne)
            {
                if (_changedAttrs == null)
                    return;

                foreach (ATTRT attrInt in _changedAttrs.ToArrayG())
                    if (newOne.Contains(AttrToString(attrInt)))
                        _changedAttrs.Remove(attrInt);

                if (_changedAttrs.Count == 0)
                    _changedAttrs = null;
            }

            /// <summary>
            /// 将存档的内容覆盖到当前。智能判断。
            /// </summary>
            protected override void UpdateFromSave(Hashtable newDict)
            {
                //如果存档里面有这个成员，则从_changedAttrs、_simpleListAttrs、_pojoListAttrs、_pojoAttrs里面删除掉。
                RemoveChangedAttrsIfHave(newDict);

                if (RemoveIfHave(_simpleListAttrs, newDict))
                    _simpleListAttrs = null;

                if (RemoveIfHave(_pojoListAttrs, newDict))
                    _pojoListAttrs = null;

                if (RemoveIfHave(_pojoAttrs, newDict))
                    _pojoAttrs = null;

                //覆盖当前m_data
                foreach (string attrString in newDict.Keys)
                    m_data[StringToAttr(attrString)] = newDict[attrString];
            }
            #endregion

            #region CreateList
            private static object ConvertItem2Number(Type type, object item, ICollection<string> warningInfo)
            {
                try
                {
                    if (type == typeof(int))
                        return Convert.ToInt32(item);
                    else if (type == typeof(long))
                        return Convert.ToInt64(item);
                    else if (type == typeof(float))
                        return Convert.ToSingle(item);
                    else
                        return Convert.ToDouble(item);
                }
                catch { }

                if (warningInfo != null)
                    warningInfo.Add("无法将数据 {0} 转化为 {1}".Eat(item, type.FullName));
                return null;
            }

            /// <summary>
            /// false表示没有生成
            /// </summary>
            private bool CreateListInner(IDictionary attrDict, Type type, ATTRT attr, Func<object, object> aTFactory, ICollection<string> warningInfo)
            {
                //m_data[attrName]是从存档加载的jsonlist
                var listContent = m_data[attr];
                _m_data.Remove(attr);

                IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type));
                if (!(listContent is string))
                {
                    //理论上不会发生这个情况，但是因为m_data的使用过于随意，因此还是小心为上。
                    if (warningInfo != null)
                        warningInfo.Add("程序编写错误：{0}#{1}.GetList<{2}>({3})，数据：[{4}]不是string，没有创建List。"
                                        .Eat(GetType().Name, __id, type.Name, AttrToString(attr), listContent));
                    return false;
                }
                else
                {
                    //可以解析了。
                    string jsonText = listContent as string;
                    ArrayList alist = jsonText.arrayListFromJson();
                    if (alist == null)
                    {   //MiniJson会解析错误时返回null
                        if (warningInfo != null)
                            warningInfo.Add("服务端返回的数据解析错误：{0}#{1}.GetList<{2}>({3})，数据：[{4}]，没有创建List。"
                                        .Eat(GetType().Name, __id, type.Name, AttrToString(attr), jsonText));
                        return false;
                    }
                    else
                    {
                        //解析成功。
                        foreach (var item in alist)
                        {
                            object t = aTFactory(item);
                            if (t == null)
                            {
                                if (warningInfo != null)
                                    warningInfo.Add("加载的List里面有NULL，忽略之。{0}.{1}<{2}>[{3}]".Eat(SaveNameG, AttrToString(attr), type.FullName, item));
                            }
                            else
                                list.Add(t);
                        }
                        LastSubmitted[attr] = jsonText;
                    }
                }

                if (list.Count > 0)
                    attrDict.Add(attr, list);

                return list.Count > 0;
            }
            #endregion

            /// <summary>
            /// 从数据找类型，反射带缓存，实际使用版本。
            /// </summary>
            protected override void CreateSaveableMemberFromMData(Func<Type, int, StreamWriter, int, Saveable> GetOrCreateOtherPojo, StreamWriter swDebug, int level, ICollection<string> warningInfo)
            {
                if (_m_data != null)
                {
                    Dictionary<ATTRT, AttrTypeEx> attrTypes = SafeGetAttrTypeDict(warningInfo);
                    foreach (var attr in new List<ATTRT>(_m_data.Keys))
                    {
                        if (attr.Equals(ATTR_NAME_ID))
                            continue;
                        if (!attrTypes.ContainsKey(attr))
                        {
                            if (warningInfo != null)
                                warningInfo.Add("程序编写错误或者数据错误：[{0}] ：在类定义里面找不到属性 [{1}]。".Eat(SaveNameG, AttrToString(attr)));
                            _m_data.Remove(attr);
                            continue;
                        }

                        ProcessOneAttr(attr, attrTypes[attr], GetOrCreateOtherPojo, swDebug, level, warningInfo);
                    }
                }
            }

            protected void ProcessOneAttr(ATTRT attr, AttrTypeEx attrType, Func<Type, int, StreamWriter, int, Saveable> GetOrCreateOtherPojo, StreamWriter swDebug, int level, ICollection<string> warningInfo)
            {
                switch (attrType.typeEnum)
                {
                    case AttrTypeEnum.POJO:
                        if (swDebug != null)
                            swDebug.WriteLine(new string('\t', level + 1) + AttrToString(attr));
                        int pojoId = Convert.ToInt32(_m_data[attr]);
                        _m_data.Remove(attr);
                        var pojo = GetOrCreateOtherPojo(attrType.type, pojoId, swDebug, level + 2);
                        if (pojo == null)
                        {
                            if (warningInfo != null)
                                warningInfo.Add("[{0}].{1} = {2}#{3}，存档找不到这个POJO".Eat(SaveNameG, AttrToString(attr), attrType.type, pojoId));
                        }
                        else
                        {
                            LastSubmitted[attr] = pojoId;
                            pojoAttrs[attr] = pojo;
                        }
                        return;
                    case AttrTypeEnum.SIMPLE_LIST:
                        CreateListInner(simpleListAttrs, attrType.listItemType, attr, item => ConvertItem2Number(attrType.listItemType, item, warningInfo), warningInfo);
                        return;
                    case AttrTypeEnum.STRING_LIST:
                        CreateListInner(simpleListAttrs, attrType.listItemType, attr, item => item.ToString(), warningInfo);
                        return;
                    case AttrTypeEnum.POJO_LIST:
                        if (swDebug != null) swDebug.WriteLine(new string('\t', level + 1) + AttrToString(attr) + "[]");
                        CreateListInner(pojoListAttrs, attrType.listItemType, attr, item => GetOrCreateOtherPojo(attrType.listItemType, Convert.ToInt32(item), swDebug, level + 2), warningInfo);
                        return;
                    default:
                        return;
                }
            }
        }
    }

    public partial class SaveableBeanDictionary<T> : AbstractSaveableDictionary<T> where T : Saveable, new()
    {
        protected override void TryObscureData() { }
        protected override void CreateSaveableMemberFromMData(Func<Type, int, StreamWriter, int, Saveable> GetOrCreateOtherPojo, StreamWriter swDebug, int level, ICollection<string> warningInfo)
        {
            if (_m_data != null)
            {
                foreach (string attr in new List<string>(_m_data.Keys))
                {
                    if (attr.Equals(ATTR_NAME_ID_STR))
                        continue;

                    ProcessOneAttr(attr, new AttrTypeEx(typeof(T), AttrTypeEnum.POJO, null, false), GetOrCreateOtherPojo, swDebug, level, warningInfo);
                }
            }
        }
    }

    public partial class SaveableStrDictionary : AbstractSaveableDictionary<string>
    {
        protected override void TryObscureData()
        {
            foreach (string attr in new ArrayList(_m_data.Keys))
            {
                if (attr.Equals(ATTR_NAME_ID_STR))
                    continue;
                _m_data[attr] = ValueUtils.Create<string>(_m_data[attr] as string);
            }
        }

        protected override void CreateSaveableMemberFromMData(Func<Type, int, StreamWriter, int, Saveable> GetOrCreateOtherPojo,
            StreamWriter swDebug, int level, ICollection<string> warningInfo)
        { }
    }

    public partial class SaveableNumberDictionary<T> : AbstractSaveableDictionary<T> where T : struct
    {
        protected override void CreateSaveableMemberFromMData(Func<Type, int, StreamWriter, int, Saveable> GetOrCreateOtherPojo, StreamWriter swDebug, int level, ICollection<string> warningInfo)
        {
        }

        protected override void TryObscureData()
        {
            Type attrType = typeof(T);
            //if (!IsNumberType(attrType)) return; //因为在构造函数总已经判断过了。

            foreach (string attr in new ArrayList(_m_data.Keys))
            {
                if (attr.Equals(ATTR_NAME_ID_STR))
                    continue;
                _m_data[attr] = ValueUtils.CreateNumber(attrType, _m_data[attr]);
            }
        }
    }
}