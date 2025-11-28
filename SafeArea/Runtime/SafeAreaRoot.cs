using UnityEngine;

namespace Jeomseon.SafeArea
{
    /// <summary>
    /// RectTransform을 Safe Area에 맞게 자동으로 맞춰주는 컴포넌트.
    /// - 런타임: SafeAreaWatcher.SafeAreaChanged 이벤트를 구독해서 갱신
    /// - 에디터/프리뷰: Preview에서 직접 ApplyPreview 호출
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaRoot : MonoBehaviour
    {
        [SerializeField] private bool _applyLeft = true;
        [SerializeField] private bool _applyRight = true;
        [SerializeField] private bool _applyTop = true;
        [SerializeField] private bool _applyBottom = true;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            SafeAreaWatcher.SafeAreaChanged += OnSafeAreaChanged;

            // 현재 SafeArea 기준으로 한 번 즉시 적용 (런타임 기준)
            ApplySafeArea(SafeAreaUtility.GetSafeArea());
        }

        private void OnDisable()
        {
            SafeAreaWatcher.SafeAreaChanged -= OnSafeAreaChanged;
        }

        private void OnSafeAreaChanged(Rect safeArea)
        {
            ApplySafeArea(safeArea);
        }

        /// <summary>
        /// 런타임/일반용: SafeAreaUtility의 ScreenSize 기준으로 적용.
        /// </summary>
        private void ApplySafeArea(Rect safeArea)
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            Vector2 screenSize = SafeAreaUtility.GetScreenSize();

#if UNITY_EDITOR
            // 에디터에서 GameView가 없어서 0,0일 수 있는 상황 방어
            if (screenSize.x <= 0f || screenSize.y <= 0f)
            {
                var canvas = _rectTransform.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    var rect = canvas.pixelRect;
                    if (rect.width > 0f && rect.height > 0f)
                    {
                        screenSize = rect.size;
                    }
                }
            }
#endif

            if (screenSize.x <= 0f || screenSize.y <= 0f)
                return;

            ApplyInternal(safeArea, screenSize);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터 PreviewScene 전용: 외부에서 safeArea + screenSize를 직접 지정해서 적용.
        /// 원본 씬의 SafeAreaUtility / Watcher와는 무관하게 동작.
        /// </summary>
        internal void ApplyPreview(Rect safeArea, Vector2 screenSize)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f)
                return;

            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            ApplyInternal(safeArea, screenSize);
        }
#endif

        private void ApplyInternal(Rect safeArea, Vector2 screenSize)
        {
            SafeAreaUtility.GetInsets(safeArea, screenSize,
                out float left, out float right, out float top, out float bottom);

            float xMin = 0f;
            float xMax = screenSize.x;
            float yMin = 0f;
            float yMax = screenSize.y;

            if (_applyLeft)
                xMin = left;
            if (_applyRight)
                xMax = screenSize.x - right;
            if (_applyBottom)
                yMin = bottom;
            if (_applyTop)
                yMax = screenSize.y - top;

            Rect target = Rect.MinMaxRect(xMin, yMin, xMax, yMax);

            Vector2 anchorMin = new Vector2(
                target.xMin / screenSize.x,
                target.yMin / screenSize.y
            );
            Vector2 anchorMax = new Vector2(
                target.xMax / screenSize.x,
                target.yMax / screenSize.y
            );

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
