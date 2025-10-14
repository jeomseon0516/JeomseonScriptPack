using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Jeomseon.Extensions;

namespace Jeomseon.ObjectPool
{
    public interface IPooledObject : IPopHandler, IReturnHandler {}

    public interface IPopHandler
    {
        void OnPopFromPool();
    }

    public interface IReturnHandler
    {
        void OnReturnToPool();
    }

    public static class GenericObjectPool<T> where T : Component
    {
        private static readonly Stack<T> _pool = new();
        private static GameObject _poolParent = null;

        private static GameObject getPoolParent()
        {
            if (_poolParent == null)
            {
                _poolParent = new(nameof(GenericObjectPool<T>));
                Object.DontDestroyOnLoad(_poolParent);
            }

            return _poolParent;
        }

        public static bool TryPop(out T genericComponent)
        {
            if (_pool.TryPop(out genericComponent) && genericComponent)
            {
                onPop(genericComponent);
                return true;
            }
            return false;
        }

        public static T Pop()
        {
            if (!_pool.TryPop(out T genericComponent) || !genericComponent) return null;

            onPop(genericComponent);
            return genericComponent;
        }

        public static void Return(T genericComponent)
        {
            if (!genericComponent) return;
            
            if (genericComponent is IReturnHandler pooledObject)
            {
                pooledObject.OnReturnToPool();
            }
            
            genericComponent.gameObject.SetActive(false);
            genericComponent.transform.SetParent(getPoolParent().transform, false);
            PoolInitializer.Initialize(genericComponent);
            
            _pool.Push(genericComponent);
        }

        public static void ReleaseAllObject()
        {
            _pool.ForEach(component => Object.Destroy(component.gameObject));
            _pool.Clear();
        }

        private static void onPop(T genericComponent)
        {
            genericComponent.transform.SetParent(null, false);
            genericComponent.gameObject.SetActive(true);
            if (genericComponent is IPopHandler pooledObject)
            {
                pooledObject.OnPopFromPool();
            }
        }
    }
    
    public static class KeyedObjectPool
    {
        private static readonly Dictionary<string, Stack<GameObject>> _pool = new();
        private static GameObject _poolParent = null;

        private static GameObject getPoolParent()
        {
            if (_poolParent == null)
            {
                _poolParent = new(nameof(KeyedObjectPool));
                Object.DontDestroyOnLoad(_poolParent);
            }

            return _poolParent;
        }

        public static bool TryPop(string key, out GameObject pooledObject)
        {
            pooledObject = null;
            
            if (_pool.TryGetValue(key, out Stack<GameObject> stack))
            {
                while (stack.TryPop(out pooledObject))
                {
                    if (!pooledObject) continue;

                    onPop(pooledObject);
                    return true;
                }
            }

            return false;
        }

        public static GameObject Pop(string key)
        {
            if (!_pool.TryGetValue(key, out Stack<GameObject> stack) || stack.Count <= 0) return null;

            while (stack.TryPop(out GameObject pooledObject))
            {
                if (!pooledObject) continue;

                onPop(pooledObject);
                return pooledObject;
            }

            return null;
        }
        
        public static void Return(string key, GameObject objectToReturn)
        {
            if (string.IsNullOrEmpty(key) || !objectToReturn) return;
            
            if (!_pool.TryGetValue(key, out Stack<GameObject> stack))
            {
                stack = new();
                _pool.Add(key, stack);
            }

            objectToReturn
                .GetComponents<IReturnHandler>()
                .ForEach(pooledComponent => pooledComponent.OnReturnToPool());
            
            objectToReturn.SetActive(false);
            objectToReturn.transform.SetParent(getPoolParent().transform, false);
            PoolInitializer.Initialize(objectToReturn);

            stack.Push(objectToReturn);
        }
        
        public static void ReleaseAllObject()
        {
            _pool
                .Values
                .SelectMany(stack => stack)
                .Where(obj => obj)
                .ForEach(Object.Destroy);

            _pool.Clear();
        }
        
        private static void onPop(GameObject pooledObject)
        {
            pooledObject.transform.SetParent(null, false);
            pooledObject.SetActive(true);
            pooledObject
                .GetComponents<IPopHandler>()
                .ForEach(pooledComponent => pooledComponent.OnPopFromPool());
        }
    }
    
    public static class GenericKeyedObjectPool<T> where T : Component
    {
        private static readonly Dictionary<string, Stack<T>> _pool = new();
        private static GameObject _poolParent = null;

        private static GameObject getPoolParent()
        {
            if (_poolParent == null)
            {
                _poolParent = new(nameof(GenericKeyedObjectPool<T>));
                Object.DontDestroyOnLoad(_poolParent);
            }

            return _poolParent;
        }

        public static bool TryPop(string key, out T genericComponent)
        {
            genericComponent = null;
            if (_pool.TryGetValue(key, out Stack<T> stack))
            {
                while (stack.TryPop(out genericComponent))
                {
                    if (!genericComponent) continue;

                    onPop(genericComponent);
                    return true;
                }
            }
            
            return false;
        }

        public static T Pop(string key)
        {
            if (!_pool.TryGetValue(key, out Stack<T> stack) || stack.Count <= 0) return null;

            while (stack.TryPop(out T genericComponent))
            {
                if (!genericComponent) continue;

                onPop(genericComponent);
                return genericComponent;
            }

            return null;
        }
        
        public static void Return(string key, T objectToReturn)
        {
            if (string.IsNullOrEmpty(key) || !objectToReturn) return;
            
            if (!_pool.TryGetValue(key, out Stack<T> stack))
            {
                stack = new();
                _pool.Add(key, stack);
            }

            if (objectToReturn is IReturnHandler pooledObject)
            {
                pooledObject.OnReturnToPool();
            }
            
            objectToReturn.gameObject.SetActive(false);
            objectToReturn.transform.SetParent(getPoolParent().transform, false);
            PoolInitializer.Initialize(objectToReturn);

            stack.Push(objectToReturn);
        }
        
        public static void ReleaseAllObject()
        {
            _pool
                .Values
                .SelectMany(stack => stack)
                .Where(obj => obj)
                .ForEach(Object.Destroy);

            _pool.Clear();
        }
        
        private static void onPop(T genericComponent)
        {
            genericComponent.transform.SetParent(null, false);
            genericComponent.gameObject.SetActive(true);

            if (genericComponent is IPopHandler pooledObject)
            {
                pooledObject.OnPopFromPool();
            }
        }
    }
}
