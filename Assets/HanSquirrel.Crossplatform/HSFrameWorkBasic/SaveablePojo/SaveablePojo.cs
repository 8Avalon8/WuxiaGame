using System.Collections.Generic;
using HSFrameWork.Common;

namespace HSFrameWork.SPojo
{
    using Inner;

    /// <summary>
    /// 可以在服务端存档的Pojo的基类。如需使用则派生。
    /// </summary>
    public abstract partial class SaveablePojo : AbstractSaveable<string, int>
    {
        protected override int ArgToAttr(string attr)
        {
            return Name2ID(attr, true);
        }

        protected override string ArgToString(string arg)
        {
            return arg;
        }

        protected override string AttrToString(int attr)
        {
            return _GlobalAttrNames[attr];
        }

        protected override int StringToAttr(string attrString)
        {
            return Name2ID(attrString, true);
        }

        #region ID
        private const int MAX_ATTR_NAME_COUNT = 8 * 1024;
        private static Dictionary<string, int> _GlobalAttrNameDict = new Dictionary<string, int>();
        protected static string[] _GlobalAttrNames = new string[MAX_ATTR_NAME_COUNT];
        private static int _GlobalAttrNameCount = 0;

        //public static string[] Test_GlobalAttrNames { get { return _GlobalAttrNames; } }
        //public static Dictionary<string, int> Test_GlobalAttrNameDict { get { return _GlobalAttrNameDict; } }

        /// <summary>
        /// 开发者内部使用
        /// </summary>
        public static IEnumerable<string> GlobalAttrNames()
        {
            for (int i = 0; i < SaveablePojo._GlobalAttrNameCount; i++)
            {
                yield return _GlobalAttrNames[i];
            }
        }

        protected override int ATTR_NAME_ID { get { return ATTR_NAME_ID_INT; } }

        /// <summary>
        /// 开发者内部使用
        /// </summary>
        public static int Name2ID(string attrName)
        {
            return Name2ID(attrName, true);
        }

        //正常情况下无法覆盖超出的情况。
#if HSFRAMEWORK_RUN_IN_MS_CONSOLE
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
        /// <summary>
        /// 开发者内部使用
        /// </summary>
        public static int Name2ID(string attrName, bool autoCreateID)
        {
            int id;
            if (_GlobalAttrNameDict.TryGetValue(attrName, out id))
            {
                return id;
            }
            else if (!autoCreateID)
            {
                return -1;
            }
            else
            {
                if (_GlobalAttrNameCount >= MAX_ATTR_NAME_COUNT)
                {
                    HSUtils.LogError("太多属性超过预期，请自行修改MAX_ATTR_NAME_COUNT。请确认项目适合使用本框架？");
                    return -1;
                }

                _GlobalAttrNameDict[attrName] = _GlobalAttrNameCount;
                _GlobalAttrNames[_GlobalAttrNameCount] = attrName;
                return _GlobalAttrNameCount++;
            }
        }

        private class Initer
        {
            public Initer()
            {
                Name2ID(ATTR_NAME_ID_STR);
            }
        }
        private static Initer _Initer = new Initer();
        #endregion

        protected SaveablePojo() : this(false) { }
        protected SaveablePojo(bool IgnoreSubmit) : base(IgnoreSubmit)
        {
        }
    }
}
