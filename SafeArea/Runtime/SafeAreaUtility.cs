// Assets/Jeomseon/SafeArea/Runtime/SafeAreaUtility.cs
using UnityEngine;

namespace Jeomseon.SafeArea
{
    /// <summary>
    /// Centralized access to Safe Area and screen size.
    /// - 런타임: Screen.safeArea / Screen.width / Screen.height 사용
    /// - 에디터: SafeAreaPreviewWindow에서 EditorOverrideEnabled, EditorSafeArea로 오버라이드 가능
    /// </summary>
    public static class SafeAreaUtility
    {
#if UNITY_EDITOR
        /// <summary>
        /// true이면 Screen.safeArea 대신 EditorSafeArea를 반환한다.
        /// (에디터 전용, 빌드에서는 항상 false 취급)
        /// </summary>
        public static bool EditorOverrideEnabled { get; set; }

        /// <summary>
        /// 에디터 전용 SafeArea 오버라이드 값(px).
        /// </summary>
        public static Rect EditorSafeArea { get; set; }
#endif

        /// <summary>
        /// 현재 Safe Area를 반환한다.
        /// 에디터에서 EditorOverrideEnabled가 true면 EditorSafeArea를 반환한다.
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
        /// 현재 화면 크기(px)를 반환한다.
        /// </summary>
        public static Vector2 GetScreenSize()
        {
            return new Vector2(Screen.width, Screen.height);
        }

        /// <summary>
        /// SafeArea와 ScreenSize를 기반으로
        /// 각 방향별 인셋(left, right, top, bottom)을 계산한다. (px 단위)
        /// </summary>
        public static void GetInsets(Rect safeArea, Vector2 screenSize,
            out float left, out float right, out float top, out float bottom)
        {
            left = safeArea.xMin;
            right = Mathf.Max(0f, screenSize.x - safeArea.xMax);
            bottom = safeArea.yMin;
            top = Mathf.Max(0f, screenSize.y - safeArea.yMax);
        }
    }
}
