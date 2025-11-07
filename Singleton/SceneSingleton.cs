using UnityEngine;

namespace Jeomseon.Singleton
{
    [DisallowMultipleComponent]
    public abstract class SingletonScene<T> : MonoBehaviour where T : SingletonScene<T>
    {
        private static T _instance;
        private bool _initialized;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
#if UNITY_2020_1_OR_NEWER
                    var found = FindObjectsOfType<T>(true);
#else
                    var found = Resources.FindObjectsOfTypeAll<T>();
#endif
                    if (found != null && found.Length > 0)
                    {
                        _instance = found[0];
                        _instance.EnsureInitialized();
                    }
                    else
                    {
                        var go = new GameObject(typeof(T).Name);
                        _instance = go.AddComponent<T>();
                        _instance.EnsureInitialized();
                    }
                }
                return _instance;
            }
        }

        protected void Awake()
        {
#if UNITY_2020_1_OR_NEWER
            var instances = FindObjectsOfType<T>(true);
#else
            var instances = Resources.FindObjectsOfTypeAll<T>();
#endif

            if (_instance == null)
            {
                _instance = (T)this;
                gameObject.name = typeof(T).Name;
                EnsureInitialized();
                // DontDestroyOnLoad 호출 안 함 → 씬 이동 시 같이 파괴
            }
            else if (_instance != this)
            {
                Destroy(this); // 중복 컴포넌트만 제거
                return;
            }

            if (instances != null && instances.Length > 1)
            {
                foreach (var inst in instances)
                {
                    if (inst == _instance) continue;
                    Destroy(inst);
                }
            }
        }

        private void OnDestroy()
        {
            // 현재 씬이 언로드되며 자신이 파괴될 때 새 씬에서 안전하게 재생성되도록 초기화
            if (ReferenceEquals(_instance, this))
                _instance = null;
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            Init();
        }

        protected abstract void Init();
    }
}
