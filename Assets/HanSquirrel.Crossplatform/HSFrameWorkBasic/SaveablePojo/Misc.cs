using HSFrameWork.Common;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HSFrameWork.SPojo
{
    namespace Inner
    {
        public static class SaveNameUtils
        {
            public static string GetSaveName(Type t, string key)
            {
                return GetSaveName(t, Convert.ToInt32(key));
            }

            public static string GetSaveName(Type t, int key)
            {
                if (key < 0)
                {
                    HSUtils.LogError("程序编写错误，key [{0}] 不可以小于0。", key);
                }
                return string.Format("{0}:{1}", t, key);
            }

            public static string GetClassName(string saveName)
            {
                TypeIdPair ti = SplitSaveName(saveName);
                return ti == null ? null : ti.type;
            }

            public class TypeIdPair
            {
                public string type;
                public int id;

                //总是覆盖不全。奇怪。
#if HSFRAMEWORK_RUN_IN_MS_CONSOLE
                [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
                public bool EqualsG(TypeIdPair b)
                {
                    return this.type == b.type && this.id == b.id;
                }
            }

            /// <summary> 直接搜索冒号，快。当前使用版本。 </summary>
            public static TypeIdPair SplitSaveName(string saveName)
            {
                int i = saveName.IndexOf(':');
                if (i == -1)
                    return null;
                return new TypeIdPair { type = saveName.Substring(0, i), id = Convert.ToInt32(saveName.Substring(i + 1)) };
            }

            private static Regex _typeRegex = new Regex("(.+):(.+)", RegexOptions.IgnoreCase);
            /// <summary> RegEx的版本比上一个版本要慢，归档不用。 </summary>
            public static TypeIdPair SplitSaveName0(string saveName)
            {
                MatchCollection matches = _typeRegex.Matches(saveName);
                if (matches.Count > 0 && matches[0].Groups.Count > 2)
                    return new TypeIdPair { type = matches[0].Groups[1].Value, id = Convert.ToInt32(matches[0].Groups[2].Value) };
                else
                    return null;
            }
        }
    }

    namespace Inner
    {
        public static class MaxIDUtils
        {
            public static void Reset()
            {
                _maxIdDict.Clear();
            }

            private static Dictionary<Type, int> _maxIdDict = new Dictionary<Type, int>();

            /// <summary>
            /// 确保找到下一个的可用ID，最小可用ID是1。
            /// </summary>
            public static int SafetGetNextID(Type type)
            {
                if (!_maxIdDict.ContainsKey(type))
                    _maxIdDict.Add(type, 0);

                return ++_maxIdDict[type]; //因此，最小可用ID是1
            }

            public static void ResetMaxID(Type type, int id)
            {
                if (!_maxIdDict.ContainsKey(type))
                    _maxIdDict.Add(type, id);
                if (_maxIdDict[type] < id)
                    _maxIdDict[type] = id;
            }
        }
    }

    public abstract partial class Saveable
    {
        /// <summary>
        /// 是否是int/float/long/double
        /// </summary>
        public static bool IsNumberType(Type t)
        {
            return t == typeof(int) || t == typeof(float) || t == typeof(long) || t == typeof(double);
        }
    }
}