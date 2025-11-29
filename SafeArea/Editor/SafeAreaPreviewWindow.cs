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

        private void OnEnable()
        {
            // 기본값: 현재 GameView 기준
            _screenSize = new Vector2(Screen.width, Screen.height);
            _safeAreaRect = Screen.safeArea;

            CreatePreviewScene();
            RebuildPreviewFromActiveScene();
            ApplyPreviewToScene();
        }

        private void OnDisable()
        {
            DestroyPreviewScene();
        }

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

            // Override 토글
            bool prevOverride = _overrideEnabled;
            _overrideEnabled = EditorGUILayout.Toggle("Override Safe Area", _overrideEnabled);

            if (prevOverride != _overrideEnabled)
            {
                if (!_overrideEnabled)
                {
                    // Override 해제 → GameView 시뮬레이터 값으로 동기화
                    _screenSize = new Vector2(Screen.width, Screen.height);
                    _safeAreaRect = Screen.safeArea;
                }

                RebuildAndApply();
            }

            EditorGUILayout.Space();

            // 수동으로 조절할 값 (Override 켜져 있을 때만 의미 있음)
            using (new EditorGUI.DisabledScope(!_overrideEnabled))
            {
                _screenSize = EditorGUILayout.Vector2Field("Screen Size (px)", _screenSize);
                _safeAreaRect = EditorGUILayout.RectField("Safe Area (px)", _safeAreaRect);
            }

            if (GUILayout.Button("Apply Override & Rebuild Preview"))
            {
                if (!_overrideEnabled)
                {
                    // Override 끈 상태라면, 그냥 현재 GameView 값 다시 가져와서 재빌드
                    _screenSize = new Vector2(Screen.width, Screen.height);
                    _safeAreaRect = Screen.safeArea;
                }

                RebuildAndApply();
            }

            EditorGUILayout.Space();

            DrawPreviewArea();
        }

        // 한 번에 처리하는 헬퍼
        private void RebuildAndApply()
        {
            CreatePreviewScene();        // (이미 있으면 early-return)
            RebuildPreviewFromActiveScene();
            UpdateCameraSettings();
            EnsureRenderTexture();
            ApplyPreviewToScene();
            Canvas.ForceUpdateCanvases();
            Repaint();
        }

        // === PreviewScene 구축/해제 ===

        private void CreatePreviewScene()
        {
            if (_previewScene.IsValid())
                return;

            _previewScene = EditorSceneManager.NewPreviewScene();

            var camGO = new GameObject("SafeAreaPreviewCamera");
            _previewCamera = camGO.AddComponent<Camera>();
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewCamera.backgroundColor = Color.gray;
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
            {
                _previewCamera.targetTexture = null;
            }

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

        /// <summary>
        /// 카메라를 논리 ScreenSize에 맞게 설정 (1유닛 = 1픽셀)
        /// </summary>
        private void UpdateCameraSettings()
        {
            if (_previewCamera == null)
                return;

            Vector2 screenSize = _overrideEnabled
                ? _screenSize
                : new Vector2(Screen.width, Screen.height);

            if (screenSize.y <= 0) screenSize.y = 1;
            if (screenSize.x <= 0) screenSize.x = screenSize.y;

            _previewCamera.orthographicSize = screenSize.y * 0.5f;  // -H/2~+H/2 범위
            _previewCamera.aspect = screenSize.x / screenSize.y;
            _previewCamera.transform.position = new Vector3(0, 0, -10);
            _previewCamera.transform.rotation = Quaternion.identity;
        }

        // RenderTexture를 현재 screenSize에 맞게 한 번만 맞춰둔다
        private void EnsureRenderTexture()
        {
            Vector2 screenSize = _overrideEnabled
                ? _screenSize
                : new Vector2(Screen.width, Screen.height);

            int renderWidth = Mathf.Max(1, (int)screenSize.x);
            int renderHeight = Mathf.Max(1, (int)screenSize.y);

            if (_rt != null && _rt.width == renderWidth && _rt.height == renderHeight)
                return;

            if (_previewCamera != null && _previewCamera.targetTexture == _rt)
            {
                _previewCamera.targetTexture = null;
            }

            if (_rt != null)
            {
                _rt.Release();
                DestroyImmediate(_rt);
                _rt = null;
            }

            _rt = new RenderTexture(renderWidth, renderHeight, 24, RenderTextureFormat.ARGB32);
            _rt.Create();

            if (_previewCamera != null)
            {
                _previewCamera.targetTexture = _rt;
                _previewCamera.pixelRect = new Rect(0, 0, renderWidth, renderHeight);
            }
        }

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

                    var cloneCanvas = clone.GetComponent<Canvas>();
                    if (cloneCanvas != null)
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
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                canvas.worldCamera = _previewCamera;
                canvas.planeDistance = 1f;
            }

            var rectTransform = canvas.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 screenSize = _overrideEnabled
                    ? _screenSize
                    : new Vector2(Screen.width, Screen.height);

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
            {
                SetLayerRecursively(canvas.gameObject, uiLayer);
            }

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
        /// SafeAreaRoot 들에 Preview용 SafeArea 적용
        /// </summary>
        private void ApplyPreviewToScene()
        {
            if (!_previewScene.IsValid())
                return;

            Rect safeArea = _overrideEnabled ? _safeAreaRect : Screen.safeArea;
            Vector2 screenSize = _overrideEnabled ? _screenSize : new Vector2(Screen.width, Screen.height);

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

        /// <summary>
        /// 아래 남은 영역 전체를 프리뷰로 사용 (비율 유지)
        /// </summary>
        private void DrawPreviewArea()
        {
            EnsureRenderTexture();
            if (_rt == null)
                return;

            // 🔴 여기 추가
            if (_previewCamera != null)
            {
                // 혹시 모르니 다시 한 번 타겟 지정 & 렌더
                _previewCamera.targetTexture = _rt;

                Vector2 screenSizeForCam = _overrideEnabled
                    ? _screenSize
                    : new Vector2(Screen.width, Screen.height);

                int renderWidth = Mathf.Max(1, (int)screenSizeForCam.x);
                int renderHeight = Mathf.Max(1, (int)screenSizeForCam.y);
                _previewCamera.pixelRect = new Rect(0, 0, renderWidth, renderHeight);

                Canvas.ForceUpdateCanvases();
                _previewCamera.Render();
            }
            // 🔴 여기까지 추가

            // 지금까지 그린 GUI 아래의 남은 영역을 통째로 요청
            Rect layoutRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                GUIStyle.none,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );

            if (layoutRect.width <= 1f || layoutRect.height <= 1f)
                return;

            Vector2 screenSize = _overrideEnabled
                ? _screenSize
                : new Vector2(Screen.width, Screen.height);

            float targetAspect = screenSize.x / screenSize.y;
            float windowAspect = layoutRect.width / layoutRect.height;

            Rect previewRect;

            if (windowAspect > targetAspect)
            {
                float height = layoutRect.height;
                float width = height * targetAspect;
                float x = layoutRect.x + (layoutRect.width - width) * 0.5f;
                float y = layoutRect.y;
                previewRect = new Rect(x, y, width, height);
            }
            else
            {
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
    }
}
#endif
