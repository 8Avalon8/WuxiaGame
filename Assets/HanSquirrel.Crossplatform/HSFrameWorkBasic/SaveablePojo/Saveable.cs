#define HSFRAMEWORK_SAVEABLE_TRACK_INSTANCE


namespace HSFrameWork.SPojo
{
    using Inner;
    public abstract partial class Saveable
    {
        /// <summary>
        /// 主键。
        /// </summary>
        virtual public string PK { get { return null; } }

        /// <summary>
        /// 加载游戏时，从服务端同步完数据后会调用InitBind。
        /// </summary>
        virtual public void InitBind() { }

        #region ID&SaveName
        /// <summary>
        /// 清空内容
        /// </summary>
        public virtual void Reset()
        {
            _saveNameGCached = null;
        }

        public const int ATTR_NAME_ID_INT = 0;
        public static string ATTR_NAME_ID_STR = "_id";

        /// <summary>
        /// 取得唯一ID
        /// </summary>
        /// <returns></returns>
        public abstract int Id();

        protected void OnIdChanged()
        {
            _saveNameGCached = null;
        }

        private string _saveNameGCached; //可以在3000个POJO的时候遍历节省大约10~20ms
        /// <summary> 
        /// 当前SaveablePojo的存档KEY，因为RuntimeData预先使用了SaveName这个成员，故此加上尾注。
        /// 会自动生成ID。
        /// </summary>
        public string SaveNameG
        {
            get
            {
                if (__NoSubmit)
                    return null;
                else
                {
                    if (_saveNameGCached == null)
                        _saveNameGCached = SaveNameUtils.GetSaveName(GetType(), Id());
                    return _saveNameGCached;
                }
            }
        }
        #endregion

        #region IgnoreSubmit
        /// <summary>
        /// 是否是本地对象，不与服务器同步。
        /// </summary>
        /// <returns></returns>
        public bool IsIgnoreSubmit()
        {
            return __NoSubmit;
        }

        private bool __NoSubmit = false;

        /// <summary>
        /// 设置该SPojo为纯本地对象，不与服务端同步。
        /// </summary>
        public virtual void IgnoreSubmit()
        {
            if (__NoSubmit)
                return;

            _saveNameGCached = null;
            __NoSubmit = true;

#if HSFRAMEWORK_SAVEABLE_TRACK_INSTANCE
            IgnoreSubmitCalledOne();
#endif
        }
        #endregion

        #region 构造析构
        protected Saveable(bool IgnoreSubmit)
        {
            //RunTimeFrozenChecker.CheckIfFrozen("Saveable.Constructor()");

#if HSFRAMEWORK_SAVEABLE_TRACK_INSTANCE
            AddInstance(IgnoreSubmit);
#endif
            __NoSubmit = IgnoreSubmit;
        }

#if HSFRAMEWORK_SAVEABLE_TRACK_INSTANCE
        ~Saveable()
        {
            SubInstance(__NoSubmit);
        }
#endif
        #endregion
#if HSFRAMEWORK_DEV_TEST
        /// <summary>
        /// 内部测试使用。
        /// </summary>
        public string Test_saveNameGCached { get { return _saveNameGCached; } }
#endif
    }
}