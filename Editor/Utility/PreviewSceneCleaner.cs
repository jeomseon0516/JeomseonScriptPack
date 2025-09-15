#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jeomseon.Editor
{
    [InitializeOnLoad]
    internal static class PreviewSceneCleaner
    {
        static PreviewSceneCleaner()
        {
            AssemblyReloadEvents.beforeAssemblyReload += closeAllPreviewScenes;
            EditorApplication.quitting += closeAllPreviewScenes;
        }
        
        private static void closeAllPreviewScenes()
        {
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isSubScene || (scene.IsValid() && scene.name.Contains("Preview Scene")) || EditorSceneManager.IsPreviewScene(scene))
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
    }
}
#endif