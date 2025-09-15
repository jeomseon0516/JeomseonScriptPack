using System;
using System.Text;
using Jeomseon.Pool;

namespace Jeomseon.Scope
{
    public sealed class StringBuilderPoolScope : IDisposable
    {
        private readonly StringBuilderPool _selectPool;
        private readonly StringBuilder _stringBuilder;
        private bool _disposed;

        public StringBuilder Get()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(StringBuilderPoolScope));
            return _stringBuilder;
        }

        public void Dispose()
        {
            if (_disposed) return;

            if (_stringBuilder is not null)
            {
                _selectPool.Release(_stringBuilder);
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        public StringBuilderPoolScope(StringBuilderPool selectPool = null)
        {
            _selectPool = selectPool ?? StringBuilderPool.Shared;
            _stringBuilder = _selectPool!.Get();
        }

        ~StringBuilderPoolScope()
        {
            Dispose();
        }
    }
}