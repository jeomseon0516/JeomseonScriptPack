// Assets/Jeomseon/SafeArea/Runtime/SafeAreaWatcher.cs
using UnityEngine;
using System;

namespace Jeomseon.SafeArea
{
    /// <summary>
    /// Static watcher for safe area / screen size changes.
    /// - 런타임: Application.onBeforeRender에서 자동 체크
    /// - 에디터: SafeAreaPreviewWindow 등에서 ForceUpdate()를 호출해 수동 트리거
    /// 
    /// 이 클래스는 카메라를 직접 건드리지 않고,
    /// SafeAreaChanged 이벤트만 브로드캐스트한다.
    /// 구독자는 SafeAreaRoot, SafeAreaPadding 등.
    /// </summary>
    public static class SafeAreaWatcher
    {
        /// <summary>
        /// SafeArea 또는 화면 크기가 변경되었을 때 호출되는 이벤트.
        /// Rect 인자는 새 SafeArea 값(Screen 좌표 기준, px).
        /// </summary>
        public static event Action<Rect> SafeAreaChanged;

        private static bool _initialized;
        private static Rect _lastSafeArea;
        private static Vector2 _lastScreenSize;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitOnPlay()
        {
            InitInternal();
        }

        /// <summary>
        /// 외부에서 명시적으로 초기화가 필요할 때 호출 가능.
        /// (보통은 RuntimeInitializeOnLoadMethod로 충분함)
        /// </summary>
        public static void Init()
        {
            InitInternal();
        }

        private static void InitInternal()
        {
            if (_initialized)
                return;

            _initialized = true;

            _lastSafeArea = SafeAreaUtility.GetSafeArea();
            _lastScreenSize = SafeAreaUtility.GetScreenSize();

            // 런타임에서 매 프레임 직전에 호출되는 훅
            Application.onBeforeRender -= CheckForChanges;
            Application.onBeforeRender += CheckForChanges;

            // 초기 상태도 한 번 브로드캐스트 해주고 싶다면 아래 주석 해제
            // SafeAreaChanged?.Invoke(_lastSafeArea);
        }

        private static void CheckForChanges()
        {
            var safe = SafeAreaUtility.GetSafeArea();
            var size = SafeAreaUtility.GetScreenSize();

            if (safe != _lastSafeArea || size != _lastScreenSize)
            {
                _lastSafeArea = safe;
                _lastScreenSize = size;

                SafeAreaChanged?.Invoke(_lastSafeArea);
            }
        }

        /// <summary>
        /// 외부(에디터 툴 등)에서 강제로 "현재 SafeArea 상태"를 브로드캐스트하고 싶을 때 사용.
        /// 에디터에서도 안전하게 호출 가능.
        /// </summary>
        public static void ForceUpdate()
        {
            InitInternal(); // onBeforeRender 등록까지 보장

            _lastSafeArea = SafeAreaUtility.GetSafeArea();
            _lastScreenSize = SafeAreaUtility.GetScreenSize();

            SafeAreaChanged?.Invoke(_lastSafeArea);
        }
    }
}
