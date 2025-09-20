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

        internal void DownCount()
        {
            if (RefCount > 0) RefCount--;
            else
            {
#if DEBUG
                Debug.LogWarning("RefCountingOperationHandle: RefCount already zero, extra Release?");
#endif
            }
        }

        internal RefCountingOperationHandle(AsyncOperationHandle<T> handle)
        {
            Handle = handle;
        }
    }
}
