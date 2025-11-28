// Assets/Jeomseon/SafeArea/Editor/SafeAreaScenePatcher.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Jeomseon.SafeArea;

namespace Jeomseon.SafeAreaEditor
{
    /// <summary>
    /// 에디터에서 실제 씬에 SafeAreaRoot를 붙여주는 Patcher.
    /// - 이걸 쓰면 런타임 AutoApplier 없이도 씬이 이미 SafeArea 대응 상태가 됨.
    /// </summary>
    public static class SafeAreaScenePatcher
    {
        [MenuItem("Jeomseon/Safe Area/Patch Active Scene")]
        public static void PatchActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded)
            {
                Debug.LogWarning("[SafeAreaScenePatcher] No active scene loaded.");
                return;
            }

            PatchScene(scene, useUndo: true);
        }

        /// <summary>
        /// 특정 Scene에 포함된 Canvas들을 SafeAreaRoot로 감싼다.
        /// useUndo가 true면 Undo 히스토리에 남긴다.
        /// </summary>
        public static void PatchScene(Scene scene, bool useUndo)
        {
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                var canvases = root.GetComponentsInChildren<Canvas>(true);
                foreach (var canvas in canvases)
                {
                    if (useUndo)
                        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Patch SafeArea Canvas");

                    SafeAreaPatchCore.EnsureSafeAreaRoot(canvas);

                    if (useUndo)
                        EditorUtility.SetDirty(canvas);
                }
            }

            if (useUndo)
                EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif
