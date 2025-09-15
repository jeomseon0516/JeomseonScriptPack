using System;
using System.Buffers;
using UnityEngine;

namespace Jeomseon.Scope
{
    public class ArrayPoolScope<T> : IDisposable
    {
        private readonly T[] _pooledArray;
        private bool _disposed;

        public T[] Get()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ArrayPoolScope<T>));

            return _pooledArray;
        }

        public void Dispose()
        {
            if (_disposed) return;

            if (_pooledArray is not null)
            {
                ArrayPool<T>.Shared.Return(_pooledArray, true);
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        public ArrayPoolScope(int minimumLength)
        {
            if (minimumLength <= 0) throw new ArgumentOutOfRangeException(nameof(minimumLength), "Minimum length must be greater than zero.");

            _pooledArray = ArrayPool<T>.Shared.Rent(minimumLength);
        }

#if DEBUG
        ~ArrayPoolScope()
        {
            if (!_disposed)
            {
                Debug.LogWarning($"{nameof(ArrayPoolScope<T>)} was not disposed properly.");
                Dispose();
            }
        }
#endif
    }
}