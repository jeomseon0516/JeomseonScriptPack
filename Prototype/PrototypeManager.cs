using Jeomseon.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Jeomseon.Prototype
{
    using Object = UnityEngine.Object;

    /// <summary>
    /// PrototypeManager (리팩터링)
    /// - 중복 코드를 줄여 get/clone 흐름을 일원화함
    /// - 모든 Completed 콜백에서 예외를 안전하게 처리하고 항상 구독 해제
    /// - 실패 시 핸들/레퍼런스 해제 로직을 일관화
    /// - 라벨/레퍼런스/로케이션 기반 호출을 공통 유틸로 정리
    /// - 동기/비동기 인터페이스를 동일한 내부 함수로 재사용
    /// </summary>
    public static class PrototypeManager
    {
        private static readonly Dictionary<string, RefCountingOperationHandle<GameObject>> _gameObjectHandles = new();
        private static readonly Dictionary<string, RefCountingOperationHandle<GameObject>> _gameObjectFromReferenceHandles = new();
        private static readonly Dictionary<string, Dictionary<string, IResourceLocation>> _labelLoadOperationManager = new();

        #region 공개 릴리즈 API
        internal static void ReleaseInstance(string primaryKey)
            => _gameObjectHandles.ReleaseInstance(primaryKey);

        internal static void ReleaseInstanceFromRuntimeKey(string runtimeKey)
            => _gameObjectFromReferenceHandles.ReleaseInstance(runtimeKey);
        #endregion

        #region --- 핵심 공통 유틸 ---

        // 공통: 성공 시 Instantiate 및 ReleaseAddressableInstance 컴포넌트 추가
        private static GameObject createInstanceFromHandle<TRelease>(RefCountingOperationHandle<GameObject> rcHandle, string primaryKey, Transform parent)
            where TRelease : ReleaseAddressableInstance
        {
            if (rcHandle?.Handle.IsValid() == true && rcHandle.Handle.Status == AsyncOperationStatus.Succeeded && rcHandle.Handle.Result != null)
            {
                var go = Object.Instantiate(rcHandle.Handle.Result, parent);
                var rel = go.AddComponent<TRelease>();
                rel.PrimaryKey = primaryKey;
                return go;
            }

            // 실패 시 호출자에게 null 반환 (호출자는 핸들 해제를 담당하도록 함)
            return null;
        }

        private static Component createComponentInstanceFromHandle<TRelease>(
            RefCountingOperationHandle<GameObject> rcHandle, string primaryKey, Transform parent, Type type)
            where TRelease : ReleaseAddressableInstance
        {
            if (rcHandle?.Handle.IsValid() == true &&
                rcHandle.Handle.Status == AsyncOperationStatus.Succeeded &&
                rcHandle.Handle.Result != null &&
                rcHandle.Handle.Result.TryGetComponent(type, out Component sourceComponent))
            {
                var inst = Object.Instantiate(sourceComponent, parent);
                var rel = inst.gameObject.AddComponent<TRelease>();
                rel.PrimaryKey = primaryKey;
                return inst;
            }

            return null;
        }

        private static TComponent createComponentInstanceFromHandle<TComponent, TRelease>(
            RefCountingOperationHandle<GameObject> rcHandle, string primaryKey, Transform parent)
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

        // 핸들 얻기: key(string) 또는 IResourceLocation 또는 AssetReferenceGameObject
        private static RefCountingOperationHandle<GameObject> getHandleForKey(string key)
            => _gameObjectHandles.GetHandle(key);

        private static RefCountingOperationHandle<GameObject> getHandleForLocation(IResourceLocation location)
            => _gameObjectHandles.GetHandle(location);

        private static RefCountingOperationHandle<GameObject> getHandleForAssetReference(AssetReference assetReference)
            => _gameObjectFromReferenceHandles.GetHandle(assetReference);

        // 실패 시 핸들 release를 안전하게 수행 (null-safe)
        private static void ensureReleaseOnFailure(Dictionary<string, RefCountingOperationHandle<GameObject>> container, string key)
        {
            try
            {
                container.ReleaseInstance(key);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        #endregion

        #region --- 비동기 공통 흐름 ---

        // 일반 GameObject 비동기 복제 (generic TRelease로 release 컴포넌트 결정)
        private static void cloneAsyncInternal(
            RefCountingOperationHandle<GameObject> rcHandle,
            string primaryKey,
            Action<GameObject> callback,
            Transform parent,
            Type releaseType)
        {
            if (rcHandle == null)
            {
                callback?.Invoke(null);
                return;
            }

            void completed(AsyncOperationHandle<GameObject> handle)
            {
                try
                {
                    GameObject result = null;
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        // reflection을 사용해서 적절한 ReleaseAddressableInstance 타입으로 인스턴스 생성
                        if (releaseType == typeof(ReleaseAddressableInstance))
                            result = createInstanceFromHandle<ReleaseAddressableInstance>(rcHandle, primaryKey, parent);
                        else if (releaseType == typeof(ReleaseAddressableInstanceFromAssetReference))
                            result = createInstanceFromHandle<ReleaseAddressableInstanceFromAssetReference>(rcHandle, primaryKey, parent);
                        else
                            result = createInstanceFromHandle<ReleaseAddressableInstance>(rcHandle, primaryKey, parent);
                    }
                    else
                    {
                        // 실패시 릴리즈(참조 카운트 감소)
                        ensureReleaseOnFailure(_gameObjectHandles, primaryKey);
                    }

                    callback?.Invoke(result);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    callback?.Invoke(null);
                    ensureReleaseOnFailure(_gameObjectHandles, primaryKey);
                }
                finally
                {
                    // 항상 구독 해제 (안전)
                    try { rcHandle.Handle.Completed -= completed; } catch { }
                }
            }

            rcHandle.Handle.Completed += completed;
        }

        // 특정 컴포넌트 형식 복제 (TComponent)
        private static void cloneComponentAsyncInternal<TComponent>(
            RefCountingOperationHandle<GameObject> rcHandle,
            string primaryKey,
            Action<TComponent> callback,
            Transform parent,
            Type releaseType) where TComponent : Component
        {
            if (rcHandle == null)
            {
                callback?.Invoke(null);
                return;
            }

            void completed(AsyncOperationHandle<GameObject> handle)
            {
                try
                {
                    TComponent result = null;
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        if (releaseType == typeof(ReleaseAddressableInstance))
                            result = createComponentInstanceFromHandle<TComponent, ReleaseAddressableInstance>(rcHandle, primaryKey, parent);
                        else if (releaseType == typeof(ReleaseAddressableInstanceFromAssetReference))
                            result = createComponentInstanceFromHandle<TComponent, ReleaseAddressableInstanceFromAssetReference>(rcHandle, primaryKey, parent);
                        else
                            result = createComponentInstanceFromHandle<TComponent, ReleaseAddressableInstance>(rcHandle, primaryKey, parent);
                    }
                    else
                    {
                        ensureReleaseOnFailure(_gameObjectHandles, primaryKey);
                    }

                    callback?.Invoke(result);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    callback?.Invoke(null);
                    ensureReleaseOnFailure(_gameObjectHandles, primaryKey);
                }
                finally
                {
                    try { rcHandle.Handle.Completed -= completed; } catch { }
                }
            }

            rcHandle.Handle.Completed += completed;
        }

        #endregion

        #region --- 공개 비동기 API (간결하게 재정의) ---

        // key 기반 (GameObject)
        public static void ClonePrototypeAsync(string key, Action<GameObject> callback, Transform parent = null)
        {
            var rcHandle = getHandleForKey(key);
            cloneAsyncInternal(rcHandle, key, callback, parent, typeof(ReleaseAddressableInstance));
        }

        // key 기반 (컴포넌트)
        public static void ClonePrototypeAsync<T>(string key, Action<T> callback, Transform parent = null) where T : Component
        {
            var rcHandle = getHandleForKey(key);
            cloneComponentAsyncInternal(rcHandle, key, callback, parent, typeof(ReleaseAddressableInstance));
        }

        // AssetReference 기반 (GameObject)
        public static void ClonePrototypeFromReferenceAsync(AssetReferenceGameObject assetReference, Action<GameObject> callback, Transform parent = null)
        {
            if (assetReference == null) { callback?.Invoke(null); return; }
            var rcHandle = getHandleForAssetReference(assetReference);
            cloneAsyncInternal(rcHandle, assetReference.RuntimeKey.ToString(), callback, parent, typeof(ReleaseAddressableInstanceFromAssetReference));
        }

        // AssetReference 기반 (컴포넌트)
        public static void ClonePrototypeFromReferenceAsync<T>(AssetReferenceGameObject assetReference, Action<T> callback, Transform parent = null) where T : Component
        {
            if (assetReference == null) { callback?.Invoke(null); return; }
            var rcHandle = getHandleForAssetReference(assetReference);
            cloneComponentAsyncInternal(rcHandle, assetReference.RuntimeKey.ToString(), callback, parent, typeof(ReleaseAddressableInstanceFromAssetReference));
        }

        // Label -> 여러 ResourceLocation 로드 후 각 로케이션으로부터 복제 (GameObject)
        public static void ClonePrototypeFromLabelAsync(string label, Action<List<GameObject>> callback, Transform parent = null)
        {
            if (string.IsNullOrEmpty(label)) { callback?.Invoke(null); return; }

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
                            cloneFromLocationsAsync(locations.Values.ToList(), callback, parent, isComponent: false);
                        }
                        else
                        {
#if DEBUG
                            Debug.LogWarning($"Label로 리소스를 찾아올 수 없습니다 Label : {label}");
#endif
                            callback?.Invoke(null);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        callback?.Invoke(null);
                    }
                    finally
                    {
                        Addressables.Release(op);
                    }
                };
            }
            else
            {
                cloneFromLocationsAsync(locations.Values.ToList(), callback, parent, isComponent: false);
            }
        }

        // Label -> 여러 로케이션 -> 컴포넌트 타입
        public static void ClonePrototypeFromLabelAsync<T>(string label, Action<List<T>> callback, Transform parent = null) where T : Component
        {
            if (string.IsNullOrEmpty(label)) { callback?.Invoke(null); return; }

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
                            cloneFromLocationsAsync(locations.Values.ToList(), callback, parent, isComponent: true);
                        }
                        else
                        {
#if DEBUG
                            Debug.LogWarning($"Label로 리소스를 찾아올 수 없습니다 Label : {label}");
#endif
                            callback?.Invoke(null);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        callback?.Invoke(null);
                    }
                    finally
                    {
                        Addressables.Release(op);
                    }
                };
            }
            else
            {
                cloneFromLocationsAsync(locations.Values.ToList(), callback, parent, isComponent: true);
            }
        }

        // Label + reference (단건) - GameObject
        public static void ClonePrototypeFromLabelAndReferenceAsync(string label, string reference, Action<GameObject> callback, Transform parent = null)
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
                            cloneFromLabelAndReferenceInternal(locations, reference, callback, parent, isComponent: false);
                        }
                        else
                        {
#if DEBUG
                            Debug.LogWarning($"Label로 리소스를 찾아올 수 없습니다 Label : {label}");
#endif
                            callback?.Invoke(null);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        callback?.Invoke(null);
                    }
                    finally
                    {
                        Addressables.Release(op);
                    }
                };
            }
            else
            {
                cloneFromLabelAndReferenceInternal(locations, reference, callback, parent, isComponent: false);
            }
        }

        // Label + reference (단건) - Component
        public static void ClonePrototypeFromLabelAndReferenceAsync<T>(string label, string reference, Action<T> callback, Transform parent = null) where T : Component
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
                            cloneFromLabelAndReferenceInternal(locations, reference, callback, parent, isComponent: true);
                        }
                        else
                        {
#if DEBUG
                            Debug.LogWarning($"Label로 리소스를 찾아올 수 없습니다 Label : {label}");
#endif
                            callback?.Invoke(null);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        callback?.Invoke(null);
                    }
                    finally
                    {
                        Addressables.Release(op);
                    }
                };
            }
            else
            {
                cloneFromLabelAndReferenceInternal(locations, reference, callback, parent, isComponent: true);
            }
        }

        // 공통: locations 리스트 순회 후 비동기 복제 (오브젝트/컴포넌트 분기)
        private static void cloneFromLocationsAsync<T>(List<IResourceLocation> locations, Action<List<T>> callback, Transform parent, bool isComponent) where T : class
        {
            if (locations == null || locations.Count == 0) { callback?.Invoke(new List<T>()); return; }

            var results = new List<T?>(locations.Count);
            int finished = 0;

            foreach (var loc in locations)
            {
                var rcHandle = getHandleForLocation(loc);
                void localCompleted(AsyncOperationHandle<GameObject> handle)
                {
                    try
                    {
                        finished++;
                        if (handle.Status == AsyncOperationStatus.Succeeded)
                        {
                            if (isComponent)
                            {
                                if (createComponentInstanceFromHandle<ReleaseAddressableInstance>(rcHandle, loc.PrimaryKey, parent, typeof(T)) is T comp) results.Add(comp);
                            }
                            else
                            {
                                if (createInstanceFromHandle<ReleaseAddressableInstance>(rcHandle, loc.PrimaryKey, parent) is T go) results.Add(go);
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"ResourceLocation 정보로 에셋을 불러오는데 실패하였습니다 Log : {loc.PrimaryKey}");
                        }

                        if (finished >= locations.Count)
                        {
                            callback?.Invoke(results.Where(r => r != null).ToList()!);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        if (finished >= locations.Count)
                            callback?.Invoke(results.Where(r => r != null).ToList()!);
                    }
                    finally
                    {
                        try { rcHandle.Handle.Completed -= localCompleted; } catch { }
                    }
                }

                rcHandle.Handle.Completed += localCompleted;
            }
        }

        // 비-제네릭 대체: 오브젝트 전용 (List<GameObject>) 호출자용
        private static void cloneFromLocationsAsync(List<IResourceLocation> locations, Action<List<GameObject>> callback, Transform parent, bool isComponent)
        {
            cloneFromLocationsAsync<GameObject>(locations, callback as Action<List<GameObject>>, parent, isComponent);
        }

        // Label + reference 내부 처리 (단건)
        private static void cloneFromLabelAndReferenceInternal<T>(Dictionary<string, IResourceLocation> locations, string reference, Action<T> callback, Transform parent, bool isComponent) where T : class
        {
            if (!locations.TryGetValue(reference, out var loc))
            {
                Debug.LogWarning($"PrototypeManager : 레퍼런스를 통해 리소스를 찾을 수 없습니다 {reference} Dictionary value not found");
                callback?.Invoke(null);
                return;
            }

            var rcHandle = getHandleForLocation(loc);

            void completed(AsyncOperationHandle<GameObject> handle)
            {
                try
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        if (isComponent)
                        {
                            T comp = createComponentInstanceFromHandle<ReleaseAddressableInstance>(rcHandle, reference, parent, typeof(T)) as T;
                            callback?.Invoke(comp);
                        }
                        else
                        {
                            T go = createInstanceFromHandle<ReleaseAddressableInstance>(rcHandle, reference, parent) as T;
                            callback?.Invoke(go);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"ResourceLocation 정보로 에셋을 불러오는데 실패하였습니다 Log : {reference}");
                        callback?.Invoke(null);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    callback?.Invoke(null);
                }
                finally
                {
                    try { rcHandle.Handle.Completed -= completed; } catch { }
                }
            }

            rcHandle.Handle.Completed += completed;
        }

        #endregion

        #region --- 동기 API (동일 내부 로직 재사용) ---

        public static GameObject ClonePrototypeSync(string key, Transform parent = null)
        {
            var rcHandle = getHandleForKey(key);
            if (rcHandle == null) return null;
            try { _ = rcHandle.Handle.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }

            GameObject go = createInstanceFromHandle<ReleaseAddressableInstance>(rcHandle, key, parent);
            if (go == null) ensureReleaseOnFailure(_gameObjectHandles, key);
            return go;
        }

        public static T ClonePrototypeSync<T>(string key, Transform parent = null) where T : Component
        {
            var rcHandle = getHandleForKey(key);
            if (rcHandle == null) return null;
            try { _ = rcHandle.Handle.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }

            var comp = createComponentInstanceFromHandle<T, ReleaseAddressableInstance>(rcHandle, key, parent);
            if (comp == null) ensureReleaseOnFailure(_gameObjectHandles, key);
            return comp;
        }

        public static GameObject ClonePrototypeFromReferenceSync(AssetReferenceGameObject assetReference, Transform parent = null)
        {
            if (assetReference == null) return null;
            var rcHandle = getHandleForAssetReference(assetReference);
            try { _ = rcHandle.Handle.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }

            var go = createInstanceFromHandle<ReleaseAddressableInstanceFromAssetReference>(rcHandle, assetReference.RuntimeKey.ToString(), parent);
            if (go == null) ensureReleaseOnFailure(_gameObjectFromReferenceHandles, assetReference.RuntimeKey.ToString());
            return go;
        }

        public static T ClonePrototypeFromReferenceSync<T>(AssetReferenceGameObject assetReference, Transform parent = null) where T : Component
        {
            if (assetReference == null) return null;
            var rcHandle = getHandleForAssetReference(assetReference);
            try { _ = rcHandle.Handle.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }

            var comp = createComponentInstanceFromHandle<T, ReleaseAddressableInstanceFromAssetReference>(rcHandle, assetReference.RuntimeKey.ToString(), parent);
            if (comp == null) ensureReleaseOnFailure(_gameObjectFromReferenceHandles, assetReference.RuntimeKey.ToString());
            return comp;
        }

        public static List<GameObject> ClonePrototypeFromLabelSync(string label, Transform parent = null)
        {
            if (string.IsNullOrEmpty(label)) return null;

            if (!_labelLoadOperationManager.TryGetValue(label, out var locations))
            {
                var loadOp = Addressables.LoadResourceLocationsAsync(label);
                try { _ = loadOp.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }

                if (loadOp.Status == AsyncOperationStatus.Succeeded)
                {
                    locations = loadOp.Result.ToDictionary(l => l.PrimaryKey);
                    _labelLoadOperationManager[label] = locations;
                }
                else
                {
                    Debug.LogWarning($"PrototypeManager : 라벨을 통해 에셋을 불러오는데 실패하였습니다. label : {label}");
                    Addressables.Release(loadOp);
                    return null;
                }
                Addressables.Release(loadOp);
            }

            var results = new List<GameObject>(locations.Count);
            foreach (var loc in locations.Values)
            {
                var rcHandle = getHandleForLocation(loc);
                try { _ = rcHandle.Handle.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }

                var go = createInstanceFromHandle<ReleaseAddressableInstance>(rcHandle, loc.PrimaryKey, parent);
                if (go != null) results.Add(go);
            }

            return results;
        }

        public static List<T> ClonePrototypeFromLabelSync<T>(string label, Transform parent = null) where T : Component
        {
            if (string.IsNullOrEmpty(label)) return null;

            if (!_labelLoadOperationManager.TryGetValue(label, out var locations))
            {
                var loadOp = Addressables.LoadResourceLocationsAsync(label);
                try { _ = loadOp.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }

                if (loadOp.Status == AsyncOperationStatus.Succeeded)
                {
                    locations = loadOp.Result.ToDictionary(l => l.PrimaryKey);
                    _labelLoadOperationManager[label] = locations;
                }
                else
                {
                    Debug.LogWarning($"PrototypeManager : 라벨을 통해 에셋을 불러오는데 실패하였습니다. label : {label}");
                    Addressables.Release(loadOp);
                    return null;
                }
                Addressables.Release(loadOp);
            }

            var results = new List<T>(locations.Count);
            foreach (var loc in locations.Values)
            {
                var rcHandle = getHandleForLocation(loc);
                try { _ = rcHandle.Handle.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }

                var comp = createComponentInstanceFromHandle<T, ReleaseAddressableInstance>(rcHandle, loc.PrimaryKey, parent);
                if (comp != null) results.Add(comp);
            }

            return results;
        }

        public static GameObject ClonePrototypeFromLabelAndReferenceSync(string label, string reference, Transform parent = null)
        {
            // 기존 시그니처와 달리 동기 호출이므로 callback 파라미터는 무시하고 반환값으로 대체 (원본과 조금 다름)
            if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(reference)) return null;

            if (!_labelLoadOperationManager.TryGetValue(label, out var locations))
            {
                var loadOp = Addressables.LoadResourceLocationsAsync(label);
                try { _ = loadOp.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }

                if (loadOp.Status == AsyncOperationStatus.Succeeded)
                {
                    locations = loadOp.Result.ToDictionary(l => l.PrimaryKey);
                    _labelLoadOperationManager[label] = locations;
                }
                else
                {
                    Debug.LogWarning($"PrototypeManager : 라벨을 통해 에셋을 불러오는데 실패하였습니다. label : {label}");
                    Addressables.Release(loadOp);
                    return null;
                }
                Addressables.Release(loadOp);
            }

            if (!locations.TryGetValue(reference, out var loc))
            {
                Debug.LogWarning($"PrototypeManager : {label} 라벨이 붙은 레퍼런스 {reference}를 찾을 수 없습니다 Reference not found");
                return null;
            }

            var rcHandle = getHandleForLocation(loc);
            try { _ = rcHandle.Handle.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }

            if (rcHandle.Handle.Status == AsyncOperationStatus.Succeeded)
            {
                var go = createInstanceFromHandle<ReleaseAddressableInstance>(rcHandle, reference, parent);
                if (go != null) return go;
            }
#if DEBUG
            Debug.LogWarning($"Prototype Manager : 레퍼런스를 통해 리소스를 찾을 수 없습니다 {reference} Dictionary value not found");
#endif
            return null;
        }

        public static T ClonePrototypeFromLabelAndReferenceSync<T>(string label, string reference, Transform parent = null) where T : Component
        {
            if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(reference)) return null;

            if (!_labelLoadOperationManager.TryGetValue(label, out var locations))
            {
                var loadOp = Addressables.LoadResourceLocationsAsync(label);
                try { _ = loadOp.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }

                if (loadOp.Status == AsyncOperationStatus.Succeeded)
                {
                    locations = loadOp.Result.ToDictionary(l => l.PrimaryKey);
                    _labelLoadOperationManager[label] = locations;
                }
                else
                {
                    Debug.LogWarning($"PrototypeManager : 라벨을 통해 에셋을 불러오는데 실패하였습니다. label : {label}");
                    Addressables.Release(loadOp);
                    return null;
                }
                Addressables.Release(loadOp);
            }

            if (!locations.TryGetValue(reference, out var loc))
            {
                Debug.LogWarning($"PrototypeManager : {label} 라벨이 붙은 레퍼런스 {reference}를 찾을 수 없습니다 Reference not found");
                return null;
            }

            var rcHandle = getHandleForLocation(loc);
            try { _ = rcHandle.Handle.WaitForCompletion(); } catch (Exception ex) { Debug.LogException(ex); }

            if (rcHandle.Handle.Status == AsyncOperationStatus.Succeeded)
            {
                var comp = createComponentInstanceFromHandle<T, ReleaseAddressableInstance>(rcHandle, reference, parent);
                if (comp != null) return comp;
            }
#if DEBUG
            Debug.LogWarning($"Prototype Manager : 레퍼런스를 통해 리소스를 찾을 수 없습니다 {reference} Dictionary value not found");
#endif
            return null;
        }

        #endregion
    }
}
