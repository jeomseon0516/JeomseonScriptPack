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
            // 기본값: SafeAreaUtility 기준 (GameView 의 실제 로직과 동일 경로)
            RefreshFromSafeAreaUtility();

            CreatePreviewScene();
            RebuildAll();
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
                // ✅ Override 를 끌 때(true → false)만
                //    SafeAreaUtility에서 새 값 가져와 동기화
                if (!_overrideEnabled)
                {
                    RefreshFromSafeAreaUtility();
                }

                RebuildAll();
                return;
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
                // ✅ Apply 동작:
                //  - Override OFF : SafeAreaUtility 값으로 다시 읽고 재빌드
                //  - Override ON  : 사용자가 입력한 _screenSize / _safeAreaRect 그대로 재빌드
                if (!_overrideEnabled)
                {
                    RefreshFromSafeAreaUtility();
                }

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
        /// 런타임과 동일하게 SafeAreaUtility를 통해 화면 크기/세이프 에어리어를 읽어온다.
        /// </summary>
        private void RefreshFromSafeAreaUtility()
        {
            // SafeAreaRoot가 사용하는 것과 동일한 함수 사용
            _screenSize = SafeAreaUtility.GetScreenSize();
            _safeAreaRect = SafeAreaUtility.GetSafeArea();

            // 방어 코드: 혹시 유틸리티에서 0,0 나올 경우 Screen 값으로 보정
            if (_screenSize.x <= 0 || _screenSize.y <= 0)
            {
                _screenSize = new Vector2(Screen.width, Screen.height);
            }
        }

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
            _previewCamera.clearFlags = CameraClearFlags.Skybox;   // ⬅ Skybox 배경
            _previewCamera.backgroundColor = Color.gray;
            _previewCamera.orthographic = true;
            _previewCamera.nearClipPlane = 0.1f;
            _previewCamera.farClipPlane = 100f;
            _previewCamera.cullingMask = ~0;
            _previewCamera.enabled = true;
            _previewCamera.cameraType = CameraType.Game;

            SceneManager.MoveGameObjectToScene(camGO, _previewScene);

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

        private void UpdateCameraSettings()
        {
            if (_previewCamera == null)
                return;

            Vector2 screenSize = _screenSize;

            if (screenSize.y <= 0) screenSize.y = 1;
            if (screenSize.x <= 0) screenSize.x = screenSize.y;

            _previewCamera.orthographicSize = screenSize.y * 0.5f;
            _previewCamera.aspect = screenSize.x / screenSize.y;
            _previewCamera.transform.position = new Vector3(0, 0, -10);
            _previewCamera.transform.rotation = Quaternion.identity;
        }

        private void DrawPreview()
        {
            if (_previewCamera == null)
                return;

            Vector2 screenSize = _screenSize;
            int renderWidth = Mathf.Max(1, (int)screenSize.x);
            int renderHeight = Mathf.Max(1, (int)screenSize.y);

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

        // ========================================
        //  Canvas 복제 / SafeArea 적용
        // ========================================

        private void RebuildPreviewFromActiveScene()
        {
            if (!_previewScene.IsValid())
                CreatePreviewScene();

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
                        continue;

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
