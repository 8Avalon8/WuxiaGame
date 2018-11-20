using System;
using System.Threading;

namespace HSFrameWork.Common
{
    namespace Inner
    {
        public abstract class BootOnly<T> where T : BootOnly<T>
        {
            private static int _Inited = 0;

            protected static bool Booted
            {
                get
                {
                    return Interlocked.CompareExchange(ref _Inited, 1, 0) == 1;
                }
            }
        }

    }

    /// <summary>
    /// 多线程安全，T必须可以new()
    /// </summary>
    public class HSSingleton<T> where T : new()
    {
        private static T ms_instance;
        private static object _LockObj = new object();
        public static T Instance
        {
            get
            {
                if (ms_instance != null)
                    return ms_instance;

                lock (_LockObj)
                {
                    if (ms_instance == null)
                        ms_instance = new T();
                    return ms_instance;
                }
            }
        }
    }

    /// <summary>
    /// 多线程安全，T必须可以new()
    /// </summary>
    public class SingletonI<T, TAs> where T : TAs, new()
    {   //GG 20180908
        private static TAs _Instance;
        private static object _LockObj = new object();
        public static TAs Instance
        {
            get
            {
                if (_Instance != null)
                    return _Instance;

                lock (_LockObj)
                {
                    if (_Instance == null)
                        _Instance = new T();
                    return _Instance;
                }
            }
        }
    }
}
