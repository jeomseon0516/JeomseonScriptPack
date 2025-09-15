using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Extensions
{
    public static class DictionaryExtensions 
    {
        public static void Add<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, KeyValuePair<TKey, TValue> kvp)
        {
            dictionary.Add(kvp.Key, kvp.Value);
        }

        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, KeyValuePair<TKey, TValue> kvp)
        {
            return dictionary.TryAdd(kvp.Key, kvp.Value);
        }

        public static void Add<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, (TKey, TValue) tuple)
        {
            dictionary.Add(tuple.Item1, tuple.Item2);
        }

        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, (TKey, TValue) tuple)
        {
            return dictionary.TryAdd(tuple.Item1, tuple.Item2);
        }
    }
}

