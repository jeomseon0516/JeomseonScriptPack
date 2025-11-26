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
    /// GameView를 해당 기기로 띄우고 여기서 'Use Current Screen.safeArea' 버튼을 누르면 된다.
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

            // 오버라이드 값이 비어있으면 현재 Screen 값으로 초기화
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
                "  2) GameView가 해당 기기를 보여주는 상태에서\n" +
                "  3) 여기서 'Use Current Screen.safeArea' 버튼 클릭",
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
                EditorGUILayout.LabelField($"Screen.width      : {Screen.width}");
                EditorGUILayout.LabelField($"Screen.height     : {Screen.height}");
                var sa = Screen.safeArea;
                EditorGUILayout.LabelField($"Screen.safeArea   : x={sa.x}, y={sa.y}, w={sa.width}, h={sa.height}");

                // Utility 관점에서의 SafeArea도 참고용으로 출력
                var utilSa = SafeAreaUtility.GetSafeArea();
                EditorGUILayout.LabelField($"Utility SafeArea  : x={utilSa.x}, y={utilSa.y}, w={utilSa.width}, h={utilSa.height}");

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
            // 1) 에디터용 SafeArea 오버라이드 설정
            SafeAreaUtility.EditorOverrideEnabled = _overrideEnabled;
            SafeAreaUtility.EditorSafeArea = _safeAreaRect;

            // 2) 에디터에서도 SafeAreaRoot가 존재하도록 강제 적용
            //    (Canvas들을 감싸고 SafeAreaRoot 컴포넌트 부착)
            SafeAreaAutoApplier.ApplyToAllCanvases();

            // 3) 현재 SafeArea 상태를 모든 구독자(SafeAreaRoot, SafeAreaPadding 등)에 브로드캐스트
            SafeAreaWatcher.ForceUpdate();

            // 4) 씬 뷰 / 게임 뷰 갱신
            SceneView.RepaintAll();
        }
    }
}
#endif
