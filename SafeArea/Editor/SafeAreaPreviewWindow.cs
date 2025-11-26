#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Jeomseon.SafeArea;

namespace Jeomseon.SafeAreaEditor
{
    /// <summary>
    /// Editor window for previewing Safe Area in the editor.
    /// - Manual override of screen size & safe area.
    /// - "Use Current Screen.safeArea" button to pull values from the active Game/Simulator view.
    ///
    /// Works on all Unity versions without Device Simulator API 의존.
    /// Device Simulator를 쓰는 경우, Simulator에서 기기 선택 후
    /// "Use Current Screen.safeArea" 버튼만 눌러주면 됨.
    /// </summary>
    public class SafeAreaPreviewWindow : EditorWindow
    {
        private bool _overrideEnabled;
        private Rect _safeAreaRect;
        private Vector2 _screenSize = new Vector2(1080, 1920);

        [MenuItem("Jeomseon/Safe Area Preview")]
        public static void ShowWindow()
        {
            var window = GetWindow<SafeAreaPreviewWindow>("Safe Area Preview");
            window.minSize = new Vector2(360, 220);
        }

        private void OnEnable()
        {
            _overrideEnabled = SafeAreaUtility.EditorOverrideEnabled;
            _safeAreaRect = SafeAreaUtility.EditorSafeArea;

            // 기본값이 비어있으면 현재 Screen 값으로 초기화
            if (_safeAreaRect.width <= 0f || _safeAreaRect.height <= 0f)
            {
                _screenSize = new Vector2(Screen.width, Screen.height);
                _safeAreaRect = Screen.safeArea;
            }
            else
            {
                _screenSize = new Vector2(Screen.width, Screen.height);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Safe Area Preview (Editor Only)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _overrideEnabled = EditorGUILayout.Toggle("Override Safe Area", _overrideEnabled);

            EditorGUILayout.Space();

            DrawCurrentScreenSection();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Manual Override", EditorStyles.boldLabel);

            _screenSize = EditorGUILayout.Vector2Field("Screen Size (px)", _screenSize);
            _safeAreaRect = EditorGUILayout.RectField("Safe Area (px)", _safeAreaRect);

            if (GUILayout.Button("Apply To SafeAreaUtility"))
            {
                ApplyOverrideToRuntime();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "This override affects SafeAreaUtility.GetSafeArea() only in the editor.\n" +
                "It is ignored in player builds.\n\n" +
                "- Device Simulator 사용 시:\n" +
                "  1) Device Simulator 창에서 기기 선택\n" +
                "  2) 여기서 'Use Current Screen.safeArea' 버튼 클릭",
                MessageType.None);
        }

        /// <summary>
        /// 현재 에디터의 Screen 값(GameView 또는 Device Simulator가 세팅한 값)을
        /// 그대로 읽어서 SafeAreaUtility 오버라이드에 반영하는 UI.
        /// </summary>
        private void DrawCurrentScreenSection()
        {
            EditorGUILayout.LabelField("From Current Screen", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"Screen.width  : {Screen.width}");
                EditorGUILayout.LabelField($"Screen.height : {Screen.height}");
                var sa = Screen.safeArea;
                EditorGUILayout.LabelField($"Screen.safeArea : x={sa.x}, y={sa.y}, w={sa.width}, h={sa.height}");

                EditorGUILayout.Space();

                if (GUILayout.Button("Use Current Screen.safeArea"))
                {
                    _screenSize = new Vector2(Screen.width, Screen.height);
                    _safeAreaRect = Screen.safeArea;
                    _overrideEnabled = true;
                    ApplyOverrideToRuntime();
                }
            }
        }

        private void ApplyOverrideToRuntime()
        {
            SafeAreaUtility.EditorOverrideEnabled = _overrideEnabled;
            SafeAreaUtility.EditorSafeArea = _safeAreaRect;

            // ⭐ 이제는 Instance 필요 없이 바로 static ForceUpdate 호출
            SafeAreaWatcher.ForceUpdate();

            SceneView.RepaintAll();
        }
    }
}
#endif
