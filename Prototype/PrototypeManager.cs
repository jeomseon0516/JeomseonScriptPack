using Jeomseon.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Jeomseon.Prototype
{
    using Object = UnityEngine.Object;

    // TODO : 차후 카탈로그 갱신 문제 수정
    public static class PrototypeManager
    {
        private static readonly Dictionary<string, RefCountingOperationHandle<GameObject>> _gameObjectHandles = new();
        private static readonly Dictionary<string, RefCountingOperationHandle<GameObject>> _gameObjectFromReferenceHandles = new();
        private static readonly Dictionary<string, Dictionary<string, IResourceLocation>> _labelLoadOperationManager = new();

        #region --- 공개 릴리즈 API ---
        internal static void ReleaseInstance(string primaryKey)
            => _gameObjectHandles.ReleaseInstance(primaryKey);

        internal static void ReleaseInstanceFromRuntimeKey(string runtimeKey)
            => _gameObjectFromReferenceHandles.ReleaseInstance(runtimeKey);
        #endregion

        #region --- 카탈로그 갱신 ---
        public static void RefreshCatalog()
        {
            Addressables.ClearResourceLocators();
            _labelLoadOperationManager.Clear();
            _gameObjectHandles.Clear();
            _gameObjectFromReferenceHandles.Clear();
        }
        #endregion

        #region --- 내부 유틸 ---
        private static RefCountingOperationHandle<GameObject> getHandle(object key)
        {
            return key switch
            {
                string strKey => _gameObjectHandles.GetHandle(strKey),
                IResourceLocation loc => _gameObjectHandles.GetHandle(loc),
                AssetReference assetRef => _gameObjectFromReferenceHandles.GetHandle(assetRef),
                _ => null
            };
        }

        private static GameObject createInstance<TRelease>(RefCountingOperationHandle<GameObject> rcHandle, string primaryKey, Transform parent)
            where TRelease : ReleaseAddressableInstance
        {
            if (rcHandle?.Handle.IsValid() == true && rcHandle.Handle.Status == AsyncOperationStatus.Succeeded && rcHandle.Handle.Result != null)
            {
                var go = Object.Instantiate(rcHandle.Handle.Result, parent);
                var rel = go.AddComponent<TRelease>();
                rel.PrimaryKey = primaryKey;
                return go;
            }
            return null;
        }

        private static TComponent createComponentInstance<TComponent, TRelease>(RefCountingOperationHandle<GameObject> rcHandle, string primaryKey, Transform parent)
            where TComponent : Component
            where TRelease : ReleaseAddressableInstance
        {
            if (rcHandle?.Handle.IsValid() == true &&
                rcHandle.Handle.Status == AsyncOperationStatus.Succeeded &&
                rcHandle.Handle.Result != null &&
                rcHandle.Handle.Result.TryGetComponent(out TComponent sourceComponent))
            {
                var inst = Object.Instantiate(sourceComponent, parent);
                var rel = inst.gameObject.AddComponent<TRelease>();
                rel.PrimaryKey = primaryKey;
                return inst;
            }
            return null;
        }

        private static Component createComponentInstance<TRelease>(RefCountingOperationHandle<GameObject> rcHandle, string primaryKey, Transform parent, Type type)
            where TRelease : ReleaseAddressableInstance
        {
            if (rcHandle?.Handle.IsValid() == true &&
                rcHandle.Handle.Status == AsyncOperationStatus.Succeeded &&
                rcHandle.Handle.Result != null &&
                rcHandle.Handle.Result.TryGetComponent(type, out var sourceComponent))
            {
                var inst = Object.Instantiate(sourceComponent, parent);
                var rel = inst.gameObject.AddComponent<TRelease>();
                rel.PrimaryKey = primaryKey;
                return inst;
            }
            return null;
        }


        private static void ensureReleaseOnFailure(Dictionary<string, RefCountingOperationHandle<GameObject>> container, string key)
        {
            try { container.ReleaseInstance(key); }
            catch (Exception e) { Debug.LogException(e); }
        }
        #endregion

        #region --- 공통 비동기 내부 ---
        private static void cloneAsyncInternal<TComponent, TRelease>(RefCountingOperationHandle<GameObject> rcHandle, string key, Action<TComponent> callback, Transform parent)
            where TComponent : class
            where TRelease : ReleaseAddressableInstance
        {
            if (rcHandle == null) { callback?.Invoke(null); return; }

            void completed(AsyncOperationHandle<GameObject> handle)
            {
                try
                {
                    TComponent result = null;
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                        result = getAssetByType<TComponent, TRelease>(rcHandle, key, parent);
                    else
                        ensureReleaseOnFailure(_gameObjectHandles, key);

                    callback?.Invoke(result);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    callback?.Invoke(null);
                    ensureReleaseOnFailure(_gameObjectHandles, key);
                }
                finally { try { rcHandle.Handle.Completed -= completed; } catch { } }
            }

            rcHandle.Handle.Completed += completed;
        }

        private static void cloneFromLocationsAsync<TComponent, TRelease>(List<IResourceLocation> locations, Action<List<TComponent>> callback, Transform parent)
            where TComponent : class
            where TRelease : ReleaseAddressableInstance
        {
            if (locations == null || locations.Count == 0) { callback?.Invoke(new List<TComponent>()); return; }

            var results = new List<TComponent>();
            int finished = 0;

            foreach (var loc in locations)
            {
                var rcHandle = getHandle(loc);
                void localCompleted(AsyncOperationHandle<GameObject> handle)
                {
                    try
                    {
                        finished++;
                        if (handle.Status == AsyncOperationStatus.Succeeded)
                        {
                            TComponent item = getAssetByType<TComponent, TRelease>(rcHandle, loc.PrimaryKey, parent);
                            if (item != null) results.Add(item);
                        }
                        if (finished >= locations.Count) callback?.Invoke(results);
                    }
                    catch (Exception ex) { Debug.LogException(ex); if (finished >= locations.Count) callback?.Invoke(results); }
                    finally { try { rcHandle.Handle.Completed -= localCompleted; } catch { } }
                }
                rcHandle.Handle.Completed += localCompleted;
            }
        }

        private static void cloneFromLabelAndReferenceAsync<TComponent, TRelease>(Dictionary<string, IResourceLocation> locations, string reference, Action<TComponent> callback, Transform parent)
            where TComponent : class
            where TRelease : ReleaseAddressableInstance
        {
            if (!locations.TryGetValue(reference, out var loc)) { callback?.Invoke(null); return; }
            var rcHandle = getHandle(loc);

            void completed(AsyncOperationHandle<GameObject> handle)
            {
                try
                {
                    callback?.Invoke(getAssetByType<TComponent, TRelease>(rcHandle, reference, parent));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    callback?.Invoke(null);
                }
                finally { try { rcHandle.Handle.Completed -= completed; } catch { } }
            }

            rcHandle.Handle.Completed += completed;
        }
        #endregion

        #region --- 공개 비동기 API ---
        public static void ClonePrototypeAsync(string key, Action<GameObject> callback, Transform parent = null)
            => cloneAsyncInternal<GameObject, ReleaseAddressableInstance>(getHandle(key), key, callback, parent);

        public static void ClonePrototypeAsync<T>(string key, Action<T> callback, Transform parent = null) where T : Component
            => cloneAsyncInternal<T, ReleaseAddressableInstance>(getHandle(key), key, callback, parent);

        public static void ClonePrototypeFromReferenceAsync(AssetReferenceGameObject assetRef, Action<GameObject> callback, Transform parent = null)
            => cloneAsyncInternal<GameObject, ReleaseAddressableInstanceFromAssetReference>(getHandle(assetRef), assetRef?.RuntimeKey.ToString(), callback, parent);

        public static void ClonePrototypeFromReferenceAsync<T>(AssetReferenceGameObject assetRef, Action<T> callback, Transform parent = null) where T : Component
            => cloneAsyncInternal<T, ReleaseAddressableInstanceFromAssetReference>(getHandle(assetRef), assetRef?.RuntimeKey.ToString(), callback, parent);

        public static void ClonePrototypeFromLabelAsync(string label, Action<List<GameObject>> callback, Transform parent = null)
            => cloneFromLabelAsyncInternal<GameObject, ReleaseAddressableInstance>(label, callback, parent);

        public static void ClonePrototypeFromLabelAsync<T>(string label, Action<List<T>> callback, Transform parent = null) where T : Component
            => cloneFromLabelAsyncInternal<T, ReleaseAddressableInstance>(label, callback, parent);

        public static void ClonePrototypeFromLabelAndReferenceAsync(string label, string reference, Action<GameObject> callback, Transform parent = null)
            => cloneFromLabelAndReferenceAsyncInternal<GameObject, ReleaseAddressableInstance>(label, reference, callback, parent);

        public static void ClonePrototypeFromLabelAndReferenceAsync<T>(string label, string reference, Action<T> callback, Transform parent = null) where T : Component
            => cloneFromLabelAndReferenceAsyncInternal<T, ReleaseAddressableInstance>(label, reference, callback, parent);
        #endregion

        #region --- 내부 라벨/레퍼런스 비동기 ---
        private static void cloneFromLabelAsyncInternal<TComponent, TRelease>(string label, Action<List<TComponent>> callback, Transform parent)
            where TComponent : class
            where TRelease : ReleaseAddressableInstance
        {
            if (string.IsNullOrEmpty(label)) { callback?.Invoke(new List<TComponent>()); return; }

            if (!_labelLoadOperationManager.TryGetValue(label, out var locations))
            {
                var loadOp = Addressables.LoadResourceLocationsAsync(label);
                loadOp.Completed += op =>
                {
                    try
                    {
                        if (op.Status == AsyncOperationStatus.Succeeded)
                        {
                            locations = op.Result.ToDictionary(l => l.PrimaryKey);
                            _labelLoadOperationManager[label] = locations;
                            cloneFromLocationsAsync<TComponent, TRelease>(locations.Values.ToList(), callback, parent);
                        }
                        else callback?.Invoke(new List<TComponent>());
                    }
                    catch (Exception ex) { Debug.LogException(ex); callback?.Invoke(new List<TComponent>()); }
                    finally { Addressables.Release(op); }
                };
            }
            else
            {
                cloneFromLocationsAsync<TComponent, TRelease>(locations.Values.ToList(), callback, parent);
            }
        }

        private static void cloneFromLabelAndReferenceAsyncInternal<TComponent, TRelease>(string label, string reference, Action<TComponent> callback, Transform parent)
            where TComponent : class
            where TRelease : ReleaseAddressableInstance
        {
            if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(reference)) { callback?.Invoke(null); return; }

            if (!_labelLoadOperationManager.TryGetValue(label, out var locations))
            {
                var loadOp = Addressables.LoadResourceLocationsAsync(label);
                loadOp.Completed += op =>
                {
                    try
                    {
                        if (op.Status == AsyncOperationStatus.Succeeded)
                        {
                            locations = op.Result.ToDictionary(l => l.PrimaryKey);
                            _labelLoadOperationManager[label] = locations;
                            cloneFromLabelAndReferenceAsync<TComponent, TRelease>(locations, reference, callback, parent);
                        }
                        else callback?.Invoke(null);
                    }
                    catch (Exception ex) { Debug.LogException(ex); callback?.Invoke(null); }
                    finally { Addressables.Release(op); }
                };
            }
            else
            {
                cloneFromLabelAndReferenceAsync<TComponent, TRelease>(locations, reference, callback, parent);
            }
        }
        #endregion

        #region --- 공개 동기 API ---
        public static GameObject ClonePrototypeSync(string key, Transform parent = null)
        {
            var rcHandle = getHandle(key);
            try { rcHandle?.Handle.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }
            var go = createInstance<ReleaseAddressableInstance>(rcHandle, key, parent);
            if (go == null) ensureReleaseOnFailure(_gameObjectHandles, key);
            return go;
        }

        public static T ClonePrototypeSync<T>(string key, Transform parent = null) where T : Component
        {
            var rcHandle = getHandle(key);
            try { rcHandle?.Handle.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }
            var comp = createComponentInstance<T, ReleaseAddressableInstance>(rcHandle, key, parent);
            if (comp == null) ensureReleaseOnFailure(_gameObjectHandles, key);
            return comp;
        }

        public static GameObject ClonePrototypeFromReferenceSync(AssetReferenceGameObject assetRef, Transform parent = null)
        {
            var rcHandle = getHandle(assetRef);
            try { rcHandle?.Handle.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }
            var go = createInstance<ReleaseAddressableInstanceFromAssetReference>(rcHandle, assetRef.RuntimeKey.ToString(), parent);
            if (go == null) ensureReleaseOnFailure(_gameObjectFromReferenceHandles, assetRef.RuntimeKey.ToString());
            return go;
        }

        public static T ClonePrototypeFromReferenceSync<T>(AssetReferenceGameObject assetRef, Transform parent = null) where T : Component
        {
            var rcHandle = getHandle(assetRef);
            try { rcHandle?.Handle.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }
            var comp = createComponentInstance<T, ReleaseAddressableInstanceFromAssetReference>(rcHandle, assetRef.RuntimeKey.ToString(), parent);
            if (comp == null) ensureReleaseOnFailure(_gameObjectFromReferenceHandles, assetRef.RuntimeKey.ToString());
            return comp;
        }

        public static List<GameObject> ClonePrototypeFromLabelSync(string label, Transform parent = null)
            => cloneFromLabelSyncInternal<GameObject, ReleaseAddressableInstance>(label, parent);

        public static List<T> ClonePrototypeFromLabelSync<T>(string label, Transform parent = null) where T : Component
            => cloneFromLabelSyncInternal<T, ReleaseAddressableInstance>(label, parent);

        public static GameObject ClonePrototypeFromLabelAndReferenceSync(string label, string reference, Transform parent = null)
            => cloneFromLabelAndReferenceSyncInternal<GameObject, ReleaseAddressableInstance>(label, reference, parent);

        public static T ClonePrototypeFromLabelAndReferenceSync<T>(string label, string reference, Transform parent = null) where T : Component
            => cloneFromLabelAndReferenceSyncInternal<T, ReleaseAddressableInstance>(label, reference, parent);
        #endregion

        #region --- 내부 동기 라벨/레퍼런스 ---
        private static List<TComponent> cloneFromLabelSyncInternal<TComponent, TRelease>(string label, Transform parent)
            where TComponent : class
            where TRelease : ReleaseAddressableInstance
        {
            if (string.IsNullOrEmpty(label)) return new List<TComponent>();

            var locations = getLocations(label);
            if (locations is null) return null;

            var results = new List<TComponent>();
            foreach (var loc in locations.Values)
            {
                var rcHandle = getHandle(loc);
                try { _ = rcHandle.Handle.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }
                TComponent item = getAssetByType<TComponent, TRelease>(rcHandle, loc.PrimaryKey, parent);
                if (item != null) results.Add(item);
            }

            return results;
        }

        private static TComponent cloneFromLabelAndReferenceSyncInternal<TComponent, TRelease>(string label, string reference, Transform parent)
            where TComponent : class
            where TRelease : ReleaseAddressableInstance
        {
            if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(reference)) return null;

            var locations = getLocations(label);

            if (locations is null || !locations.TryGetValue(reference, out var loc)) return null;

            var rcHandle = getHandle(loc);
            try { _ = rcHandle.Handle.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }

            return getAssetByType<TComponent, TRelease>(rcHandle, reference, parent);
        }

        private static TComponent getAssetByType<TComponent, TRelease>(RefCountingOperationHandle<GameObject> rcHandle, string reference, Transform parent) 
            where TComponent : class
            where TRelease : ReleaseAddressableInstance
        {
            return typeof(TComponent) == typeof(GameObject)
                ? createInstance<TRelease>(rcHandle, reference, parent) as TComponent
                : createComponentInstance<TRelease>(rcHandle, reference, parent, typeof(TComponent)) as TComponent;
        }

        private static Dictionary<string, IResourceLocation> getLocations(string label)
        {
            if (!_labelLoadOperationManager.TryGetValue(label, out var locations))
            {
                var loadOp = Addressables.LoadResourceLocationsAsync(label);
                try { _ = loadOp.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }
                if (loadOp.Status == AsyncOperationStatus.Succeeded)
                {
                    locations = loadOp.Result.ToDictionary(l => l.PrimaryKey);
                    _labelLoadOperationManager[label] = locations;
                }
                else { Addressables.Release(loadOp); return null; }
                Addressables.Release(loadOp);
            }

            return locations;
        }
        #endregion
    }
}
