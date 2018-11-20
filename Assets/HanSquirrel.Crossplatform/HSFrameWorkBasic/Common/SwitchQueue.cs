using GLib;
using System;
using System.Buffers;
using System.Collections;
using System.Threading;

namespace HSFrameWork.Common.Inner
{
    public static class SmartBufferExtentions
    {
        public static SmartBuffer CreateSB(this ArrayPool<byte> ap, int size)
        {
            return new SmartBufferEx<bool>(ap, size, true);
        }

        public static SmartBuffer CreateSB(this ArrayPool<byte> ap, byte[] data, int offset, int size)
        {
            var sb = new SmartBufferEx<bool>(ap, size, true);
            Array.Copy(data, offset, sb.Data, 0, data.Length);
            return sb;
        }

        public static SmartBufferEx<T> CreateSBEx<T>(this ArrayPool<byte> ap, int size, T state)
        {
            return new SmartBufferEx<T>(ap, size, state);
        }

        public static SmartBufferEx<T> CreateSBEx<T>(this ArrayPool<byte> ap, byte[] data, int offset, int size, T state)
        {
            var sb = new SmartBufferEx<T>(ap, size, state);
            Array.Copy(data, offset, sb.Data, 0, size);
            return sb;
        }

        public static SmartBuffer CreateSB(this ArrayPool<byte> ap, int headSize, byte[] data)
        {
            var sb = new SmartBufferEx<bool>(ap, headSize + data.Length, true);
            Array.Copy(data, 0, sb.Data, headSize, data.Length);
            return sb;
        }

        public static SmartBuffer CreateSB(this ArrayPool<byte> ap, int headSize, byte[] data, int offset, int size)
        {
            var sb = new SmartBufferEx<bool>(ap, headSize + size, true);
            Array.Copy(data, offset, sb.Data, headSize, size);
            return sb;
        }

        public static SmartBufferEx<T> CreateSBEx<T>(this ArrayPool<byte> ap, int headSize, byte[] data, int offset, int size, T state)
        {
            var sb = new SmartBufferEx<T>(ap, headSize + size, state);
            Array.Copy(data, offset, sb.Data, headSize, size);
            return sb;
        }

        public static SmartBufferEx<T> CreateSBEx<T>(this ArrayPool<byte> ap, byte[] data, T state)
        {
            var sb = new SmartBufferEx<T>(ap, data.Length, state);
            Array.Copy(data, sb.Data, data.Length);
            return sb;
        }
    }

    /// <summary>
    /// 击鼓传花式：最后一个使用的地方负责销毁。
    /// </summary>
    public abstract class SmartBuffer : IDisposable
    {
        protected ArrayPool<byte> _arrayPool;
        public byte[] Data { get; protected set; }
        public int Offset { get; set; }
        public int Size { get; set; }

        public abstract void Dispose();

        public byte[] GetCommonBytes()
        {
            byte[] ret = new byte[Size];
            Array.Copy(Data, Offset, ret, 0, Size);
            return ret;
        }
    }

    public class SmartBufferEx<T> : SmartBuffer
    {
        public T State { get; private set; }

        public SmartBufferEx(T state)
        {
            State = state;
        }

        public SmartBufferEx(byte[] buffer, T state)
        {
            Data = buffer;
            State = state;
            Offset = 0;
            Size = buffer.Length;
        }

        public SmartBufferEx(ArrayPool<byte> arrayPool, int size, T state)
        {
            _arrayPool = arrayPool;
            State = state;
            Offset = 0;
            Size = size;
            Data = _arrayPool.Rent(size);
        }

        public override void Dispose()
        {
            if (_arrayPool != null)
                _arrayPool.Return(Data);
        }
    }
    /// <summary>
    /// 简洁高效，必须看明白代码再使用！！！否则后果自负。
    /// </summary>
    public class SwitchQueue<T> where T : class
    {
        public int Count { get { return _Count; } }
        private volatile int _Count;

#if HSFRAMEWORK_NET_ABOVE_4_5
        public AsyncAutoResetEvent EnqueueEvent { get; private set; }
#endif

        private Queue mConsumeQueue;
        private Queue mProduceQueue;

        public SwitchQueue(int capcity, bool useEvent)
        {
            mConsumeQueue = new Queue(capcity);
            mProduceQueue = new Queue(capcity);
#if HSFRAMEWORK_NET_ABOVE_4_5
            if (useEvent)
                EnqueueEvent = new AsyncAutoResetEvent();
#endif
        }


        public SwitchQueue() : this(16, true)
        {
        }

        public SwitchQueue(int capcity) : this(capcity, true)
        {
        }

        /// <summary>
        /// forceAsync为false会大概率引发正在等待的Task直接在调用线程执行。
        /// </summary>
        public void Enqueue(T obj, bool forceAsync)
        {
            Interlocked.Increment(ref _Count);
            lock (mProduceQueue)
            {
                mProduceQueue.Enqueue(obj);
            }
#if HSFRAMEWORK_NET_ABOVE_4_5
            if (EnqueueEvent != null)
                EnqueueEvent.Set(forceAsync);
#endif
        }

        // consumer.Not Thread Safe
        public T Dequeue()
        {
            Interlocked.Decrement(ref _Count);
            return (T)mConsumeQueue.Dequeue();
        }

        public bool Empty()
        {
            return 0 == mConsumeQueue.Count;
        }

        public void Switch()
        {
            lock (mProduceQueue)
            {
                Swap(ref mConsumeQueue, ref mProduceQueue);
            }
        }

        public static void Swap<QT>(ref QT t1, ref QT t2)
        {
            QT temp = t1;
            t1 = t2;
            t2 = temp;
        }

        public void Clear()
        {
            lock (mProduceQueue)
            {
                mConsumeQueue.Clear();
                mProduceQueue.Clear();
                _Count = 0;
            }
        }
    }
}