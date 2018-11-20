// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;

namespace System.Buffers
{
    internal sealed partial class DefaultArrayPool<T> : ArrayPool<T>
    {
        /// <summary>Provides a thread-safe bucket containing buffers that can be Rent'd and Return'd.</summary>
        private sealed class Bucket
        {
            internal readonly int _bufferLength;
            private readonly T[][] _buffers;
            private readonly int _poolId;

#if HSFRAMEWORK_NET_ABOVE_4_5
            private SpinLock _lock; // do not make this readonly; it's a mutable struct
#else
            private object _LockObj;
#endif
            private int _index;
            private ArrayPool<T> _Context;
            /// <summary>
            /// Creates the pool with numberOfBuffers arrays where each buffer is of bufferLength length.
            /// </summary>
            internal Bucket(int bufferLength, int numberOfBuffers, int poolId, ArrayPool<T> context)
            {
#if HSFRAMEWORK_NET_ABOVE_4_5
                _lock = new SpinLock(Debugger.IsAttached); // only enable thread tracking if debugger is attached; it adds non-trivial overheads to Enter/Exit
#else
                _LockObj = new object();
#endif
                Id = GetHashCode();
                _buffers = new T[numberOfBuffers][];
                _bufferLength = bufferLength;
                _poolId = poolId;
                _Context = context;
            }

            /// <summary>Gets an ID for the bucket to use with events.</summary>
            internal int Id;

            /// <summary>Takes an array from the bucket.  If the bucket is empty, returns null.</summary>
            internal T[] Rent()
            {
                T[][] buffers = _buffers;
                T[] buffer = null;

                // While holding the lock, grab whatever is at the next available index and
                // update the index.  We do as little work as possible while holding the spin
                // lock to minimize contention with other threads.  The try/finally is
                // necessary to properly handle thread aborts on platforms which have them.
                bool lockTaken = false, allocateBuffer = false;
                try
                {
#if HSFRAMEWORK_NET_ABOVE_4_5
                    _lock.Enter(ref lockTaken);
#else
                    Monitor.Enter(_LockObj);
#endif
                    if (_index < buffers.Length)
                    {
                        buffer = buffers[_index];
                        buffers[_index++] = null;
                        allocateBuffer = buffer == null;
                    }
                }
                finally
                {
#if HSFRAMEWORK_NET_ABOVE_4_5
                    if (lockTaken) _lock.Exit(false);
#else
                    Monitor.Exit(_LockObj);
#endif
                }

                // While we were holding the lock, we grabbed whatever was at the next available index, if
                // there was one.  If we tried and if we got back null, that means we hadn't yet allocated
                // for that slot, in which case we should do so now.
                if (allocateBuffer)
                {
                    buffer = new T[_bufferLength];

                    Interlocked.Increment(ref _Context.AllocatedSum);
                    Interlocked.Increment(ref _Context.AllocatedGreen);
                    Interlocked.Add(ref _Context.AllocatedBufSize, _bufferLength);
#if HSFRAMEWORK_RUN_IN_MS_CONSOLE
                    var log = ArrayPoolEventSource.Log;
                    if (log.IsEnabled())
                    {
                        log.BufferAllocated(buffer.GetHashCode(), _bufferLength, _poolId, Id,
                            ArrayPoolEventSource.BufferAllocatedReason.Pooled);
                    }
#endif
                }

                return buffer;
            }

            /// <summary>
            /// Attempts to return the buffer to the bucket.  If successful, the buffer will be stored
            /// in the bucket and true will be returned; otherwise, the buffer won't be stored, and false
            /// will be returned.
            /// </summary>
            internal void Return(T[] array)
            {
                // Check to see if the buffer is the correct size for this bucket
                if (array.Length != _bufferLength)
                {
                    throw new ArgumentException("SR.ArgumentException_BufferNotFromPool", "array");
                }

                // While holding the spin lock, if there's room available in the bucket,
                // put the buffer into the next available slot.  Otherwise, we just drop it.
                // The try/finally is necessary to properly handle thread aborts on platforms
                // which have them.
                bool lockTaken = false;
                try
                {
#if HSFRAMEWORK_NET_ABOVE_4_5
                    _lock.Enter(ref lockTaken);
#else
                    Monitor.Enter(_LockObj);
#endif
                    if (_index != 0)
                    {
                        _buffers[--_index] = array;
                    }
                }
                finally
                {
#if HSFRAMEWORK_NET_ABOVE_4_5
                    if (lockTaken) _lock.Exit(false);
#else
                    Monitor.Exit(_LockObj);
#endif
                }
            }
        }
    }
}
