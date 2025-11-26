using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jeomseon.SafeArea
{
    /// <summary>
    /// Automatically ensures there is a SafeArea-aware UI camera in the scene and
    /// redirects canvases to use it, unless they are tagged "IgnoreSafeAreaCamera".
    /// </summary>
    public static class SafeAreaAutoApplier
    {
        private const string UiCameraName = "UICamera_SafeArea";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Apply();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Apply();
        }

        /// <summary>
        /// Applies SafeArea camera to all canvases in the scene (except those tagged IgnoreSafeAreaCamera).
        /// </summary>
        public static void Apply()
        {
            var uiCamera = EnsureUiCamera();

#if UNITY_2023_1_OR_NEWER
            // 최신 버전: FindObjectsByType 사용
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            // 구버전 호환 (2019.3+): FindObjectsOfType 사용
            var canvases = Object.FindObjectsOfType<Canvas>(true); // includeInactive = true
#endif

            foreach (var canvas in canvases)
            {
                if (canvas == null)
                    continue;

                // 태그 미정의여도 예외 안 나게 string 비교 사용
                if (canvas.gameObject.tag == "IgnoreSafeAreaCamera")
                    continue;

                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = uiCamera;
                    canvas.planeDistance = 1f;
                }
                else if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
                {
                    canvas.worldCamera = uiCamera;
                }
            }
        }

        private static Camera EnsureUiCamera()
        {
            var existing = Object.FindObjectOfType<SafeAreaCamera>();
            if (existing != null)
                return existing.GetComponent<Camera>();

            var go = new GameObject(UiCameraName);
            Object.DontDestroyOnLoad(go);

            var cam = go.AddComponent<Camera>();
            go.AddComponent<SafeAreaCamera>();

            cam.clearFlags = CameraClearFlags.Depth;
            cam.cullingMask = LayerMask.GetMask("UI"); // assumes "UI" layer exists
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.depth = 100;

            return cam;
        }
    }
}
