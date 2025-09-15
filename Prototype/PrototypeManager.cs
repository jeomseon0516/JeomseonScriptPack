using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Jeomseon.Singleton;

namespace Jeomseon.Prototype
{
    using Coroutine = UnityEngine.Coroutine;

    /// <summary>
    /// ..  프로토타입 매니저는 어드레서블 에셋 시스템을 사용하여 런타임중 동적으로 생성하거나 불러와야할 리소스들에 대한 접근을 위한 클래스입니다.
    /// 비동기 인터페이스 접근 시 코루틴을 제공합니다 하지만 StopCoroutine등으로 메서드의 동작을 멈추게할 시 핸들이 Release되지 않아 메모리 릭이 발생할 수 있습니다.
    /// </summary>
    public sealed class PrototypeManager : Singleton<PrototypeManager>
    {
        private readonly Dictionary<string, RefCountingOperationHandle<GameObject>> _gameObjectHandles = new();
        // .. 레퍼런스로 구해오는 값은 Group에 들어있는 Key값을 유추할 수 없기 때문에 RuntimeKey라는 AssetReference의 고유 키값으로 따로 관리합니다
        private readonly Dictionary<string, RefCountingOperationHandle<GameObject>> _gameObjectFromReferenceHandles = new();

        internal void ReleaseInstance(string primaryKey)
            => _gameObjectHandles.ReleaseInstance(primaryKey);

        internal void ReleaseInstanceFromRuntimeKey(string runtimeKey)
            => _gameObjectFromReferenceHandles.ReleaseInstance(runtimeKey);

        private static GameObject getSuccessObject<T>(
            Dictionary<string, RefCountingOperationHandle<GameObject>> handles,
            string key,
            RefCountingOperationHandle<GameObject> resourceOperationHandle,
            Transform parent) where T : ReleaseAddressableInstance
        {
            GameObject resource = null;

            if (resourceOperationHandle.Handle.IsValid() && resourceOperationHandle.Handle.Status == AsyncOperationStatus.Succeeded)
            {
                resource = Instantiate(resourceOperationHandle.Handle.Result, parent);
                resource.AddComponent<T>().PrimaryKey = key;
            }
            else
            {
                handles.ReleaseInstance(key);
            }

            return resource;
        }

        private static T getSuccessesComponent<T, TR>(
            Dictionary<string, RefCountingOperationHandle<GameObject>> handles,
            string key,
            RefCountingOperationHandle<GameObject> resourceOperationHandle,
            Transform parent) where T : MonoBehaviour where TR : ReleaseAddressableInstance
        {
            T cloneComponent = null;

            if (resourceOperationHandle.Handle.IsValid() &&
                resourceOperationHandle.Handle.Status == AsyncOperationStatus.Succeeded &&
                resourceOperationHandle.Handle.Result.TryGetComponent(out T component))
            {
                cloneComponent = Instantiate(component, parent);
                cloneComponent.gameObject.AddComponent<TR>().PrimaryKey = key;
            }
            else
            {
                handles.ReleaseInstance(key);
            }

            return cloneComponent;
        }

        #region 비동기 인터페이스
        /// <summary>
        /// .. 키값에 해당하는 리소스를 비동기적으로 로드합니다.
        /// </summary>
        /// <typeparam name="T"> .. 리소스의 타입 만약 불러올 리소스가 T가 아니라면 불러오지 않습니다 </typeparam>
        /// <param name="key"> .. 불러올 리소스의 키값 </param>
        /// <param name="parent"> .. 부모 트랜스폼 설정하지 않으면 부모를 설정하지 않습니다 </param>
        public Coroutine ClonePrototypeAsync<T>(string key, Action<T> callback, Transform parent = null) where T : MonoBehaviour
            => StartCoroutine(iEClonePrototypeAsync(key, callback, parent));

        /// <summary>
        /// .. 키값에 해당하는 리소스를 비동기적으로 로드합니다.
        /// </summary>
        /// <param name="key"> .. 불러올 리소스의 키값 </param>
        /// <param name="parent"> .. 부모 트랜스폼 설정하지 않으면 부모를 설정하지 않습니다 </param>
        public Coroutine ClonePrototypeAsync(string key, Action<GameObject> callback, Transform parent = null)
            => StartCoroutine(iEClonePrototypeAsync(key, callback, parent));

        /// <summary>
        /// .. 라벨에 해당하는 모든 리소스들을 비동기적으로 로드합니다 callback메서드로 리소스를 받아오며 리소스가 없거나 실패하면 null을 가져옵니다.
        /// </summary>
        /// <typeparam name="T"> .. 어떤 타입을 불러올것인지 판단합니다 타입에 해당하지 않는다면 리스트에서 제외시킵니다 </typeparam>
        /// <param name="label"> .. 불러올 라벨 </param>
        /// <param name="callback"> .. 라벨을 통해 불러올 리소스들을 콜백으로 받아옵니다 존재하지않으면 null을 가져옵니다 </param>
        public Coroutine ClonePrototypeFromLabelAsync<T>(string label, Action<T> callback, Transform parent = null) where T : MonoBehaviour
            => StartCoroutine(iEClonePrototypeFromLabelAsync(label, callback, parent));

        /// <summary>
        /// .. 라벨에 해당하는 모든 리소스들을 비동기적으로 로드합니다 callback메서드로 리소스를 받아오며 리소스가 없거나 실패하면 null을 가져옵니다.
        /// </summary>
        /// <param name="label"> .. 불러올 라벨 </param>
        /// <param name="callback"> .. 라벨을 통해 불러올 리소스들을 콜백으로 받아옵니다 존재하지않으면 null을 가져옵니다 </param>
        public Coroutine ClonePrototypeFromLabelAsync(string label, Action<GameObject> callback, Transform parent = null)
            => StartCoroutine(iEClonePrototypeFromLabelAsync(label, callback, parent));

        /// <summary>
        /// .. 에셋 참조로 리소스를 비동기적으로 로드합니다 callback메서드로 리소스를 받아오며 리소스가 없거나 실패하면 null을 가져옵니다.
        /// </summary>
        /// <param name="assetReference"> .. 불러올 라벨 </param>
        /// <param name="callback"> .. 라벨을 통해 불러올 리소스들을 콜백으로 받아옵니다 존재하지않으면 null을 가져옵니다 </param>
        public Coroutine ClonePrototypeFromReferenceAsync<T>(AssetReferenceGameObject assetReference, Action<T> callback, Transform parent = null) where T : MonoBehaviour
            => StartCoroutine(iEClonePrototypeFromReferenceAsync(assetReference, callback, parent));

        /// <summary>
        /// .. 에셋 참조로 리소스를 비동기적으로 로드합니다 callback메서드로 리소스를 받아오며 리소스가 없거나 실패하면 null을 가져옵니다.
        /// </summary>
        /// <param name="assetReference"> .. 불러올 라벨 </param>
        /// <param name="callback"> .. 라벨을 통해 불러올 리소스들을 콜백으로 받아옵니다 존재하지않으면 null을 가져옵니다 </param>
        public Coroutine ClonePrototypeFromReferenceAsync(AssetReferenceGameObject assetReference, Action<GameObject> callback, Transform parent = null)
            => StartCoroutine(iEClonePrototypeFromReferenceAsync(assetReference, callback, parent));
        #endregion
        #region 동기 인터페이스
        /// <summary>
        /// .. 키값에 해당하는 리소스를 동기적으로 로드합니다. 로드가 될때까지 메인스레드에서 대기합니다
        /// </summary>
        /// <typeparam name="T"> .. 리소스의 타입 </typeparam>
        /// <param name="key"> .. 불러올 리소스의 키값 </param>
        /// <param name="parent"> .. 부모 트랜스폼 설정하지 않으면 부모를 설정하지 않습니다 </param>
        /// <returns> .. 클론된 리소스를 리턴합니다 </returns>
        public T ClonePrototypeSync<T>(string key, Transform parent = null) where T : MonoBehaviour
        {
            RefCountingOperationHandle<GameObject> resourceOperationHandle = _gameObjectHandles.GetHandle(key);
            _ = resourceOperationHandle.Handle.WaitForCompletion();

            return getSuccessesComponent<T, ReleaseAddressableInstance>(
                _gameObjectHandles, key, resourceOperationHandle, parent);
        }

        /// <summary>
        /// .. 키값에 해당하는 리소스를 동기적으로 로드합니다. 로드가 될때까지 메인스레드에서 대기합니다
        /// </summary>
        /// <param name="key"> .. 불러올 리소스의 키값 </param>
        /// <param name="parent"> .. 부모 트랜스폼 설정하지 않으면 부모를 설정하지 않습니다 </param>
        /// <returns> .. 클론된 리소스를 리턴합니다 </returns>
        public GameObject ClonePrototypeSync(string key, Transform parent = null)
        {
            RefCountingOperationHandle<GameObject> resourceOperationHandle = _gameObjectHandles.GetHandle(key);
            _ = resourceOperationHandle.Handle.WaitForCompletion();

            return getSuccessObject<ReleaseAddressableInstance>(
                _gameObjectHandles, key, resourceOperationHandle, parent);
        }

        /// <summary>
        /// .. 라벨에 해당하는 모든 리소스들을 로드합니다 메인스레드에서 로드 될때까지 대기합니다.
        /// </summary>
        /// <typeparam name="T"> .. 어떤 타입을 불러올것인지 판단합니다 </typeparam>
        /// <param name="label"> .. 불러올 라벨 </param>
        /// <returns> .. 라벨을 통해 불러올 리소드들 입니다. 존재하지않으면 null을 리턴합니다 </returns>
        public List<T> ClonePrototypeFromLabelSync<T>(string label, Transform parent = null) where T : MonoBehaviour
        {
            AsyncOperationHandle<IList<IResourceLocation>> loadOperation = Addressables.LoadResourceLocationsAsync(label);
            _ = loadOperation.WaitForCompletion();
            List<T> values = null;

            if (loadOperation.Status == AsyncOperationStatus.Succeeded)
            {
                values = new(loadOperation.Result.Count);

                foreach (IResourceLocation resourceLocation in loadOperation.Result)
                {
                    RefCountingOperationHandle<GameObject> resourceOperationHandle = _gameObjectHandles.GetHandle(resourceLocation);
                    _ = resourceOperationHandle.Handle.WaitForCompletion();

                    T component = getSuccessesComponent<T, ReleaseAddressableInstance>(
                        _gameObjectHandles, resourceLocation.PrimaryKey, resourceOperationHandle, parent);

                    if (component)
                    {
                        values.Add(component);
                    }
                }
            }
            else
            {
#if DEBUG
                Debug.LogWarning("Label로 리소스를 찾아올 수 없습니다");
#endif
            }

            Addressables.Release(loadOperation);

            return values;
        }

        /// <summary>
        /// .. 라벨에 해당하는 모든 리소스들을 로드합니다 메인스레드에서 로드 될때까지 대기합니다.
        /// </summary>
        /// <param name="label"> .. 불러올 라벨 </param>
        /// <returns> .. 라벨을 통해 불러올 리소드들 입니다. 존재하지않으면 null을 리턴합니다 </returns>
        public List<GameObject> ClonePrototypeFromLabelSync(string label, Transform parent = null)
        {
            AsyncOperationHandle<IList<IResourceLocation>> loadOperation = Addressables.LoadResourceLocationsAsync(label);
            _ = loadOperation.WaitForCompletion();
            List<GameObject> values = null;

            if (loadOperation.Status == AsyncOperationStatus.Succeeded)
            {
                values = new(loadOperation.Result.Count);

                foreach (IResourceLocation resourceLocation in loadOperation.Result)
                {
                    RefCountingOperationHandle<GameObject> resourceOperationHandle = _gameObjectHandles.GetHandle(resourceLocation);
                    _ = resourceOperationHandle.Handle.WaitForCompletion();

                    GameObject resource = getSuccessObject<ReleaseAddressableInstance>(
                        _gameObjectHandles, resourceLocation.PrimaryKey, resourceOperationHandle, parent);

                    if (resource)
                    {
                        values.Add(resource);
                    }
                }
            }
            else
            {
#if DEBUG
                Debug.LogWarning("Label로 리소스를 찾아올 수 없습니다");
#endif
            }

            Addressables.Release(loadOperation);

            return values;
        }

        public T ClonePrototypeFromReferenceSync<T>(AssetReferenceGameObject assetReference, Transform parent = null) where T : MonoBehaviour
        {
            RefCountingOperationHandle<GameObject> resourceOperationHandle = _gameObjectFromReferenceHandles.GetHandle(assetReference);
            _ = resourceOperationHandle.Handle.WaitForCompletion();

            return getSuccessesComponent<T, ReleaseAddressableInstanceFromAssetReference>(
                _gameObjectFromReferenceHandles, assetReference.RuntimeKey.ToString(), resourceOperationHandle, parent);
        }

        public GameObject ClonePrototypeFromReferenceSync(AssetReferenceGameObject assetReference, Transform parent = null)
        {
            RefCountingOperationHandle<GameObject> resourceOperationHandle = _gameObjectFromReferenceHandles.GetHandle(assetReference);
            _ = resourceOperationHandle.Handle.WaitForCompletion();

            return getSuccessObject<ReleaseAddressableInstanceFromAssetReference>(
                _gameObjectFromReferenceHandles, assetReference.RuntimeKey.ToString(), resourceOperationHandle, parent);
        }
        #endregion
        #region 코루틴 비동기

        private IEnumerator iEClonePrototypeAsync<T>(string key, Action<T> callback, Transform parent) where T : MonoBehaviour
        {
            RefCountingOperationHandle<GameObject> resourceOperationHandle = _gameObjectHandles.GetHandle(key);

            yield return resourceOperationHandle.Handle;

            callback?.Invoke(getSuccessesComponent<T, ReleaseAddressableInstance>(
                _gameObjectHandles, key, resourceOperationHandle, parent));
        }

        private IEnumerator iEClonePrototypeAsync(string key, Action<GameObject> callback, Transform parent)
        {
            RefCountingOperationHandle<GameObject> resourceOperationHandle = _gameObjectHandles.GetHandle(key);

            yield return resourceOperationHandle.Handle;

            callback?.Invoke(getSuccessObject<ReleaseAddressableInstance>(
                _gameObjectHandles, key, resourceOperationHandle, parent));
        }

        private IEnumerator iEClonePrototypeFromLabelAsync<T>(string label, Action<T> callback, Transform parent) where T : MonoBehaviour
        {
            AsyncOperationHandle<IList<IResourceLocation>> loadOperation = Addressables.LoadResourceLocationsAsync(label);
            yield return loadOperation;

            if (loadOperation.Status == AsyncOperationStatus.Succeeded)
            {
                foreach (IResourceLocation resourceLocation in loadOperation.Result)
                {
                    RefCountingOperationHandle<GameObject> resourceOperationHandle = _gameObjectHandles.GetHandle(resourceLocation);

                    yield return resourceOperationHandle.Handle;

                    callback?.Invoke(getSuccessesComponent<T, ReleaseAddressableInstance>(
                        _gameObjectHandles, resourceLocation.PrimaryKey, resourceOperationHandle, parent));
                }
            }
            else
            {
#if DEBUG
                Debug.LogWarning("Label로 리소스를 찾아올 수 없습니다");
#endif
            }

            Addressables.Release(loadOperation);
        }

        private IEnumerator iEClonePrototypeFromLabelAsync(string label, Action<GameObject> callback, Transform parent)
        {
            AsyncOperationHandle<IList<IResourceLocation>> loadOperation = Addressables.LoadResourceLocationsAsync(label);
            yield return loadOperation;

            if (loadOperation.Status == AsyncOperationStatus.Succeeded)
            {
                foreach (IResourceLocation t in loadOperation.Result)
                {
                    RefCountingOperationHandle<GameObject> resourceOperationHandle = _gameObjectHandles.GetHandle(t);

                    yield return resourceOperationHandle.Handle;

                    callback?.Invoke(getSuccessObject<ReleaseAddressableInstance>(
                        _gameObjectHandles, t.PrimaryKey, resourceOperationHandle, parent));
                }
            }
            else
            {
#if DEBUG
                Debug.LogWarning("Label로 리소스를 찾아올 수 없습니다");
#endif
            }

            Addressables.Release(loadOperation);
        }

        private IEnumerator iEClonePrototypeFromReferenceAsync<T>(AssetReferenceGameObject assetReference, Action<T> callback, Transform parent) where T : MonoBehaviour
        {
            RefCountingOperationHandle<GameObject> resourceOperationHandle = _gameObjectFromReferenceHandles.GetHandle(assetReference);

            yield return resourceOperationHandle.Handle;

            callback?.Invoke(getSuccessesComponent<T, ReleaseAddressableInstanceFromAssetReference>(
                _gameObjectFromReferenceHandles, assetReference.RuntimeKey.ToString(), resourceOperationHandle, parent));
        }

        private IEnumerator iEClonePrototypeFromReferenceAsync(AssetReferenceGameObject assetReference, Action<GameObject> callback, Transform parent)
        {
            RefCountingOperationHandle<GameObject> resourceOperationHandle = _gameObjectFromReferenceHandles.GetHandle(assetReference);

            yield return resourceOperationHandle.Handle;

            callback?.Invoke(getSuccessObject<ReleaseAddressableInstanceFromAssetReference>(
                _gameObjectFromReferenceHandles, assetReference.RuntimeKey.ToString(), resourceOperationHandle, parent));
        }
        #endregion
        protected override void Init() { }
    }
}