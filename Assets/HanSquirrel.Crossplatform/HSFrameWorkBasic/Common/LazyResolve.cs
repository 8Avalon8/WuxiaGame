using System;

namespace HSFrameWork.Common
{
    public class LazyResolve<T>
    {
        private Lazy<T> _Lazy;
        public static LazyResolve<T> Create(Func<T> factory)
        {
            var ret = new LazyResolve<T>();
            ret._Lazy = new Lazy<T>(factory);
            return ret;
        }

        /// <summary>
        /// 使用 Activator.CreateInstance实现
        /// </summary>
        public static LazyResolve<T> Create()
        {
            return Create(() => Activator.CreateInstance<T>());
        }

        /// <summary>
        /// 使用 Container.Resolve 实现
        /// </summary>
        public static LazyResolve<T> CreateInContainer()
        {
            return Create(() => Container.Resolve<T>());
        }

        /// <summary>
        /// 使用 container.Resolve 实现
        /// </summary>
        public static LazyResolve<T> CreateInContainer(IContainer container)
        {
            return Create(() => container.Resolve<T>());
        }

        public T Value
        {
            get
            {
                return _Lazy.Value;
            }
        }
    }
}
