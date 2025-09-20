using Jeomseon.Prototype;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

internal static class AsyncOperationHandleExtensions
{
    internal static void ReleaseErrorHandle<T>(this AsyncOperationHandle<T> handle) where T : class
    {
#if DEBUG
        Debug.LogWarning("PrototypeManager : 잘못된 핸들을 해제합니다.");
#endif
        Addressables.Release(handle);
    }

    internal static RefCountingOperationHandle<T> GetHandle<T>(
        this Dictionary<string, RefCountingOperationHandle<T>> handles,
        string key) where T : class
    {
        if (!handles.TryGetValue(key, out var handle))
        {
            var asyncHandle = Addressables.LoadAssetAsync<T>(key);
            if (!asyncHandle.IsValid())
            {
                asyncHandle.ReleaseErrorHandle();
                return null;
            }

            handle = new(asyncHandle);
            handles.Add(key, handle);
        }

        handle.UpCount();
        return handle;
    }

    internal static RefCountingOperationHandle<T> GetHandle<T>(
        this Dictionary<string, RefCountingOperationHandle<T>> handles,
        IResourceLocation resourceLocation) where T : class
        => handles.GetHandle<T>(resourceLocation.PrimaryKey);

    internal static RefCountingOperationHandle<T> GetHandle<T>(
        this Dictionary<string, RefCountingOperationHandle<T>> handles,
        AssetReference assetReference) where T : class
        => handles.GetHandle<T>(assetReference.RuntimeKey.ToString());

    private static RefCountingOperationHandle<T> GetHandle<T>(
        this Dictionary<string, RefCountingOperationHandle<T>> handles,
        string key,
        System.Func<AsyncOperationHandle<T>> loader) where T : class
    {
        if (!handles.TryGetValue(key, out var handle))
        {
            var asyncHandle = loader();
            if (!asyncHandle.IsValid())
            {
                asyncHandle.ReleaseErrorHandle();
                return null;
            }

            handle = new(asyncHandle);
            handles.Add(key, handle);
        }

        handle.UpCount();
        return handle;
    }

    internal static void ReleaseInstance<T>(
        this Dictionary<string, RefCountingOperationHandle<T>> handles,
        string primaryKey) where T : class
    {
        if (!handles.TryGetValue(primaryKey, out var handle)) return;

        handle.DownCount();
        if (handle.RefCount < 1)
        {
            Addressables.Release(handle.Handle);
            handles.Remove(primaryKey);
        }
    }
}