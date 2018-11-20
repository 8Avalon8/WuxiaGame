using C5;
using System.Collections.Generic;
using System;
using System.Collections;

namespace HSFrameWork.Common.Inner
{
    public class FixedCircularQueue<T> : IEnumerable<T>
    {
        private readonly CircularQueue<T> _CircularQueue;
        private readonly int _FixedSize;
        public FixedCircularQueue(int size)
        {
            _CircularQueue = new CircularQueue<T>(size);
            _FixedSize = size;
        }

        public int Count
        {
            get
            {
                return _CircularQueue.Count;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _CircularQueue.GetEnumerator();
        }

        public T Dequeue()
        {
            return _CircularQueue.Dequeue();
        }

        public void Enqueue(T item)
        {
            if (_CircularQueue.Count >= _FixedSize)
                _CircularQueue.Dequeue();
            _CircularQueue.Enqueue(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _CircularQueue.GetEnumerator();
        }
    }
}
