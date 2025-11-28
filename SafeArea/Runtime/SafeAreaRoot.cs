using UnityEngine;

namespace Jeomseon.SafeArea
{
    /// <summary>
    /// RectTransform을 Safe Area에 맞게 자동으로 맞춰주는 컴포넌트.
    /// - 에디터/런타임 모두 동작(ExecuteAlways)
    /// - SafeAreaWatcher.SafeAreaChanged 이벤트를 구독해서 갱신
    /// </summary>
    [ExecuteAlways]
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
            // 에디터/런타임 공통으로 구독
            SafeAreaWatcher.SafeAreaChanged += OnSafeAreaChanged;

            // 현재 SafeArea 기준으로 한 번 즉시 적용
            ApplySafeArea(SafeAreaUtility.GetSafeArea());
        }

        private void OnDisable()
        {
            SafeAreaWatcher.SafeAreaChanged -= OnSafeAreaChanged;
        }

#if UNITY_EDITOR
        //// 인스펙터에서 값 바꿀 때도 바로 반영되도록
        //private void OnValidate()
        //{
        //    if (!isActiveAndEnabled)
        //        return;

        //    if (_rectTransform == null)
        //        _rectTransform = GetComponent<RectTransform>();

        //    ApplySafeArea(SafeAreaUtility.GetSafeArea());
        //}
#endif

        private void OnSafeAreaChanged(Rect safeArea)
        {
            // ApplySafeArea(safeArea);
        }

        private void ApplySafeArea(Rect safeArea)
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            Vector2 screenSize = SafeAreaUtility.GetScreenSize();
            SafeAreaUtility.GetInsets(safeArea, screenSize,
                out float left, out float right, out float top, out float bottom);

            // 화면 전체 기준
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
