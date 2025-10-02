using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Jeomseon.UnityReactive
{
    public delegate void ElementChangedHandler<in T>(int index, T previous, T current);

    public interface IReadOnlyReactiveList<out T> : IReadOnlyList<T>
    {
        event Action<T[]> AddedEvent;
        event Action<T[]> RemovedEvent;
        event ElementChangedHandler<T> ChangedEvent;
    }

    [Serializable]
    public class ReactiveList<T> : IList<T>, IReadOnlyReactiveList<T>
    {
        [SerializeField] private List<T> _list = new();

        [SerializeField] private UnityEvent<T[]> _addedEvent = new();
        [SerializeField] private UnityEvent<T[]> _removedEvent = new();
        [SerializeField] private UnityEvent<int, T, T> _changedEvent = new();

        public event Action<T[]> AddedEvent
        {
            add => AddListenerSafe(_addedEvent, value);
            remove => RemoveListenerSafe(_addedEvent, value);
        }

        public event Action<T[]> RemovedEvent
        {
            add => AddListenerSafe(_removedEvent, value);
            remove => RemoveListenerSafe(_removedEvent, value);
        }

        public event ElementChangedHandler<T> ChangedEvent
        {
            add => AddListenerSafe(_changedEvent, value);
            remove => RemoveListenerSafe(_changedEvent, value);
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

        // -------------------- Add / Insert --------------------
        public void Add(T item) => InsertInternal(_list.Count, item);
        public void Insert(int index, T item) => InsertInternal(index, item);

        private void InsertInternal(int index, T item)
        {
            if (index < 0 || index > _list.Count) return;

            _list.Insert(index, item);
            _addedEvent.Invoke(new T[] { item });
        }

        public void AddRange(IEnumerable<T> collection) => InsertRange(_list.Count, collection);
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null || index < 0 || index > _list.Count) return;

            int diff = GetCollectionCount(collection);
            if (diff <= 0) return;

            _list.InsertRange(index, collection);
            T[] array = GetArrayFromCollection(diff, collection);
            _addedEvent.Invoke(array);
        }

        // -------------------- Remove --------------------
        public bool Remove(T item)
        {
            if (!_list.Remove(item)) return false;
            _removedEvent.Invoke(new T[] { item });
            return true;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _list.Count) return;
            T item = _list[index];
            _list.RemoveAt(index);
            _removedEvent.Invoke(new T[] { item });
        }

        public void RemoveRange(int index, int count)
        {
            if (index < 0 || count < 0 || index + count > _list.Count) return;

            T[] items = _list.GetRange(index, count).ToArray();
            _list.RemoveRange(index, count);
            _removedEvent.Invoke(items);
        }

        public int RemoveAll(Predicate<T> match)
        {
            T[] removed = _list.Where(match.Invoke).ToArray();
            int count = _list.RemoveAll(match);
            if (count > 0) _removedEvent.Invoke(removed);
            return count;
        }

        public void Clear()
        {
            if (_list.Count == 0) return;
            T[] items = _list.ToArray();
            _list.Clear();
            _removedEvent.Invoke(items);
        }

        // -------------------- Helpers --------------------
        private static void AddListenerSafe(UnityEvent<T[]> unityEvent, Action<T[]> callback)
        {
            if (callback == null) return;
            unityEvent.AddListener((UnityAction<T[]>)Delegate.CreateDelegate(typeof(UnityAction<T[]>), callback.Target, callback.Method));
        }

        private static void RemoveListenerSafe(UnityEvent<T[]> unityEvent, Action<T[]> callback)
        {
            if (callback == null) return;
            unityEvent.RemoveListener((UnityAction<T[]>)Delegate.CreateDelegate(typeof(UnityAction<T[]>), callback.Target, callback.Method));
        }

        private static void AddListenerSafe(UnityEvent<int, T, T> unityEvent, ElementChangedHandler<T> callback)
        {
            if (callback == null) return;
            unityEvent.AddListener((UnityAction<int, T, T>)Delegate.CreateDelegate(typeof(UnityAction<int, T, T>), callback.Target, callback.Method));
        }

        private static void RemoveListenerSafe(UnityEvent<int, T, T> unityEvent, ElementChangedHandler<T> callback)
        {
            if (callback == null) return;
            unityEvent.RemoveListener((UnityAction<int, T, T>)Delegate.CreateDelegate(typeof(UnityAction<int, T, T>), callback.Target, callback.Method));
        }

        private static int GetCollectionCount(IEnumerable<T> collection) =>
            collection is ICollection<T> col ? col.Count : collection.Count();

        private static T[] GetArrayFromCollection(int count, IEnumerable<T> collection)
        {
            if (collection is IList<T> list)
            {
                T[] arr = new T[count];
                for (int i = 0; i < count; i++) arr[i] = list[i];
                return arr;
            }
            else
            {
                T[] arr = new T[count];
                using var enumerator = collection.GetEnumerator();
                for (int i = 0; i < count; i++)
                {
                    if (!enumerator.MoveNext()) break;
                    arr[i] = enumerator.Current;
                }
                return arr;
            }
        }

        // -------------------- 기타 List<T> Wrappers --------------------
        public List<T> ToList() => _list.ToList();
        public T[] ToArray() => _list.ToArray();
        public ReadOnlyCollection<T> AsReadOnly() => _list.AsReadOnly();
        public bool Contains(T item) => _list.Contains(item);
        public void ForEach(Action<T> action) => _list.ForEach(action);
        public int IndexOf(T item) => _list.IndexOf(item);
        public void Sort() => _list.Sort();
        public void Sort(IComparer<T> comparer) => _list.Sort(comparer);
        public void Reverse() => _list.Reverse();
        public void TrimExcess() => _list.TrimExcess();
        public List<T>.Enumerator GetEnumerator() => _list.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        // 생성자
        public ReactiveList() { }
        public ReactiveList(int capacity) => _list.Capacity = capacity;
        public ReactiveList(IEnumerable<T> collection) => _list = new List<T>(collection);
    }
}
