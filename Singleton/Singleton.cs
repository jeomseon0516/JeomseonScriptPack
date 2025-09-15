using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Singleton
{
    /// <summary>
    /// .. Awake를 자식에서 재정의할시 싱글톤의 단일객체라는 기능을 상실하게 됩니다. Awake대신 Init함수를 정의하여 사용해야합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T _instance = null;
        public static T Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = FindObjectOfType<T>() ?? new GameObject(typeof(T).Name).AddComponent<T>();
                    _instance.Init();
                }

                return _instance;
            }
        }

        protected void Awake() // .. protected로 자식클래스에 경고문 띄워주기
        {
            T[] instances = FindObjectsOfType<T>(); // .. 씬에 미리 로드되어있는 경우

            if (_instance is null)
            {
                _instance = instances[0];
                _instance.name = typeof(T).Name;
                _instance.Init();
            }

            if (instances.Length > 1)
            {
                // .. 하이어라키에 여러개의 싱글톤 객체가 존재할 경우 모두 삭제 .. 같은 오브젝트에 여러개의 싱글톤 객체가 있을 경우 위험할 수 있음
                foreach (T instance in instances)
                {
                    if (instance == _instance) continue;

                    Destroy(instance.gameObject);
                }
            }

            DontDestroyOnLoad(gameObject);
        }

        protected abstract void Init();
        protected Singleton() { }
    }
}