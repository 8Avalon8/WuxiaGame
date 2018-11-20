using System;
using System.Collections.Generic;
using HSFrameWork.Common;
using System.Collections;
using GLib;
using System.IO;
using System.Xml.Serialization;

namespace HSFrameWork.SPojo.Inner
{
    public abstract partial class AbstractSaveable<ARGT, ATTRT> : Saveable
    {
        protected abstract ATTRT ArgToAttr(ARGT attr);
        protected abstract string ArgToString(ARGT attr);
        protected abstract ATTRT StringToAttr(string attrString);
        protected abstract string AttrToString(ATTRT attr);

        #region 核心数据
        /// <summary>
        /// <para>如果是临时SPojo，则所有成员都存储在m_data["attrName"]里面。</para>
        /// <para>对于持久型SPojo：
        ///       普通struct和string成员，直接存储在m_data["attrName"]里面。
        ///       SPojo成员：其Id存储在m_data["attrName"]里面，实例存储在_pojoAttrs["attrName"]里面，
        ///             还有全局的SPojoShared._pojos[type][id]里面。
        ///       Lists成员：实例存储在 _simpleListAttrs/_pojoListAttrs["attrName"]里面。
        ///             如果是从存档加载的，则其List的Json字串存储在m_data["attrName"]里面；
        ///                 如果在加载后List修改了，则m_data["attrName"]会在Submit的时候被更新。
        ///             如果是客户端新创建而存档中没有的，则没有对一个的m_data["attrName"]。
        ///                 在Submit的时候，会将Json备份在m_data["attrName"]里面。
        /// </para>
        /// </summary>
        protected Dictionary<ATTRT, object> m_data
        {
            get
            {
                if (_m_data == null)
                {
                    _m_data = new Dictionary<ATTRT, object>();
                }
                return _m_data;
            }
        }

        protected Dictionary<ATTRT, object> _m_data;

        /// <summary> 持久型SPojo的所有SPojo类型的成员。 ["attrName"] </summary>
        protected Dictionary<ATTRT, Saveable> pojoAttrs
        {
            get
            {
                if (_pojoAttrs == null)
                    _pojoAttrs = new Dictionary<ATTRT, Saveable>();
                return _pojoAttrs;
            }
        }
        protected Dictionary<ATTRT, Saveable> _pojoAttrs;

        /// <summary> 包含且仅包含持久型SPojo的所有已创建的简单数据List类型的成员。["attrName"] </summary>
        private Dictionary<ATTRT, IList> simpleListAttrs
        {
            get
            {
                if (_simpleListAttrs == null)
                    _simpleListAttrs = new Dictionary<ATTRT, IList>();
                return _simpleListAttrs;
            }
        }
        private Dictionary<ATTRT, IList> _simpleListAttrs;

        /// <summary> 包含且仅包含持久型SPojo的所有已创建的SPojo数据List类型的成员。["attrName"] </summary>
        private Dictionary<ATTRT, IList> pojoListAttrs
        {
            get
            {
                if (_pojoListAttrs == null)
                    _pojoListAttrs = new Dictionary<ATTRT, IList>();
                return _pojoListAttrs;
            }
        }
        private Dictionary<ATTRT, IList> _pojoListAttrs;

        /// <summary> 仅仅持久型SPojo才有用 </summary>
        protected HashSet<ATTRT> changedAttrs
        {
            get
            {
                if (_changedAttrs == null)
                    _changedAttrs = new HashSet<ATTRT>();
                return _changedAttrs;
            }
        }

        private HashSet<ATTRT> _changedAttrs;
        protected bool IsChanged(ATTRT attr)
        {
            return _changedAttrs != null && _changedAttrs.Contains(attr);
        }
        protected void SetChanged(ATTRT attrNameInt)
        {
            if (!IsIgnoreSubmit())
                changedAttrs.Add(attrNameInt);
        }

        /// <summary>
        /// 缓存上次提交的数据。
        /// </summary>
        protected Dictionary<ATTRT, object> _LastSubmitted;
        protected Dictionary<ATTRT, object> LastSubmitted
        {
            get
            {
                if (_LastSubmitted == null) _LastSubmitted = new Dictionary<ATTRT, object>();
                return _LastSubmitted;
            }
        }
        #endregion

        #region 数据操作
        public override void IgnoreSubmit()
        {
            _changedAttrs = null;  //经常会设置些属性，然后才会调用IgnoreSubmit
            base.IgnoreSubmit();
        }
        #endregion

        #region ID

        protected abstract ATTRT ATTR_NAME_ID { get; }
        public override int Id()
        {
            return __id;
        }

        /// <summary>
        /// __id的一个特点就是只在SaveablePojo里面使用，外界看不到。
        /// </summary>
        protected int __id
        {
            get
            {
                if (IsIgnoreSubmit())
                {
                    HSUtils.LogError("程序编写错误，临时SaveablePojo不可以使用__id。{0}", GetType());
                    return 0;
                }
                else
                {
                    if (_m_data != null && m_data.ContainsKey(ATTR_NAME_ID))
                        return (int)_m_data[ATTR_NAME_ID];
                    else
                        return (__id = MaxIDUtils.SafetGetNextID(GetType()));
                }
            }

            //if (_m_data == null || !_m_data.ContainsKey(ATTR_NAME_ID) || ((int)_m_data[ATTR_NAME_ID]) != value)总是无法全面覆盖；
#if HSFRAMEWORK_RUN_IN_MS_CONSOLE
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
            private set
            {
                if (IsIgnoreSubmit())
                {
                    HSUtils.LogError("程序编写错误，临时SaveablePojo不可以使用__id。{0}", GetType());
                }
                else
                {
                    if (_m_data == null || !_m_data.ContainsKey(ATTR_NAME_ID) || ((int)_m_data[ATTR_NAME_ID]) != value)
                    {
                        changedAttrs.Add(ATTR_NAME_ID);
                        m_data[ATTR_NAME_ID] = value;
                        MaxIDUtils.ResetMaxID(GetType(), value);
                        OnIdChanged();
                    }
                }
            }
        }
        #endregion


        protected AbstractSaveable() : this(false)
        {
        }

        protected AbstractSaveable(bool IgnoreSubmit) : base(IgnoreSubmit)
        {
        }

        //因为全覆盖测试的数据来自特定版本的JiangHuX，故此只能作为Console程序独立运行。
        //在实际项目Untiy里面测试会比较麻烦。
        //在这个测试程序编译时设置此宏定义。
#if HSFRAMEWORK_DEV_TEST
        /// <summary>
        /// 仅仅用于内部测试
        /// </summary>
        [XmlIgnore]
        public Dictionary<ATTRT, object> Test_m_data { get { return _m_data; } }
        [XmlIgnore]
        public HashSet<ATTRT> Test_changedAttrs { get { return _changedAttrs; } }
        [XmlIgnore]
        public Dictionary<ATTRT, IList> Test_simpleListAttrs { get { return _simpleListAttrs; } }
        public int Test_simpleListAttrsCount { get { return _simpleListAttrs == null ? 0 : _simpleListAttrs.Count; } }
        [XmlIgnore]
        public Dictionary<ATTRT, IList> Test_pojoListAttrs { get { return _pojoListAttrs; } }
        public int Test_pojoListAttrsCount { get { return _pojoListAttrs == null ? 0 : _pojoListAttrs.Count; } }
        [XmlIgnore]
        public Dictionary<ATTRT, Saveable> Test_pojoAttrs { get { return _pojoAttrs; } }
        public int Test_pojoAttrsCount { get { return _pojoAttrs == null ? 0 : _pojoAttrs.Count; } }

        public void TestSetId(int id)
        {
            __id = id;
        }
#endif
    }
}
