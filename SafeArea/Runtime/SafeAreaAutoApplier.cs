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

            var canvases = Object.FindObjectsOfType<Canvas>(true);
            foreach (var canvas in canvases)
            {
                if (canvas == null)
                    continue;

                // Skip canvases that explicitly ignore SafeArea camera.
                if (canvas.CompareTag("IgnoreSafeAreaCamera"))
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
            cam.cullingMask = LayerMask.GetMask("UI"); // assumes UI layer exists
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.depth = 100;

            return cam;
        }
    }
}
