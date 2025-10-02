using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

namespace Jeomseon.UnityReactive
{
    public delegate void ElementChangedHandler<in T>(int index, T previous, T current);
    public delegate void AddOrRemoveHandler<in T>(int[] indices, T[] items);

    /// <summary>
    /// .. 내부 값을 추가/제거/변경이 불가능하고 리스너 추가만 가능한 읽기전용 인터페이스입니다
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IReadOnlyReactiveList<out T> : IReadOnlyList<T>
    {
        /// <summary>
        /// .. 값이 추가될때 발행되는 이벤트를 구독자에게 알림없이 리스너를 추가합니다
        /// </summary>
        /// <param name="onAddAction"> .. 리스너 메서드 </param>
        void AddListenerToAddedEventWithoutNotify(AddOrRemoveHandler<T> onAddAction);
        /// <summary>
        /// .. 값이 추가 될때 발행되는 이벤트입니다 리스너 추가시 한번 이벤트를 발행합니다
        /// </summary>
        event AddOrRemoveHandler<T> AddedEvent;
        /// <summary>
        /// .. 값이 제거 될때 발행되는 이벤트입니다
        /// </summary>
        event AddOrRemoveHandler<T> RemovedEvent;
        /// <summary>
        /// .. 내부
        /// </summary>
        event ElementChangedHandler<T> ChangedEvent;
        /// <summary>
        /// .. 내부 값들의 순서가 재배치 될때 발행되는 이벤트입니다
        /// </summary>
        event Action<IReadOnlyList<T>> ReorderedEvent;
    }

    [Serializable]
    public class ReactiveList<T> : IList<T>, IReadOnlyReactiveList<T>
    {
        [SerializeField] private List<T> _list = new();

        [SerializeField] private UnityEvent<int[], T[]> _addedEvent = new();
        [SerializeField] private UnityEvent<int[], T[]> _removedEvent = new();
        [SerializeField] private UnityEvent<int, T, T> _changedEvent = new();
        [SerializeField] private UnityEvent<IReadOnlyList<T>> _reorderedEvent = new();

        public event AddOrRemoveHandler<T> AddedEvent
        {
            add 
            {
                if (value == null) return;

                _addedEvent.AddListener((UnityAction<int[], T[]>)Delegate.CreateDelegate(typeof(UnityAction<int[], T[]>), value.Target, value.Method));

                int[] indices = new int[_list.Count];
                for (int i = 0; i < _list.Count; i++)
                {
                    indices[i] = i;
                }

                value.Invoke(indices, _list.ToArray());
            }
            remove => removeListenerSafe(_addedEvent, value);
        }

        public event AddOrRemoveHandler<T> RemovedEvent
        {
            add => addListenerSafe(_removedEvent, value);
            remove => removeListenerSafe(_removedEvent, value);
        }

        public event ElementChangedHandler<T> ChangedEvent
        {
            add => addListenerSafe(_changedEvent, value);
            remove => removeListenerSafe(_changedEvent, value);
        }

        public event Action<IReadOnlyList<T>> ReorderedEvent
        {
            add { if (value == null) return; _reorderedEvent.AddListener((UnityAction<IReadOnlyList<T>>)Delegate.CreateDelegate(typeof(UnityAction<IReadOnlyList<T>>), value.Target, value.Method)); }
            remove { if (value == null) return; _reorderedEvent.RemoveListener((UnityAction<IReadOnlyList<T>>)Delegate.CreateDelegate(typeof(UnityAction<IReadOnlyList<T>>), value.Target, value.Method)); }
        }

        public int Count => _list.Count;
        public int Capacity { get => _list.Capacity; set => _list.Capacity = value; }
        public bool IsReadOnly => false;

        public T this[int index]
        {
            get => _list[index];
            set
            {
                if (index < 0 || index >= _list.Count) return;

                T prev = _list[index];
                _list[index] = value;
                _changedEvent.Invoke(index, prev, value);
            }
        }

        public void AddListenerToAddedEventWithoutNotify(AddOrRemoveHandler<T> onAddAction) => addListenerSafe(_addedEvent, onAddAction);

        // -------------------- Add / Insert --------------------
        public void Add(T item) => insertInternal(_list.Count, item);
        public void Insert(int index, T item) => insertInternal(index, item);

        private void insertInternal(int index, T item)
        {
            if (index < 0 || index > _list.Count) return;

            _list.Insert(index, item);
            _addedEvent.Invoke(new int[] { index }, new T[] { item });
        }

        public void AddRange(IEnumerable<T> collection) => InsertRange(_list.Count, collection);
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null || index < 0 || index > _list.Count) return;

            int diff = getCollectionCount(collection);
            if (diff <= 0) return;

            _list.InsertRange(index, collection);
            getArrayFromCollection(index, diff, collection, out int[] indices, out T[] arr);
            _addedEvent.Invoke(indices, arr);
        }

        // -------------------- Remove --------------------
        public bool Remove(T item)
        {
            int index = _list.IndexOf(item);
            if (index < 0) return false;
            _list.RemoveAt(index);
            _removedEvent.Invoke(new[] { index }, new T[] { item });
            return true;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _list.Count) return;
            T item = _list[index];
            _list.RemoveAt(index);
            _removedEvent.Invoke(new[] { index }, new T[] { item });
        }

        public void RemoveRange(int index, int count)
        {
            if (index < 0 || count < 0 || index + count > _list.Count) return;

            T[] items = _list.GetRange(index, count).ToArray();
            int[] indices = new int[items.Length];

            for (int i = 0; i < count; i++)
            {
                indices[i] = index + i;
            }

            _list.RemoveRange(index, count);
            _removedEvent.Invoke(indices, items);
        }

        public int RemoveAll(Predicate<T> match)
        {
            List<(int, T)> values = new();
            for (int i = 0; i < _list.Count; i++)
            {
                if (!match.Invoke(_list[i])) continue;

                values.Add((i, _list[i]));
            }

            int count = 0;
            if (values.Count > 0)
            {
                int[] indices = new int[values.Count];
                T[] items = new T[values.Count];
                for (int i = 0; i < values.Count; i++)
                {
                    indices[i] = values[i].Item1;
                    items[i] = values[i].Item2;
                }

                count = _list.RemoveAll(match);
                _removedEvent.Invoke(indices, items);
            }

            return count;
        }

        public void Clear()
        {
            if (_list.Count == 0) return;
            T[] items = _list.ToArray();
            int[] indices = new int[_list.Count];

            for (int i = 0; i < items.Length; i++)
            {
                indices[i] = i;
            }

            _list.Clear();
            _removedEvent.Invoke(indices, items);
        }

        // -------------------- Helpers --------------------
        private static void addListenerSafe(UnityEvent<int[], T[]> unityEvent, AddOrRemoveHandler<T> callback)
        {
            if (callback == null) return;
            unityEvent.AddListener((UnityAction<int[], T[]>)Delegate.CreateDelegate(typeof(UnityAction<int[], T[]>), callback.Target, callback.Method));
        }

        private static void removeListenerSafe(UnityEvent<int[], T[]> unityEvent, AddOrRemoveHandler<T> callback)
        {
            if (callback == null) return;
            unityEvent.RemoveListener((UnityAction<int[], T[]>)Delegate.CreateDelegate(typeof(UnityAction<int[], T[]>), callback.Target, callback.Method));
        }

        private static void addListenerSafe(UnityEvent<int, T, T> unityEvent, ElementChangedHandler<T> callback)
        {
            if (callback == null) return;
            unityEvent.AddListener((UnityAction<int, T, T>)Delegate.CreateDelegate(typeof(UnityAction<int, T, T>), callback.Target, callback.Method));
        }

        private static void removeListenerSafe(UnityEvent<int, T, T> unityEvent, ElementChangedHandler<T> callback)
        {
            if (callback == null) return;
            unityEvent.RemoveListener((UnityAction<int, T, T>)Delegate.CreateDelegate(typeof(UnityAction<int, T, T>), callback.Target, callback.Method));
        }

        private static int getCollectionCount(IEnumerable<T> collection) =>
            collection is ICollection<T> col ? col.Count : collection.Count();

        private static void getArrayFromCollection(int start, int count, IEnumerable<T> collection, out int[] indices, out T[] arr)
        {
            indices = new int[count];
            arr = new T[count];

            if (collection is IList<T> list)
            {
                for (int i = 0; i < count; i++)
                {
                    indices[i] = start + i;
                    arr[i] = list[i];
                }
            }
            else
            {
                using var enumerator = collection.GetEnumerator();
                for (int i = 0; i < count; i++)
                {
                    if (!enumerator.MoveNext()) break;
                    indices[i] = start + i;
                    arr[i] = enumerator.Current;
                }
            }
        }

        public void Reverse(int index, int count, IComparer<T> comparer) 
        { 
            _list.Reverse(index, count);
            _reorderedEvent.Invoke(_list);
        }

        public void Reverse()
        {
            _list.Reverse();
            _reorderedEvent.Invoke(_list);
        }

        public void Sort(Comparison<T> comparison)
        {
            _list.Sort(comparison);
            _reorderedEvent.Invoke(_list);
        }

        public void Sort(int index, int count, IComparer<T> comparer)
        {
            _list.Sort(index, count, comparer);
            _reorderedEvent.Invoke(_list);
        }

        public void Sort()
        {
            _list.Sort();
            _reorderedEvent.Invoke(_list);
        }

        public void Sort(IComparer<T> comparer)
        {
            _list.Sort(comparer);
            _reorderedEvent.Invoke(_list);
        }

        // -------------------- 기타 List<T> Wrappers --------------------
        public List<T> ToList() => _list.ToList();
        public T[] ToArray() => _list.ToArray();
        public ReadOnlyCollection<T> AsReadOnly() => _list.AsReadOnly();
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer) => _list.BinarySearch(index, count, item, comparer);
        public int BinarySearch(T item) => _list.BinarySearch(item);
        public int BinarySearch(T item, IComparer<T> comparer) => _list.BinarySearch(item ,comparer);
        public bool Contains(T item) => _list.Contains(item);
        public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter) => _list.ConvertAll(converter);
        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public void CopyTo(T[] array) => _list.CopyTo(array);
        public void CopyTo(int index, T[] array, int arrayIndex, int count) => _list.CopyTo(index, array, arrayIndex, count);
        public bool Exist(Predicate<T> match) => _list.Exists(match);
        public T Find(Predicate<T> match) => _list.Find(match);
        public List<T> FindAll(Predicate<T> match) => _list.FindAll(match);
        public int FindIndex(int startIndex, int count, Predicate<T> match) => _list.FindIndex(startIndex, count, match);
        public int FindIndex(int startIndex, Predicate<T> match) => _list.FindIndex(startIndex, match);
        public int FindIndex(Predicate<T> match) => _list.FindIndex(match);
        public void ForEach(Action<T> action) => _list.ForEach(action);
        public List<T> GetRange(int index, int count) => _list.GetRange(index, count);
        public int IndexOf(T item, int index, int count) => _list.IndexOf(item, index, count);
        public int IndexOf(T item, int index) => _list.IndexOf(item, index);
        public int IndexOf(T item) => _list.IndexOf(item);
        public int LastIndexOf(T item) => _list.LastIndexOf(item);
        public int LastIndexOf(T item, int index) => _list.LastIndexOf(item, index);
        public int LastIndexOf(T item, int index, int count) => _list.LastIndexOf(item, index, count);
        public void TrimExcess() => _list.TrimExcess();
        public bool TrueForAll(Predicate<T> match) => _list.TrueForAll(match);
        public List<T>.Enumerator GetEnumerator() => _list.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        // 생성자
        public ReactiveList() { }
        public ReactiveList(int capacity) => _list.Capacity = capacity;
        public ReactiveList(IEnumerable<T> collection) => _list = new List<T>(collection);
    }
}
