// Assets/Jeomseon/SafeArea/Editor/SafeAreaPreviewWindow.cs
#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Jeomseon.SafeArea;

namespace Jeomseon.SafeAreaEditor
{
    public class SafeAreaPreviewWindow : EditorWindow
    {
        private bool _overrideEnabled = true;
        private Rect _safeAreaRect;
        private Vector2 _screenSize = new Vector2(1080, 1920);

        private Scene _previewScene;
        private Camera _previewCamera;
        private RenderTexture _rt;

        private int _srcCanvasCount;
        private int _previewCanvasCount;

        [MenuItem("Jeomseon/Safe Area/Preview Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<SafeAreaPreviewWindow>("Safe Area Preview");
            window.minSize = new Vector2(480, 320);
        }

        // ========================================
        //  Life Cycle
        // ========================================

        private void OnEnable()
        {
            // 기본값: 현재 GameView 기준
            RefreshScreenFromGameView();
            RefreshSafeAreaFromGameView();

            CreatePreviewScene();
            RebuildAll();   // 캔버스 복제 + 카메라 설정 + SafeArea 적용
        }

        private void OnDisable()
        {
            DestroyPreviewScene();
        }

        // ========================================
        //  GUI
        // ========================================

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Safe Area Preview (PreviewScene)", EditorStyles.boldLabel);

            EditorGUILayout.LabelField($"Source canvases  : {_srcCanvasCount}");
            EditorGUILayout.LabelField($"Preview canvases : {_previewCanvasCount}");

            if (_previewScene.IsValid())
            {
                int canvasInScene = _previewScene.GetRootGameObjects()
                    .Sum(r => r.GetComponentsInChildren<Canvas>(true).Length);
                EditorGUILayout.LabelField($"Canvases in PreviewScene: {canvasInScene}");
            }

            if (_previewCamera != null)
            {
                EditorGUILayout.LabelField($"Camera enabled: {_previewCamera.enabled}");
                EditorGUILayout.LabelField($"Camera active: {_previewCamera.gameObject.activeInHierarchy}");
            }

            EditorGUILayout.Space();

            // ==== Override 토글 ====
            bool prevOverride = _overrideEnabled;
            _overrideEnabled = EditorGUILayout.Toggle("Override Safe Area", _overrideEnabled);

            if (prevOverride != _overrideEnabled)
            {
                // 토글이 바뀔 때마다 항상 현재 GameView 값으로 동기화
                RefreshScreenFromGameView();
                RefreshSafeAreaFromGameView(); // Override 켜질 때는 현재 기기 SafeArea를 기본값으로

                RebuildAll();
                return; // 이 프레임에서는 여기까지, 다음 프레임에서 새 상태로 DrawPreview
            }

            EditorGUILayout.Space();

            // ==== Screen / SafeArea 편집 ====
            using (new EditorGUI.DisabledScope(!_overrideEnabled))
            {
                _screenSize = EditorGUILayout.Vector2Field("Screen Size (px)", _screenSize);
                _safeAreaRect = EditorGUILayout.RectField("Safe Area (px)", _safeAreaRect);
            }

            if (GUILayout.Button("Apply & Rebuild Preview"))
            {
                // 👉 Apply 는 항상 현재 GameView 해상도를 다시 읽는다
                RefreshScreenFromGameView();

                if (!_overrideEnabled)
                {
                    // Override 꺼져 있으면 SafeArea도 기기 값으로 다시 가져온다
                    RefreshSafeAreaFromGameView();
                }
                // Override 켜져 있으면 SafeAreaRect 는 사용자가 입력한 값 유지

                RebuildAll();
                return;
            }

            EditorGUILayout.Space();

            // ==== RenderTexture 준비 & 카메라 렌더 ====
            DrawPreview();
        }

        // ========================================
        //  High-level helpers
        // ========================================

        /// <summary>
        /// GameView의 Screen.width/height 를 읽어서 내부 screenSize 갱신
        /// </summary>
        private void RefreshScreenFromGameView()
        {
            _screenSize = new Vector2(Screen.width, Screen.height);
        }

        /// <summary>
        /// GameView의 Screen.safeArea 를 읽어서 내부 safeAreaRect 갱신
        /// </summary>
        private void RefreshSafeAreaFromGameView()
        {
            _safeAreaRect = Screen.safeArea;
        }

        /// <summary>
        /// Preview 전체를 다시 빌드:
        /// - Canvas 복제
        /// - Camera 설정
        /// - SafeAreaRoot에 Preview 적용
        /// - Canvas 레이아웃 강제 업데이트
        /// </summary>
        private void RebuildAll()
        {
            CreatePreviewScene();
            RebuildPreviewFromActiveScene();
            UpdateCameraSettings();
            ApplyPreviewToScene();
            Canvas.ForceUpdateCanvases();
            Repaint();
        }

        // ========================================
        //  PreviewScene 구축/해제
        // ========================================

        private void CreatePreviewScene()
        {
            if (_previewScene.IsValid())
                return;

            _previewScene = EditorSceneManager.NewPreviewScene();

            var camGO = new GameObject("SafeAreaPreviewCamera");
            _previewCamera = camGO.AddComponent<Camera>();
            _previewCamera.clearFlags = CameraClearFlags.Skybox;   // 🔵 요청대로 Skybox 사용
            _previewCamera.backgroundColor = Color.gray;           // Skybox 없을 때 fallback
            _previewCamera.orthographic = true;
            _previewCamera.nearClipPlane = 0.1f;
            _previewCamera.farClipPlane = 100f;
            _previewCamera.cullingMask = ~0;
            _previewCamera.enabled = true;
            _previewCamera.cameraType = CameraType.Game;

            SceneManager.MoveGameObjectToScene(camGO, _previewScene);

            // PreviewScene 전용 culling mask 적용
            ulong sceneMask = EditorSceneManager.GetSceneCullingMask(_previewScene);
            _previewCamera.overrideSceneCullingMask = sceneMask;

            UpdateCameraSettings();
        }

        private void DestroyPreviewScene()
        {
            if (_previewCamera != null && _previewCamera.targetTexture != null)
                _previewCamera.targetTexture = null;

            if (_rt != null)
            {
                _rt.Release();
                DestroyImmediate(_rt);
                _rt = null;
            }

            if (_previewScene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(_previewScene);
            }
        }

        // ========================================
        //  Camera / RenderTexture / Draw
        // ========================================

        /// <summary>
        /// 카메라를 논리 ScreenSize에 맞게 설정 (1유닛 = 1픽셀)
        /// </summary>
        private void UpdateCameraSettings()
        {
            if (_previewCamera == null)
                return;

            Vector2 screenSize = _screenSize;

            if (screenSize.y <= 0) screenSize.y = 1;
            if (screenSize.x <= 0) screenSize.x = screenSize.y;

            _previewCamera.orthographicSize = screenSize.y * 0.5f;  // -H/2~+H/2 범위
            _previewCamera.aspect = screenSize.x / screenSize.y;
            _previewCamera.transform.position = new Vector3(0, 0, -10);
            _previewCamera.transform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// RenderTexture 준비 + 카메라 렌더 + 창에 그리기
        /// </summary>
        private void DrawPreview()
        {
            if (_previewCamera == null)
                return;

            Vector2 screenSize = _screenSize;
            int renderWidth = Mathf.Max(1, (int)screenSize.x);
            int renderHeight = Mathf.Max(1, (int)screenSize.y);

            // RT 크기 맞추기
            if (_rt == null || _rt.width != renderWidth || _rt.height != renderHeight)
            {
                if (_previewCamera.targetTexture == _rt)
                    _previewCamera.targetTexture = null;

                if (_rt != null)
                {
                    _rt.Release();
                    DestroyImmediate(_rt);
                    _rt = null;
                }

                _rt = new RenderTexture(renderWidth, renderHeight, 24, RenderTextureFormat.ARGB32);
                _rt.Create();
            }

            if (_rt != null)
            {
                Canvas.ForceUpdateCanvases();

                _previewCamera.targetTexture = _rt;
                _previewCamera.pixelRect = new Rect(0, 0, renderWidth, renderHeight);
                _previewCamera.Render();
            }

            // 지금까지 그린 GUI 아래의 남은 영역 전체를 프리뷰로 사용
            Rect layoutRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                GUIStyle.none,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );

            if (layoutRect.width <= 1f || layoutRect.height <= 1f || _rt == null)
                return;

            float targetAspect = screenSize.x / screenSize.y;
            float windowAspect = layoutRect.width / layoutRect.height;

            Rect previewRect;

            if (windowAspect > targetAspect)
            {
                // 창이 더 납작함 → 높이에 맞추고 좌우 여백
                float height = layoutRect.height;
                float width = height * targetAspect;
                float x = layoutRect.x + (layoutRect.width - width) * 0.5f;
                float y = layoutRect.y;
                previewRect = new Rect(x, y, width, height);
            }
            else
            {
                // 창이 더 세로로 김 → 너비에 맞추고 상하 여백
                float width = layoutRect.width;
                float height = width / targetAspect;
                float x = layoutRect.x;
                float y = layoutRect.y + (layoutRect.height - height) * 0.5f;
                previewRect = new Rect(x, y, width, height);
            }

            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(previewRect, _rt, ScaleMode.StretchToFill, false);
            }
        }

        // ========================================
        //  Canvas 복제 / 세팅 / SafeArea 적용
        // ========================================

        /// <summary>
        /// 현재 Active Scene의 Canvas들을 PreviewScene으로 복제
        /// </summary>
        private void RebuildPreviewFromActiveScene()
        {
            if (!_previewScene.IsValid())
                CreatePreviewScene();

            // 카메라만 남기고 정리
            foreach (var root in _previewScene.GetRootGameObjects())
            {
                if (root.name != "SafeAreaPreviewCamera")
                    Object.DestroyImmediate(root);
            }

            _srcCanvasCount = 0;
            _previewCanvasCount = 0;

            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
                return;

            var roots = activeScene.GetRootGameObjects();

            foreach (var root in roots)
            {
                var canvases = root.GetComponentsInChildren<Canvas>(true);
                _srcCanvasCount += canvases.Length;

                foreach (var canvas in canvases)
                {
                    if (canvas.renderMode == RenderMode.WorldSpace)
                        continue; // 3D UI는 제외

                    var clone = Object.Instantiate(canvas.gameObject);
                    clone.name = canvas.gameObject.name + " (Preview)";
                    clone.SetActive(true);

                    SceneManager.MoveGameObjectToScene(clone, _previewScene);

                    if (clone.TryGetComponent<Canvas>(out var cloneCanvas))
                    {
                        SetupCanvasForPreview(cloneCanvas);
                        SafeAreaPatchCore.EnsureSafeAreaRoot(cloneCanvas);
                        _previewCanvasCount++;
                    }
                }
            }

            Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// PreviewScene에 맞게 Canvas 설정
        /// (SafeAreaRoot가 실제 SafeArea 적용을 담당하므로 Canvas는 전체 화면 기준)
        /// </summary>
        private void SetupCanvasForPreview(Canvas canvas)
        {
            if (canvas == null || _previewCamera == null)
                return;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                canvas.renderMode = RenderMode.ScreenSpaceCamera;

            canvas.worldCamera = _previewCamera;
            canvas.planeDistance = 1f;

            var rectTransform = canvas.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 screenSize = _screenSize;

                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = screenSize;
                rectTransform.localPosition = Vector3.zero;
            }

            canvas.sortingOrder = 0;

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
                SetLayerRecursively(canvas.gameObject, uiLayer);

            if (!canvas.gameObject.activeInHierarchy)
                canvas.gameObject.SetActive(true);
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        /// <summary>
        /// 현재 설정된 safeArea / screenSize를 PreviewScene 안의 SafeAreaRoot들에게만 적용.
        /// 원본 씬은 건드리지 않는다.
        /// </summary>
        private void ApplyPreviewToScene()
        {
            if (!_previewScene.IsValid())
                return;

            Rect safeArea = _safeAreaRect;
            Vector2 screenSize = _screenSize;

            var roots = _previewScene.GetRootGameObjects();
            foreach (var root in roots)
            {
                var safeAreaRoots = root.GetComponentsInChildren<SafeAreaRoot>(true);
                foreach (var sr in safeAreaRoots)
                {
                    sr.ApplyPreview(safeArea, screenSize);
                }
            }
        }
    }
}
#endif
