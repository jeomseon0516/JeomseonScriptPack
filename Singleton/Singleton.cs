using UnityEngine;

namespace Jeomseon.Singleton
{
    [DisallowMultipleComponent]
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T _instance;
        private static bool _quitting;
        private bool _initialized;

        public static T Instance
        {
            get
            {
                if (_quitting) return null; // 종료 중엔 생성/접근 회피

                if (_instance == null)
                {
                    // Unity 2020.1+ 에서는 비활성 포함 검색 오버로드가 있습니다.
#if UNITY_2020_1_OR_NEWER
                    var found = FindObjectsOfType<T>(true);
#else
                    // 구버전 대응: 에디터 프리팹까지 잡히는 건 주의가 필요합니다.
                    // 필요시 아래 라인을 FindObjectOfType<T>()로 낮추고, 비활성 케이스는 설계로 회피하세요.
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

        // Awake는 되도록 건드리지 않게 하고, 실수 방지를 위해 protected internal로 공개
        protected internal void Awake()
        {
            // 자신 포함 모든 중복 수집
#if UNITY_2020_1_OR_NEWER
            var instances = FindObjectsOfType<T>(true);
#else
            var instances = Resources.FindObjectsOfTypeAll<T>();
#endif

            if (_instance == null)
            {
                _instance = (T)this;
                gameObject.name = typeof(T).Name; // 통일된 이름
                EnsureInitialized();
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                // 같은 GameObject에 T가 여러 개 붙은 경우 등 부작용 최소화를 위해 "컴포넌트만" 제거
                // 단, 이미 DontDestroyOnLoad 된 루트와 경합 시 현재 컴포넌트만 제거
                Destroy(this);
                return;
            }

            // 혹시 모를 중복 컴포넌트들 정리 (자기 자신 제외)
            if (instances != null && instances.Length > 1)
            {
                foreach (var inst in instances)
                {
                    if (inst == _instance) continue;
                    // 같은 오브젝트에 여러 개 붙어 있어도 안전하게 컴포넌트만 제거
                    Destroy(inst);
                }
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _quitting = true;
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            Init();
            DontDestroyOnLoad(gameObject); // 보장
        }

        // 자식이 반드시 구현
        protected abstract void Init();

        // 외부에서 new() 불가하지만, 명시적 기본 생성자 유지
        protected Singleton() { }
    }
}
