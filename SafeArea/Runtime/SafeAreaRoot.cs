using UnityEngine;

namespace Jeomseon.SafeArea
{
    /// <summary>
    /// Applies safe area to the attached RectTransform by adjusting anchors/offsets.
    /// You can choose which edges (left/right/top/bottom) should respect the safe area.
    /// This is an alternative to camera-based SafeArea, useful for specific UI panels.
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
            OnSafeAreaChanged(SafeAreaUtility.GetSafeArea());
        }

        private void OnDisable()
        {
            SafeAreaWatcher.SafeAreaChanged -= OnSafeAreaChanged;
        }

        private void OnSafeAreaChanged(Rect safeArea)
        {
            ApplySafeArea(safeArea);
        }

        private void ApplySafeArea(Rect safeArea)
        {
            Vector2 screenSize = SafeAreaUtility.GetScreenSize();
            SafeAreaUtility.GetInsets(safeArea, screenSize, out float left, out float right, out float top, out float bottom);

            // Start with full-screen rect
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
