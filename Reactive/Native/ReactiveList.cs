using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Jeomseon.Reactive
{
    /// <summary>
    /// Unity 의존성 없는 순수 C# ReactiveList
    /// </summary>
    [Serializable]
    public class ReactiveList<T> : IList<T>, IReadOnlyReactiveList<T>
    {
        private List<T> _list = new();

        private AddOrRemoveHandler<T> _added;
        private AddOrRemoveHandler<T> _removed;
        private ElementChangedHandler<T> _changed;
        private Action<IReadOnlyList<T>> _reordered;

        public event AddOrRemoveHandler<T> AddedEvent
        {
            add
            {
                if (value == null) return;
                _added += value;
                int[] indices = Enumerable.Range(0, _list.Count).ToArray();
                value.Invoke(indices, _list.ToArray());
            }
            remove { if (value != null) _added -= value; }
        }

        public event AddOrRemoveHandler<T> RemovedEvent
        {
            add { if (value != null) _removed += value; }
            remove { if (value != null) _removed -= value; }
        }

        public event ElementChangedHandler<T> ChangedEvent
        {
            add { if (value != null) _changed += value; }
            remove { if (value != null) _changed -= value; }
        }

        public event Action<IReadOnlyList<T>> ReorderedEvent
        {
            add { if (value != null) _reordered += value; }
            remove { if (value != null) _reordered -= value; }
        }

        public int Count => _list.Count;
        public int Capacity { get => _list.Capacity; set => _list.Capacity = value; }
        public bool IsReadOnly => false;

        public T this[int index]
        {
            get => _list[index];
            set
            {
                if ((uint)index >= (uint)_list.Count) return;
                T prev = _list[index];
                _list[index] = value;
                _changed?.Invoke(index, prev, value);
            }
        }

        public void AddListenerToAddedEventWithoutNotify(AddOrRemoveHandler<T> onAddAction)
        { if (onAddAction != null) _added += onAddAction; }

        // -------------------- Add / Insert --------------------
        public void Add(T item) => insertInternal(_list.Count, item);
        public void Insert(int index, T item) => insertInternal(index, item);
        private void insertInternal(int index, T item)
        {
            if ((uint)index > (uint)_list.Count) return;
            _list.Insert(index, item);
            var idx = new[] { index };
            var arr = new[] { item };
            _added?.Invoke(idx, arr);
        }

        public void AddRange(IEnumerable<T> collection) => InsertRange(_list.Count, collection);
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null || (uint)index > (uint)_list.Count) return;
            int diff = getCollectionCount(collection);
            if (diff <= 0) return;
            _list.InsertRange(index, collection);
            getArrayFromCollection(index, diff, collection, out int[] indices, out T[] arr);
            _added?.Invoke(indices, arr);
        }

        // -------------------- Remove --------------------
        public bool Remove(T item)
        {
            int index = _list.IndexOf(item);
            if (index < 0) return false;
            _list.RemoveAt(index);
            var idx = new[] { index };
            var arr = new[] { item };
            _removed?.Invoke(idx, arr);
            return true;
        }

        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_list.Count) return;
            T item = _list[index];
            _list.RemoveAt(index);
            var idx = new[] { index };
            var arr = new[] { item };
            _removed?.Invoke(idx, arr);
        }

        public void RemoveRange(int index, int count)
        {
            if (index < 0 || count < 0 || index + count > _list.Count) return;
            T[] items = _list.GetRange(index, count).ToArray();
            int[] indices = Enumerable.Range(index, count).ToArray();
            _list.RemoveRange(index, count);
            _removed?.Invoke(indices, items);
        }

        public int RemoveAll(Predicate<T> match)
        {
            if (match == null) return 0;
            var values = new List<(int, T)>();
            for (int i = 0; i < _list.Count; i++) if (match(_list[i])) values.Add((i, _list[i]));
            int count = 0;
            if (values.Count > 0)
            {
                int[] indices = new int[values.Count];
                T[] items = new T[values.Count];
                for (int i = 0; i < values.Count; i++) { indices[i] = values[i].Item1; items[i] = values[i].Item2; }
                count = _list.RemoveAll(match);
                _removed?.Invoke(indices, items);
            }
            return count;
        }

        public void Clear()
        {
            if (_list.Count == 0) return;
            T[] items = _list.ToArray();
            int[] indices = Enumerable.Range(0, _list.Count).ToArray();
            _list.Clear();
            _removed?.Invoke(indices, items);
        }

        // -------------------- Reorder --------------------
        public void Reverse(int index, int count) { _list.Reverse(index, count); _reordered?.Invoke(_list); }
        public void Reverse() { _list.Reverse(); _reordered?.Invoke(_list); }
        public void Sort(Comparison<T> comparison) { _list.Sort(comparison); _reordered?.Invoke(_list); }
        public void Sort(int index, int count, IComparer<T> comparer) { _list.Sort(index, count, comparer); _reordered?.Invoke(_list); }
        public void Sort() { _list.Sort(); _reordered?.Invoke(_list); }
        public void Sort(IComparer<T> comparer) { _list.Sort(comparer); _reordered?.Invoke(_list); }

        // -------------------- List<T> Wrappers --------------------
        public List<T> ToList() => _list.ToList();
        public T[] ToArray() => _list.ToArray();
        public ReadOnlyCollection<T> AsReadOnly() => _list.AsReadOnly();
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer) => _list.BinarySearch(index, count, item, comparer);
        public int BinarySearch(T item) => _list.BinarySearch(item);
        public int BinarySearch(T item, IComparer<T> comparer) => _list.BinarySearch(item, comparer);
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

        public ReactiveList() { }
        public ReactiveList(int capacity) { _list = new List<T>(capacity); }
        public ReactiveList(IEnumerable<T> collection) { _list = new List<T>(collection ?? Array.Empty<T>()); }

        private static int getCollectionCount(IEnumerable<T> collection) =>
            collection is ICollection<T> col ? col.Count : collection.Count();

        private static void getArrayFromCollection(int start, int count, IEnumerable<T> collection, out int[] indices, out T[] arr)
        {
            indices = new int[count];
            arr = new T[count];
            if (collection is IList<T> list)
            {
                for (int i = 0; i < count; i++) { indices[i] = start + i; arr[i] = list[i]; }
            }
            else
            {
                using var enumerator = collection.GetEnumerator();
                for (int i = 0; i < count; i++) { if (!enumerator.MoveNext()) break; indices[i] = start + i; arr[i] = enumerator.Current; }
            }
        }
    }
}
