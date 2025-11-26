using UnityEngine;
using UnityEngine.UI;

namespace Jeomseon.SafeArea
{
    /// <summary>
    /// LayoutGroup의 padding에 Safe Area 인셋을 더해주는 컴포넌트.
    /// - 에디터/런타임 모두 동작(ExecuteAlways)
    /// - Top/Bottom/Left/Right 방향별로 적용 여부 선택 가능.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LayoutGroup))]
    public sealed class SafeAreaPadding : MonoBehaviour
    {
        [SerializeField] private bool _useLeft;
        [SerializeField] private bool _useRight;
        [SerializeField] private bool _useTop = true;
        [SerializeField] private bool _useBottom;

        private LayoutGroup _layoutGroup;
        private RectOffset _originalPadding;
        private Canvas _canvas;
        private bool _initializedOriginal;

        private void Awake()
        {
            _layoutGroup = GetComponent<LayoutGroup>();
            CacheOriginalPadding();
            _canvas = GetComponentInParent<Canvas>();
        }

        private void CacheOriginalPadding()
        {
            if (_layoutGroup == null)
                _layoutGroup = GetComponent<LayoutGroup>();

            if (_layoutGroup != null && !_initializedOriginal)
            {
                _originalPadding = new RectOffset(
                    _layoutGroup.padding.left,
                    _layoutGroup.padding.right,
                    _layoutGroup.padding.top,
                    _layoutGroup.padding.bottom
                );
                _initializedOriginal = true;
            }
        }

        private void OnEnable()
        {
            SafeAreaWatcher.SafeAreaChanged += OnSafeAreaChanged;
            CacheOriginalPadding();
            OnSafeAreaChanged(SafeAreaUtility.GetSafeArea());
        }

        private void OnDisable()
        {
            SafeAreaWatcher.SafeAreaChanged -= OnSafeAreaChanged;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!isActiveAndEnabled)
                return;

            _layoutGroup = GetComponent<LayoutGroup>();
            _canvas = GetComponentInParent<Canvas>();
            CacheOriginalPadding();

            OnSafeAreaChanged(SafeAreaUtility.GetSafeArea());
        }
#endif

        private void OnSafeAreaChanged(Rect safeArea)
        {
            ApplyPadding(safeArea);
        }

        private void ApplyPadding(Rect safeArea)
        {
            if (_layoutGroup == null)
                _layoutGroup = GetComponent<LayoutGroup>();

            if (!_initializedOriginal)
                CacheOriginalPadding();

            Vector2 screenSize = SafeAreaUtility.GetScreenSize();
            SafeAreaUtility.GetInsets(safeArea, screenSize,
                out float left, out float right, out float top, out float bottom);

            float scaleFactor = 1f;
            if (_canvas != null)
            {
                var scaler = _canvas.GetComponent<CanvasScaler>();
                if (scaler != null && scaler.uiScaleMode != CanvasScaler.ScaleMode.ConstantPixelSize)
                {
                    scaleFactor = scaler.scaleFactor;
                }
            }

            int padLeft = _originalPadding.left;
            int padRight = _originalPadding.right;
            int padTop = _originalPadding.top;
            int padBottom = _originalPadding.bottom;

            if (_useLeft)
                padLeft = _originalPadding.left + Mathf.RoundToInt(left / scaleFactor);
            if (_useRight)
                padRight = _originalPadding.right + Mathf.RoundToInt(right / scaleFactor);
            if (_useTop)
                padTop = _originalPadding.top + Mathf.RoundToInt(top / scaleFactor);
            if (_useBottom)
                padBottom = _originalPadding.bottom + Mathf.RoundToInt(bottom / scaleFactor);

            _layoutGroup.padding.left = padLeft;
            _layoutGroup.padding.right = padRight;
            _layoutGroup.padding.top = padTop;
            _layoutGroup.padding.bottom = padBottom;

            LayoutRebuilder.MarkLayoutForRebuild(_layoutGroup.transform as RectTransform);
        }
    }
}
