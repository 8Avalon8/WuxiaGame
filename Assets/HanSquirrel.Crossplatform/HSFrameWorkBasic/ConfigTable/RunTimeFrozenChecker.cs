using System;
using GLib;
using System.Threading;
using System.Collections.Generic;
using HSFrameWork.Common;

namespace HSFrameWork.ConfigTable
{
    /// <summary>
    /// 跨平台。在打包阶段的Pojo不可以调用任何ResourceManger和RunTimeData的函数。如果调用可能会引起不可预料的错误。
    /// 因此会在相关运行时的函数处检查，以防止BUG发生。如果抛出此异常，说明程序编写错误了。
    /// </summary>
    public static class RunTimeFrozenChecker
    {
        /// <summary>
        /// 在冻结模式下，ResourceManager和RunTimeData会被禁用。
        /// </summary>
        public static bool Frozen { get { return _frozenCount != 0; } }
        private static volatile int _frozenCount;
        private static Stack<string> _reasons = new Stack<string>();

        public static IDisposable TempFrozen(string reason)
        {
            return DisposeHelper.Create(()=>FrozenOne(reason), WarmOne);
        } 

        public static void FrozenOne(string reason)
        {
            Interlocked.Increment(ref _frozenCount);
            lock (_reasons)
                _reasons.Push(reason);
            HSUtils.Log("▦▦RunTimeFrozenChecker.FrozenOne({0}) [{1}]。".EatWithTID(reason, _frozenCount));
        }

        public static void WarmOne()
        {
            if (_frozenCount == 0)
            {
                throw new Exception("程序编写错误：RunTimeFrozenChecker.WarmOne和Frozen不匹配。");
            }
            Interlocked.Decrement(ref _frozenCount);
            string reason;
            lock (_reasons)
                reason = _reasons.Pop();
            HSUtils.Log("▦▦RunTimeFrozenChecker.WarmOne({0}) [{1}]。".EatWithTID(reason, _frozenCount));
        }

        public static void CheckIfFrozen(string methodName)
        {
            if (Frozen)
                throw new Exception("程序编写错误：运行时数据被临时冻结 {0}()。".EatWithTID(methodName));
        }

        public static void CheckIfFrozen<T>(string methodName, int id)
        {
            if (Frozen)
                throw new Exception("程序编写错误：运行时数据被临时冻结 {0}<{1}<({2})。".EatWithTID(methodName, typeof(T).FullName, id));
        }
        public static void CheckIfFrozen(string methodName, string key, string typeName)
        {
            if (Frozen) throw new Exception("程序编写错误：运行时数据被临时冻结 {0}({1}, {2})。".EatWithTID(methodName, typeName, key));
        }
        public static void CheckIfFrozen<T>(string methodName, string key)
        {
            if (Frozen) throw new Exception("程序编写错误：运行时数据被临时冻结 {0}<{1}>({2}) 。".EatWithTID(methodName, typeof(T).FullName, key));
        }

        public static void CheckIfFrozen<T>(string methodName)
        {
            if (Frozen) throw new Exception("程序编写错误：运行时数据被临时冻结 {0}<{1}> 函数。".EatWithTID(methodName, typeof(T).FullName));
        }

    }
}
