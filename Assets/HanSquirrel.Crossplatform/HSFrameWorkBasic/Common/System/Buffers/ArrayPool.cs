// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using HSFrameWork.Common;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

namespace System.Buffers
{
    /// <summary>
    /// Provides a resource pool that enables reusing instances of type <see cref="T:T[]"/>. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// Renting and returning buffers with an <see cref="ArrayPool{T}"/> can increase performance
    /// in situations where arrays are created and destroyed frequently, resulting in significant
    /// memory pressure on the garbage collector.
    /// </para>
    /// <para>
    /// This class is thread-safe.  All members may be used by multiple threads concurrently.
    /// </para>
    /// </remarks>
    public abstract class ArrayPool<T>
    {
        public volatile int RentedSum, ReturnedSum, AllocatedSum, AllocatedGreen, AllocatedBufSize;
        public string DebugString { get {
                return string.Format("PoolSize=[{0}] All=[{1}/{2}] Out=[{3}] In=[{4}]", 
                    AllocatedBufSize, AllocatedGreen, AllocatedSum, RentedSum, ReturnedSum); } }

        //public void ClearSum() { RentedSum = ReturnedSum = AllocatedSum = 0; }

        /// <summary>The lazily-initialized shared pool instance.</summary>
        private static ArrayPool<T> s_sharedInstance = null;

        /// <summary>
        /// Retrieves a shared <see cref="ArrayPool{T}"/> instance.
        /// </summary>
        /// <remarks>
        /// The shared pool provides a default implementation of <see cref="ArrayPool{T}"/>
        /// that's intended for general applicability.  It maintains arrays of multiple sizes, and 
        /// may hand back a larger array than was actually requested, but will never hand back a smaller 
        /// array than was requested. Renting a buffer from it with <see cref="Rent"/> will result in an 
        /// existing buffer being taken from the pool if an appropriate buffer is available or in a new 
        /// buffer being allocated if one is not available.
        /// </remarks>
#if HSFRAMEWORK_NET_ABOVE_4_5
        public static ArrayPool<T> Shared
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Volatile.Read(ref s_sharedInstance) ?? EnsureSharedCreated(); }
        }

        /// <summary>Ensures that <see cref="s_sharedInstance"/> has been initialized to a pool and returns it.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ArrayPool<T> EnsureSharedCreated()
        {
            Interlocked.CompareExchange(ref s_sharedInstance, Create(), null);
            return s_sharedInstance;
        }
#else
        private static object _lockObj = new object();
        public static ArrayPool<T> Shared
        {
            get
            {
                if (s_sharedInstance != null)
                    return s_sharedInstance;
                lock (_lockObj)
                {
                    if (s_sharedInstance == null)
                        s_sharedInstance = Create();
                    return s_sharedInstance;
                }
            }
        }
#endif

        /// <summary>
        /// Creates a new <see cref="ArrayPool{T}"/> instance using default configuration options.
        /// </summary>
        /// <returns>A new <see cref="ArrayPool{T}"/> instance.</returns>
        public static ArrayPool<T> Create()
        {
            return new DefaultArrayPool<T>();
        }

        /// <summary>
        /// Creates a new <see cref="ArrayPool{T}"/> instance using custom configuration options.
        /// </summary>
        /// <param name="maxArrayLength">The maximum length of array instances that may be stored in the pool.</param>
        /// <param name="maxArraysPerBucket">
        /// The maximum number of array instances that may be stored in each bucket in the pool.  The pool
        /// groups arrays of similar lengths into buckets for faster access.
        /// </param>
        /// <returns>A new <see cref="ArrayPool{T}"/> instance with the specified configuration options.</returns>
        /// <remarks>
        /// The created pool will group arrays into buckets, with no more than <paramref name="maxArraysPerBucket"/>
        /// in each bucket and with those arrays not exceeding <paramref name="maxArrayLength"/> in length.
        /// </remarks>
        public static ArrayPool<T> Create(int maxArrayLength, int maxArraysPerBucket)
        {
            return new DefaultArrayPool<T>(maxArrayLength, maxArraysPerBucket);
        }

        public static ArrayPool<T> System()
        {
            return new SysArrayPool();
        }

        internal sealed class SysArrayPool : ArrayPool<T>
        {
            public override T[] Rent(int minimumLength)
            {
                return new T[minimumLength];
            }

            public override void Return(T[] array, bool clearArray = false)
            {
            }
        }


        /// <summary>
        /// Retrieves a buffer that is at least the requested length.
        /// </summary>
        /// <param name="minimumLength">The minimum length of the array needed.</param>
        /// <returns>
        /// An <see cref="T:T[]"/> that is at least <paramref name="minimumLength"/> in length.
        /// </returns>
        /// <remarks>
        /// This buffer is loaned to the caller and should be returned to the same pool via 
        /// <see cref="Return"/> so that it may be reused in subsequent usage of <see cref="Rent"/>.  
        /// It is not a fatal error to not return a rented buffer, but failure to do so may lead to 
        /// decreased application performance, as the pool may need to create a new buffer to replace
        /// the one lost.
        /// </remarks>
        public abstract T[] Rent(int minimumLength);

        /// <summary>
        /// Returns to the pool an array that was previously obtained via <see cref="Rent"/> on the same 
        /// <see cref="ArrayPool{T}"/> instance.
        /// </summary>
        /// <param name="array">
        /// The buffer previously obtained from <see cref="Rent"/> to return to the pool.
        /// </param>
        /// <param name="clearArray">
        /// If <c>true</c> and if the pool will store the buffer to enable subsequent reuse, <see cref="Return"/>
        /// will clear <paramref name="array"/> of its contents so that a subsequent consumer via <see cref="Rent"/> 
        /// will not see the previous consumer's content.  If <c>false</c> or if the pool will release the buffer,
        /// the array's contents are left unchanged.
        /// </param>
        /// <remarks>
        /// Once a buffer has been returned to the pool, the caller gives up all ownership of the buffer 
        /// and must not use it. The reference returned from a given call to <see cref="Rent"/> must only be
        /// returned via <see cref="Return"/> once.  The default <see cref="ArrayPool{T}"/>
        /// may hold onto the returned buffer in order to rent it again, or it may release the returned buffer
        /// if it's determined that the pool already has enough buffers stored.
        /// </remarks>
        public abstract void Return(T[] array, bool clearArray = false);

        protected static IHSLogger _Logger = HSLogManager.GetLogger("ArrayPool");
        protected static ObjectIDGenerator _IDG = new ObjectIDGenerator();
        protected static long GetId(T[] array)
        {
            bool firstTime;
            return _IDG.GetId(array, out firstTime);
        }
    }
}
