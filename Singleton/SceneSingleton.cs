using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Jeomseon.Extensions;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Jeomseon.Singleton
{
    /// <summary>
    /// .. SceneName이 일치하는 씬에서만 해당 싱글톤이 존재할 수 있도록 하는 제네릭 싱글톤 패턴입니다
    /// 전역적인 접근이 가능합니다
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DisallowMultipleComponent]
    public abstract class SceneSingleton<T> : MonoBehaviour where T : SceneSingleton<T>
    {
        private static T _instance = null;
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

        /// <summary>
        /// .. 특정 씬에만 존재해야하는 싱글톤이므로 올바르지 않은 씬에서 생성될경우 Destroy
        /// </summary>
        protected abstract string SceneName { get; }

        protected void Awake() // protected로 상속 클래스에서 재정의시 경고문 띄워주기
        {
            if (SceneManager.GetActiveScene().name != SceneName)
            {
                Destroy(gameObject);
                return;
            }

            T[] instances = FindObjectsOfType<T>(); // .. 씬에 미리 로드되어있는 경우

            if (_instance is null)
            {
                _instance = instances[0];
                _instance.name = typeof(T).ToString();
                _instance.Init();
            }

            if (instances.Length > 1)
            {
                // .. 하이어라키에 여러개의 싱글톤 객체가 존재할 경우 모두 삭제 .. 같은 오브젝트에 여러개의 싱글톤 객체가 있을 경우 위험할 수 있음
                instances
                    .Where(instance => instance != _instance)
                    .ForEach(instance => Destroy(instance.gameObject));
            }
        }

        private void OnDestroy()
        {
            if (_instance != this) return;

            _instance = null;
        }

        /// <summary>
        /// .. Awake 대체용 메서드
        /// </summary>
        protected abstract void Init();
    }
}
