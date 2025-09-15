using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Jeomseon.Scope
{
    public sealed class DictionaryPoolScope<TKey, TValue> : IDisposable
    {
        private readonly Dictionary<TKey, TValue> _pooledDictionary = DictionaryPool<TKey, TValue>.Get();
        private bool _disposed;

        public Dictionary<TKey, TValue> Get()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DictionaryPoolScope<TKey, TValue>));

            return _pooledDictionary;
        }

        public void Dispose()
        {
            if (_disposed) return;

            if (_pooledDictionary is not null)
            {
                _pooledDictionary.Clear();
                DictionaryPool<TKey, TValue>.Release(_pooledDictionary);
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        ~DictionaryPoolScope()
        {
            if (!_disposed)
            {
                Debug.LogWarning($"{nameof(DictionaryPoolScope<TKey, TValue>)} was not disposed properly.");
                Dispose();
            }
        }
    }
}