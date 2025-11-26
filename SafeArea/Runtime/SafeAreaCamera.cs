using UnityEngine;

namespace Jeomseon.SafeArea
{
    /// <summary>
    /// Adjusts the attached Camera's viewport rect to match the safe area.
    /// Useful for Screen Space - Camera and World Space canvases.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class SafeAreaCamera : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        private void Awake()
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();
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

            float widthRatio  = safeArea.width  / screenSize.x;
            float heightRatio = safeArea.height / screenSize.y;

            float x = safeArea.x / screenSize.x;
            float y = safeArea.y / screenSize.y;

            _camera.rect = new Rect(x, y, widthRatio, heightRatio);
        }
    }
}
