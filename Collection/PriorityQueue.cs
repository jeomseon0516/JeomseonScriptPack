using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Collections
{
    public class PriorityQueue<T> : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>
    {
        private readonly IComparer<T> _comparer;
        private List<T> _heap = new List<T>();
        public int Count { get => _heap.Count; }

        public void Clear()
        {
            _heap.Clear();
        }

        public void Push(T t)
        {
            _heap.Add(t);

            int nowIndex = _heap.Count - 1;

            // 가장 끝 노드에 원소 삽입 후 순차적으로 자신의 위치를 찾아간다.
            while (nowIndex > 0)
            {
                int parentIndex = (nowIndex - 1) / 2;

                if (_comparer.Compare(_heap[nowIndex], _heap[parentIndex]) < 0) // 특정 조건에 만족하는 경우 자신의 위치가 된다.
                    break;

                (_heap[parentIndex], _heap[nowIndex]) = (_heap[nowIndex], _heap[parentIndex]);
                nowIndex = parentIndex;
            }
        }
        public T Pop()
        {
            if (_heap.Count == 0) throw new InvalidOperationException("PriorityQueue is empty");

            // 항상 루트 노드는 특정 조건에 의해 정렬된 값이므로 루트 노드를 반환 해준다.
            T ret = _heap[0];

            int lastIndex = _heap.Count - 1;
            _heap[0] = _heap[lastIndex];
            _heap.RemoveAt(lastIndex--);

            int nowIndex = 0;

            while (true)
            {
                int leftIndex = nowIndex * 2 + 1;
                int rightIndex = nowIndex * 2 + 2;

                int nextIndex = nowIndex;

                if (leftIndex <= lastIndex && _comparer.Compare(_heap[nextIndex], _heap[leftIndex]) < 0)
                    nextIndex = leftIndex;
                if (rightIndex <= lastIndex && _comparer.Compare(_heap[nextIndex], _heap[rightIndex]) < 0)
                    nextIndex = rightIndex;

                if (nowIndex == nextIndex)
                    break;

                T temp = _heap[nowIndex];
                _heap[nowIndex] = _heap[nextIndex];
                _heap[nextIndex] = temp;

                nowIndex = nextIndex;
            }

            return ret;
        }

        public T Peek()
        {
            if (_heap.Count == 0) throw new InvalidOperationException("PriorityQueue is empth");

            return _heap[0];
        }

        public void CopyTo(T[] array, int index)
        {
            _heap.CopyTo(array, index);
        }

        public IEnumerator<T> GetEnumerator() => _heap.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public PriorityQueue(IComparer<T> comparer = null)
        {
            _comparer = comparer ?? Comparer<T>.Default;
        }
    }
}
