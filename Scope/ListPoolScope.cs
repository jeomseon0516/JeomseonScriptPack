using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Jeomseon.Scope
{
    public sealed class ListPoolScope<T> : ICollectionScope<List<T>, T>
    {
        private readonly List<T> _pooledList = ListPool<T>.Get();
        private bool _disposed;

        public List<T> Get()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ListPoolScope<T>));

            return _pooledList;
        }

        public void Dispose()
        {
            if (_disposed) return;

            if (_pooledList is not null)
            {
                _pooledList.Clear();
                ListPool<T>.Release(_pooledList);
            }
            
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        ~ListPoolScope()
        {
            if (!_disposed)
            {
                Debug.LogWarning($"{nameof(ListPoolScope<T>)} was not disposed properly.");
                Dispose();
            }
        }
    }
}