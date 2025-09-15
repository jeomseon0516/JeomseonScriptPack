using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Jeomseon.Prototype
{
    internal sealed class RefCountingOperationHandle<T> where T : class
    {
        internal AsyncOperationHandle<T> Handle { get; }
        internal int RefCount { get; private set; }

        internal void UpCount() => RefCount++;
        internal void DownCount() => RefCount--;

        internal RefCountingOperationHandle(AsyncOperationHandle<T> handle)
        {
            Handle = handle;
        }

        private RefCountingOperationHandle() { }
    }
}
