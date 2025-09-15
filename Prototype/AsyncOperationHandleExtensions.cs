using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Jeomseon.Prototype
{
    internal static class AsyncOperationHandleExtensions
    {
        internal static void ReleaseErrorHandle<T>(this AsyncOperationHandle<T> handle) where T : class
        {
#if DEBUG
            Debug.LogWarning("PrototypeManager : 키값을 통해 리소스를 찾을 수 없습니다. ");
#endif
            Addressables.Release(handle);
        }

        //internal static RefCountingOperationHandle<Sprite> GetHandle(this Dictionary<string, RefCountingOperationHandle<Sprite>> handles, string key) where T : class
        //{
        //    if (!handles.TryGetValue(key, out RefCountingOperationHandle<Sprite> handle))
        //    {
        //        handle = new(Addressables.LoadAssetAsync<Sprite>(key));
        //        handles.Add(key, handle);
        //    }

        //    handle.UpCount();
        //    return handle;
        //}

        internal static RefCountingOperationHandle<T> GetHandle<T>(this Dictionary<string, RefCountingOperationHandle<T>> handles, string key) where T : class
        {
            if (!handles.TryGetValue(key, out RefCountingOperationHandle<T> handle))
            {
                handle = new(Addressables.LoadAssetAsync<T>(key));
                handles.Add(key, handle);
            }

            handle.UpCount();
            return handle;
        }

        internal static RefCountingOperationHandle<T> GetHandle<T>(this Dictionary<string, RefCountingOperationHandle<T>> handles, IResourceLocation resourceLocation) where T : class
        {
            if (!handles.TryGetValue(resourceLocation.PrimaryKey, out RefCountingOperationHandle<T> handle))
            {
                handle = new(Addressables.LoadAssetAsync<T>(resourceLocation));
                handles.Add(resourceLocation.PrimaryKey, handle);
            }

            handle.UpCount();
            return handle;
        }

        internal static RefCountingOperationHandle<T> GetHandle<T>(this Dictionary<string, RefCountingOperationHandle<T>> handles, AssetReference assetReference) where T : class
        {
            if (!handles.TryGetValue(assetReference.RuntimeKey.ToString(), out RefCountingOperationHandle<T> handle))
            {
                handle = new(assetReference.LoadAssetAsync<T>());
                handles.Add(assetReference.RuntimeKey.ToString(), handle);
            }

            handle.UpCount();
            return handle;
        }

        internal static void ReleaseInstance<T>(this Dictionary<string, RefCountingOperationHandle<T>> handles, string primaryKey) where T : class
        {
            if (!handles.TryGetValue(primaryKey, out RefCountingOperationHandle<T> handle)) return;

            handle.DownCount();

            if (handle.RefCount < 1)
            {
                Addressables.Release(handle.Handle);
                handles.Remove(primaryKey);
            }
        }
    }
}
