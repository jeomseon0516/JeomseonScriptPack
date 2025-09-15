using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AYellowpaper.SerializedCollections
{
    internal class DictionaryLookupTable<TKey, TValue> : IKeyable
    {
        private readonly SerializedDictionary<TKey, TValue> _dictionary;
        private readonly Dictionary<TKey, List<int>> _occurences = new();

        private static readonly List<int> EmptyList = new List<int>();

        public IEnumerable Keys => _dictionary.Keys;

        public DictionaryLookupTable(SerializedDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        public IReadOnlyList<int> GetOccurences(object key)
        {
            if (key is TKey castKey && _occurences.TryGetValue(castKey, out List<int> list))
                return list;

            return EmptyList;
        }

        public void RecalculateOccurences()
        {
            _occurences.Clear();

            int count = _dictionary._serializedList.Count;
            for (int i = 0; i < count; i++)
            {
                SerializedKeyValuePair<TKey, TValue> kvp = _dictionary._serializedList[i];
                if (!SerializedCollectionsUtility.IsValidKey(kvp.Key))
                    continue;

                if (!_occurences.TryGetValue(kvp.Key, out List<int> occurence))
                    _occurences.Add(kvp.Key, new() { i });
                else
                    occurence.Add(i);
            }
        }

        public void RemoveKey(object key)
        {
            for (int i = _dictionary._serializedList.Count - 1; i >= 0; i--)
            {
                TKey dictKey = _dictionary._serializedList[i].Key;
                if ((object)dictKey == key || dictKey.Equals(key))
                    _dictionary._serializedList.RemoveAt(i);
            }
        }

        public void RemoveAt(int index)
        {
            _dictionary._serializedList.RemoveAt(index);
        }

        public object GetKeyAt(int index)
        {
            return _dictionary._serializedList[index];
        }

        public void RemoveDuplicates()
        {
            _dictionary._serializedList = _dictionary._serializedList
                .GroupBy(x => x.Key)
                .Where(x => SerializedCollectionsUtility.IsValidKey(x.Key))
                .Select(x => x.First()).ToList();
        }

        public void AddKey(object key)
        {
            SerializedKeyValuePair<TKey, TValue> entry = new()
            {
                Key = (TKey)key
            };
            _dictionary._serializedList.Add(entry);
        }
    }
}