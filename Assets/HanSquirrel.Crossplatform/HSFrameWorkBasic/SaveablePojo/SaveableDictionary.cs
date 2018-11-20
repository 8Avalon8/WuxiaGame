using System;
using System.Collections;
using System.Collections.Generic;
using HSFrameWork.Common;

namespace HSFrameWork.SPojo
{
    using Inner;

    /// <summary>
    /// 可以在服务端存档的 SavePojo字典（string-SavePojo）。
    /// </summary>
    public partial class SaveableBeanDictionary<T> : AbstractSaveableDictionary<T> where T : Saveable, new()
    {
        /// <summary>
        /// 取得attr对应的数据项
        /// </summary>
        public override T Get(string attr)
        {
            return GetPojo<T>(attr);
        }

        public override T this[string key]
        {
            get
            {
                return GetPojo<T>(key);
            }

            set
            {
                SavePojo(key, value);
            }
        }

        /// <summary>
        /// 设置attr对应的数据项
        /// </summary>
        public override void Add(string key, T value)
        {
            SavePojo(key, value);
        }

        /// <summary>
        /// 删除attr对应的数据项
        /// </summary>
        public override bool Remove(string key)
        {
            SavePojo(key, null);
            return true;
        }
    }

    /// <summary>
    /// 可以在服务端存档的字符串字典(string-string)
    /// </summary>
    public partial class SaveableStrDictionary : AbstractSaveableDictionary<string>
    {
        public override string Get(string attr)
        {
            return GetStringBasic(attr);
        }

        public override string this[string key]
        {
            get
            {
                return GetStringBasic(key);
            }

            set
            {
                SaveStringBasic(key, value);
            }
        }

        public override void Add(string key, string value)
        {
            SaveStringBasic(key, value);
        }

        public override bool Remove(string key)
        {
            SaveStringBasic(key, null);
            return true;
        }
    }

    /// <summary>
    /// 可以在服务端存档的数据字典（string-int/long/float/double)
    /// </summary>
    public partial class SaveableNumberDictionary<T> : AbstractSaveableDictionary<T> where T : struct
    {
        public override T Get(string attr)
        {
            return GetSimple<T>(attr);
        }

        public SaveableNumberDictionary()
        {
            if (!IsNumberType(typeof(T)))
                HSUtils.LogError("程序编写错误：SaveableDictionary<{0}>：不支持的类型。", typeof(T).FullName);
        }

        public override T this[string key]
        {
            get
            {
                return GetSimple<T>(key);
            }

            set
            {
                SaveSimple<T>(key, value);
            }
        }

        public override void Add(string key, T value)
        {
            SaveSimple<T>(key, value);
        }

        public override bool Remove(string key)
        {
            RemoveAttr<T>(key);
            return true;
        }
    }

    namespace Inner
    {
        public abstract partial class SaveableDictionary : AbstractSaveable<string, string>
        {
            protected override string ArgToAttr(string attr)
            {
                return attr;
            }

#if HSFRAMEWORK_RUN_IN_MS_CONSOLE
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
            protected override string ArgToString(string arg)
            {
                return arg;
            }

            protected override string AttrToString(string attr)
            {
                return attr;
            }

            protected override string StringToAttr(string attrString)
            {
                return attrString;
            }

            protected override string ATTR_NAME_ID { get { return ATTR_NAME_ID_STR; } }

            public int Count
            {
                get
                {
                    int c = 0;
                    if (_m_data != null)
                    {
                        c = _m_data.Count;
                        if (_m_data.ContainsKey(ATTR_NAME_ID_STR))
                            c--;
                    }

                    if (_pojoAttrs != null)
                    {
                        c += _pojoAttrs.Count;
                    }
                    return c;
                }
            }

            //MS对复杂的逻辑判断总是认为没有完全覆盖，因此忽略代码覆盖测试
#if HSFRAMEWORK_RUN_IN_MS_CONSOLE
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
            public bool ContainsKey(string attr)
            {
                return (_m_data != null && _m_data.ContainsKey(attr) && _m_data[attr] != null) ||
                     (_pojoAttrs != null && _pojoAttrs.ContainsKey(attr) && _pojoAttrs[attr] != null);
            }
        }

        public abstract class AbstractSaveableDictionary<T> : SaveableDictionary, IEnumerable<KeyValuePair<string, T>>
        {
            public abstract T this[string key] { get; set; }
            public abstract T Get(string attr);
            public abstract void Add(string attr, T v);
            public abstract bool Remove(string key);
            public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
            {
                if (_m_data != null)
                {
                    foreach (var kv in _m_data)
                    {
                        if (kv.Key.Equals(ATTR_NAME_ID_STR))
                            continue;
                        yield return new KeyValuePair<string, T>(kv.Key, Get(kv.Key));
                    }
                }

                if (_pojoAttrs != null)
                {
                    foreach (var kv in _pojoAttrs)
                    {
                        yield return new KeyValuePair<string, T>(kv.Key, Get(kv.Key));
                    }
                }
            }

#if HSFRAMEWORK_RUN_IN_MS_CONSOLE
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }
}


