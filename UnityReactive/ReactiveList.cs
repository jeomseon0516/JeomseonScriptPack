using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;
using Jeomseon.Extensions;
using UnityEngine.AI;

namespace Jeomseon.UnityReactive
{
    [Serializable]
    public class ReactiveList<T>
    {
        public enum RangeEventMode : byte
        {
            /// <summary>
            /// .. OnAddedElement, OnRemovedElement에서 이벤트를 발행합니다.
            /// </summary>
            PER_ELEMENT,
            /// <summary>
            /// .. OnAddedRange, OnRemovedRange에서 이벤트를 발행합니다.
            /// </summary>
            BATCHED
        }

        [Serializable]
        public struct ChangedElementMessage
        {
            public int Index { get; private set; }
            public T PreviousElement { get; private set; }
            public T NewElement { get; private set; }

            public ChangedElementMessage(int index, T previousElement, T newElement)
            {
                Index = index;
                PreviousElement = previousElement;
                NewElement = newElement;
            }
        }

        [SerializeField] private List<T> _list = new();

        public int Capacity
        {
            get => _list.Capacity;
            set => _list.Capacity = value;
        }

        public int Count => _list.Count;

        /// <summary>
        /// .. 요소가 추가될때 이벤트를 발행합니다 RangeEventMode가 PER_ELEMENT일경우 Range 관련 메서드를 호출할 경우 해당 이벤트에서 메세지를 발행합니다. 
        /// BATCHED일 경우에는 Range 메서드를 호출해도 이벤트를 발행하지 않습니다.
        /// </summary>
        [field: SerializeField] public UnityEvent<T> OnAddedElement { get; private set; } = new();
        /// <summary>
        /// .. 요소가 추가될때 이벤트를 발행합니다 RangeEventMode가 PER_ELEMENT일경우 Range 관련 메서드를 호출할 경우 해당 이벤트에서 메세지를 발행합니다.  
        /// BATCHED일 경우에는 Range 메서드를 호출해도 이벤트를 발행하지 않습니다.
        /// </summary>
        [field: SerializeField] public UnityEvent<T> OnRemovedElement { get; private set; } = new();
        /// <summary>
        /// .. 리스트의 내부 요소가 다른 값으로 변경되었을 시 이벤트를 발행합니다. 
        /// </summary>
        [field: SerializeField] public UnityEvent<ChangedElementMessage> OnChangedElement { get; private set; } = new();

        /// <summary>
        /// .. RangeEventMode가 BATCHED일때만 이벤트를 발행합니다.
        /// </summary>
        [field: SerializeField] public UnityEvent<T[]> OnAddedRange { get; private set; } = new();
        /// <summary>
        /// .. RangeEventMode가 BATCHED일때만 이벤트를 발행합니다.
        /// </summary>
        [field: SerializeField] public UnityEvent<T[]> OnRemovedRange { get; private set; } = new();

        /// <summary>
        /// .. RangeEventMode가 PER_ELEMENT일때는 Range관련 메서드를 호출할 시 OnAddedElement, OnRemovedElement에서 이벤트를 발행합니다.
        /// .. BATCHE 모드에서는 OnAddedRange, OnRemovedRange에서 이벤트를 발행합니다.
        /// </summary>
        [field: SerializeField] public RangeEventMode RangeMode { get; set; } = RangeEventMode.PER_ELEMENT; 

        public void Add(T item)
        {
            _list.Add(item);
            OnAddedElement.Invoke(item);
        }

        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null) return;

            _list.AddRange(collection);
            
            switch(RangeMode)
            {
                case RangeEventMode.PER_ELEMENT:
                    collection.ForEach(OnAddedElement.Invoke);
                    break;
                default:
                    OnAddedRange.Invoke(collection as T[] ?? collection.ToArray());
                    break;
            }
        }

        public void Remove(T item)
        {
            if (!_list.Remove(item)) return;
            OnRemovedElement.Invoke(item);
        }

        public void RemoveAt(int index)
        {
            if (index >= _list.Count || index < 0) return;

            T item = _list[index];
            _list.RemoveAt(index);
            OnRemovedElement.Invoke(item);
        }

        public void RemoveRange(int index, int count)
        {
            if (index < 0 || count < 0 || index + count > _list.Count) return;

            T[] items = new T[count];
            for (int i = 0; i < count; i++)
            {
                items[i] = _list[index + i];
            }
            _list.RemoveRange(index, count);

            switch (RangeMode)
            {
                case RangeEventMode.PER_ELEMENT:
                    items.ForEach(OnRemovedElement.Invoke);
                    break;
                default:
                    OnRemovedRange.Invoke(items);
                    break;
            }
        }

        public int RemoveAll(Predicate<T> match)
        {
            T[] removedElements = _list
                .Where(item => match.Invoke(item))
                .ToArray();

            int count = _list.RemoveAll(match);

            if (count > 0)
            {
                switch (RangeMode)
                {
                    case RangeEventMode.PER_ELEMENT:
                        removedElements.ForEach(OnRemovedElement.Invoke);
                        break;
                    default:
                        OnRemovedRange.Invoke(removedElements);
                        break;
                }
            }

            return count;
        }

        public void Insert(int index, T item)
        {
            if (index > _list.Count || index < 0) return;

            _list.Insert(index, item);
            OnAddedElement.Invoke(item);
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (index > _list.Count || index < 0) return;

            _list.InsertRange(index, collection);
            switch (RangeMode)
            {
                case RangeEventMode.PER_ELEMENT:
                    collection.ForEach(OnAddedElement.Invoke);
                    break;
                default:
                    OnAddedRange.Invoke(collection as T[] ?? collection.ToArray());
                    break;
            }
        }

        public void Clear()
        {
            if (_list.Count == 0) return;

            T[] items = _list.ToArray();
            _list.Clear();

            switch (RangeMode)
            {
                case RangeEventMode.PER_ELEMENT:
                    items.ForEach(OnRemovedElement.Invoke);
                    break;
                default:
                    OnRemovedRange.Invoke(items);
                    break;
            }
        }

        public List<T> ToList() => _list.ToList();
        public T[] ToArray() => _list.ToArray();
        public ReadOnlyCollection<T> AsReadOnly() => _list.AsReadOnly();
        public void BinarySearch(int index, int count, T item, IComparer<T> comparer) => _list.BinarySearch(index, count, item, comparer);
        public void BinarySearch(T item) => _list.BinarySearch(item);
        public void BinarySearch(T item, IComparer<T> comparer) => _list.BinarySearch(item, comparer);
        public bool Contains(T item) => _list.Contains(item);
        public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter) => _list.ConvertAll(converter);
        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public void CopyTo(T[] array) => _list.CopyTo(array);
        public void CopyTo(int index, T[] array, int arrayIndex, int count) => _list.CopyTo(index, array, arrayIndex, count);
        public bool Exists(Predicate<T> match) => _list.Exists(match);
        public T Find(Predicate<T> match) => _list.Find(match);
        public List<T> FindAll(Predicate<T> match) => _list.FindAll(match);
        public int FindIndex(int startIndex, int count, Predicate<T> match) => _list.FindIndex(startIndex, count, match);
        public int FindIndex(int startIndex, Predicate<T> match) => _list.FindIndex(startIndex, match);
        public int FindIndex(Predicate<T> match) => _list.FindIndex(match);
        public T FindLast(Predicate<T> match) => _list.FindLast(match);
        public int FindLastIndex(int startIndex, int count, Predicate<T> match) => _list.FindLastIndex(startIndex, count, match);
        public int FindLastIndex(int startIndex, Predicate<T> match) => _list.FindLastIndex(startIndex, match);
        public int FindLastIndex(Predicate<T> match) => _list.FindLastIndex(match);
        public void ForEach(Action<T> action) => _list.ForEach(action);
        public List<T>.Enumerator GetEnumerator() => _list.GetEnumerator();
        public List<T> GetRange(int index, int count) => _list.GetRange(index, count);
        public int IndexOf(T item, int index, int count) => _list.IndexOf(item, index, count);
        public int IndexOf(T item, int index) => _list.IndexOf(item, index);
        public int IndexOf(T item) => _list.IndexOf(item);
        public int LastIndexOf(T item) => _list.LastIndexOf(item);
        public int LastIndexOf(T item, int index) => _list.LastIndexOf(item, index);
        public int LastIndexOf(T item, int index, int count) => _list.LastIndexOf(item, index, count);
        public void Reverse(int index, int count) => _list.Reverse(index, count);
        public void Reverse() => _list.Reverse();
        public void Sort(Comparison<T> comparison) => _list.Sort(comparison);
        public void Sort(int index, int count, IComparer<T> comparer) => _list.Sort(index, count, comparer);
        public void Sort() => _list.Sort();
        public void Sort(IComparer<T> comparer) => _list.Sort(comparer);
        public void TrimExcess() => _list.TrimExcess();
        public bool TrueForAll(Predicate<T> match) => _list.TrueForAll(match);

        public ReactiveList(IEnumerable<T> collection) => _list.AddRange(collection);
        public ReactiveList(int capacity) => _list.Capacity = capacity;
        public ReactiveList() { }

        public T this[int index] 
        {
            get => _list[index];
            set
            {
                if (index < 0 || index >= _list.Count) return;

                T previousItem = _list[index];
                _list[index] = value;

                OnChangedElement.Invoke(new(index, previousItem, value));
            }
        }
    }
}