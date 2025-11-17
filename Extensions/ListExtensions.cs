using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Extensions
{
    public static class ListExtensions
    {
        public static void AddElements<T>(this List<T> values, params T[] element) => values.AddRange(element);

        public static bool RemoveByConditionOne<T>(this List<T> values, Func<T, bool> predicate)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (!predicate.Invoke(values[i])) continue;

                values.RemoveAt(i);
                return true;
            }

            return false;
        }

        public static bool RemoveByConditionAll<T>(this List<T> values, Func<T, bool> predicate)
        {
            bool hasRemoved = false;
            
            for (int i = 0; i < values.Count; i++)
            {
                if (!predicate.Invoke(values[i])) continue;
                
                values.RemoveAt(i);
                hasRemoved = true;
            }

            return hasRemoved;
        }

        public static List<T> RemoveElements<T>(this List<T> source, Func<T> predicate)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            _ = source.Remove(predicate.Invoke());
            return source;
        }
        
        public static void AddRangeIfNotNull<T>(this List<T> values, IEnumerable<T> additional)
        {
            if (additional is null) return;

            values.AddRange(additional);
        }

        public static void ForEach<T>(this IList<T> source, Action<T> action)
        {
            for (int i = 0; i < source.Count; i++) action.Invoke(source[i]);
        }

        public static void ForEachSafe<T>(this IList<T> source, Action<T> action) where T : class
        {
            for (int i = 0; i < source.Count; i++) if (source[i] != null) action.Invoke(source[i]);
        }

        public static bool TryPop<T>(this IList<T> source, int index, out T result)
        {
            if (index < 0 || index >= source.Count)
            {
                result = default;
                return false;
            }

            result = source[index];
            source.RemoveAt(index);

            return true;
        }

        public static bool TryPopFirst<T>(this IList<T> source, out T result)
        {
            return source.TryPop(0, out result);
        }

        public static bool TryPopLast<T>(this IList<T> source, out T result)
        {
            return source.TryPop(source.Count - 1, out result);
        }

        public static T Pop<T>(this IList<T> source, int index)
        {
            if (index < 0 || index >= source.Count) return default;

            T element = source[index];
            source.RemoveAt(index);

            return element;
        }

        public static T PopFirst<T>(this IList<T> source)
        {
            return source.Pop(0);
        }

        public static T PopLast<T>(this IList<T> source)
        {
            return source.Pop(source.Count - 1);
        }
    }
}