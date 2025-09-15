using System;
using System.Text;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Jeomseon.Pool
{
    public class StringBuilderPool : IDisposable
    {
        public static StringBuilderPool Shared { get; } = new();
        private readonly ConcurrentStack<StringBuilder> _pool = new();
        private readonly SemaphoreSlim _semaphore;

        private bool _disposed;
        public int BufferSize { get; }
        public int MaxCapacity { get; } // StringBuilder의 최대 용량

        public StringBuilder Get()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(StringBuilderPool));

            _semaphore.Wait();
            try
            {
                return _pool.TryPop(out StringBuilder builder) ? builder : new();
            }
            catch
            {
                _semaphore.Release();
                throw;
            }
        }

        public void Release(StringBuilder builder, bool clear = true)
        {
            if (_disposed || builder is null || builder.Capacity > MaxCapacity) // .. 스트링 빌더의 용량이 지나치게 큰 경우 Pool반환을 받지않고 객체 파괴
            {
                _semaphore.Release();
                return;
            }

            if (clear)
            {
                builder.Clear();
            }

            _pool.Push(builder);
            _semaphore.Release();
        }

        public StringBuilderPool(int bufferSize = 100, int maxCapacity = 1024)
        {
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "BufferSize must be greater than zero.");
            }

            BufferSize = bufferSize;
            MaxCapacity = maxCapacity;
            _semaphore = new(bufferSize, bufferSize);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _semaphore.Dispose();
            _pool.Clear();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        ~StringBuilderPool()
        {
            Dispose();
        }
    }
}
