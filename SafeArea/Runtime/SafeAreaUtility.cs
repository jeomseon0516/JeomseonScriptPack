using UnityEngine;

namespace Jeomseon.SafeArea
{
    /// <summary>
    /// Centralized access to Safe Area and screen size.
    /// In editor you can override the safe area for preview.
    /// </summary>
    public static class SafeAreaUtility
    {
#if UNITY_EDITOR
        /// <summary>
        /// When true, EditorSafeArea will be returned instead of Screen.safeArea.
        /// Only affects editor; ignored in player builds.
        /// </summary>
        public static bool EditorOverrideEnabled { get; set; }

        /// <summary>
        /// Editor-only override safe area rect in pixels.
        /// </summary>
        public static Rect EditorSafeArea { get; set; }
#endif

        /// <summary>
        /// Returns the current safe area.
        /// In editor, may be overridden by SafeAreaPreviewWindow.
        /// </summary>
        public static Rect GetSafeArea()
        {
#if UNITY_EDITOR
            if (EditorOverrideEnabled)
            {
                return EditorSafeArea;
            }
#endif
            return Screen.safeArea;
        }

        /// <summary>
        /// Returns the current screen size in pixels.
        /// </summary>
        public static Vector2 GetScreenSize()
        {
            return new Vector2(Screen.width, Screen.height);
        }

        /// <summary>
        /// Calculates edge insets (left, right, top, bottom) in pixels
        /// from a given safe area and screen size.
        /// </summary>
        public static void GetInsets(Rect safeArea, Vector2 screenSize, out float left, out float right, out float top, out float bottom)
        {
            left = safeArea.xMin;
            right = Mathf.Max(0f, screenSize.x - safeArea.xMax);
            bottom = safeArea.yMin;
            top = Mathf.Max(0f, screenSize.y - safeArea.yMax);
        }
    }
}
