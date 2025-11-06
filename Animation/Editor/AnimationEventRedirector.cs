#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;

namespace Jeomseon.Animation.Editor
{
    using Animation = UnityEngine.Animation;

    public class AnimationEventRedirector : EditorWindow
    {
        private const string DefaultTargetFunction = "ReceiveAnimationEvent";

        private string _targetFunctionName = DefaultTargetFunction;
        private bool _migrateOriginalFunctionNameToStringParam = true;
        private bool _onlyWhenStringParamEmpty = true;
        private bool _addDefaultEventIfNone = true;
        private float _defaultEventTimeNormalized = 1f; // 1 = 클립 끝

        // 🔹 추가: 하위 오브젝트 순회 시 비활성 포함 여부
        private bool _includeInactiveChildren = true;

        [MenuItem("Jeomseon/Animation/Redirect Events To Receiver")]
        public static void Open()
        {
            var win = GetWindow<AnimationEventRedirector>("Redirect Anim Events");
            win.minSize = new Vector2(420, 260);
            win.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Redirect Animation Events", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _targetFunctionName = EditorGUILayout.TextField(
                new GUIContent("Target Function Name", "AnimationClip 이벤트의 Function Name을 이 값으로 변경합니다."),
                _targetFunctionName
            );

            EditorGUILayout.Space(4);

            _migrateOriginalFunctionNameToStringParam = EditorGUILayout.ToggleLeft(
                new GUIContent("Move original functionName → stringParameter",
                    "기존 함수명을 evt.stringParameter로 옮겨 런타임에서 식별에 사용합니다."),
                _migrateOriginalFunctionNameToStringParam);

            using (new EditorGUI.DisabledScope(!_migrateOriginalFunctionNameToStringParam))
            {
                _onlyWhenStringParamEmpty = EditorGUILayout.ToggleLeft(
                    new GUIContent("Only when stringParameter is empty",
                        "stringParameter가 비어 있을 때만 기존 함수명을 옮깁니다."),
                    _onlyWhenStringParamEmpty);
            }

            EditorGUILayout.Space(4);

            _addDefaultEventIfNone = EditorGUILayout.ToggleLeft(
                new GUIContent("Add a default event if none exists",
                    "클립에 이벤트가 하나도 없으면 기본 이벤트를 추가합니다."),
                _addDefaultEventIfNone);

            using (new EditorGUI.DisabledScope(!_addDefaultEventIfNone))
            {
                _defaultEventTimeNormalized = Mathf.Clamp01(EditorGUILayout.Slider(
                    new GUIContent("Default Event Time (normalized)",
                        "0=시작, 1=끝. 기본 이벤트를 넣을 시점(정규화된 시간)"),
                    _defaultEventTimeNormalized, 0f, 1f));
            }

            EditorGUILayout.Space(6);

            // 🔹 추가: 하위 오브젝트 순회 옵션
            _includeInactiveChildren = EditorGUILayout.ToggleLeft(
                new GUIContent("Include Inactive Children (scan hierarchy)",
                    "선택한 오브젝트의 본인 포함 하위 오브젝트 전체를 순회하며 AnimationClip을 수집합니다."),
                _includeInactiveChildren);

            EditorGUILayout.Space();
            if (GUILayout.Button("Process Selected Clips / GameObjects", GUILayout.Height(34)))
            {
                ProcessSelection();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "사용법:\n" +
                "- Project/Hierarchy에서 AnimationClip, AnimatorController, 또는 Animator/Animation이 달린 GameObject(들)를 선택합니다.\n" +
                "- 버튼을 누르면 선택 항목과 (옵션에 따라) 자식들로부터 참조되는 모든 AnimationClip 이벤트가 지정한 함수로 리다이렉트됩니다.\n" +
                "- 읽기 전용(외부 DCC에서 임포트된) 클립도 이벤트는 에셋 상에 기록됩니다.",
                MessageType.Info);
        }

        private void ProcessSelection()
        {
            var clips = GatherClipsFromSelection(_includeInactiveChildren).Distinct().ToList();
            if (clips.Count == 0)
            {
                EditorUtility.DisplayDialog("Redirect Animation Events", "선택한 항목에서 AnimationClip을 찾지 못했습니다.", "OK");
                return;
            }

            int changedClips = 0, changedEvents = 0, addedEvents = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var clip in clips)
                {
                    var events = AnimationUtility.GetAnimationEvents(clip);
                    bool anyChanged = false;

                    // 이벤트가 없을 경우 기본 이벤트 추가 옵션
                    if (events == null || events.Length == 0)
                    {
                        if (_addDefaultEventIfNone && clip.length > 0f)
                        {
                            var newEvent = new AnimationEvent
                            {
                                time = Mathf.Clamp01(_defaultEventTimeNormalized) * clip.length,
                                functionName = _targetFunctionName
                            };
                            events = new[] { newEvent };
                            anyChanged = true;
                            addedEvents++;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // 기존 이벤트 리다이렉트
                        for (int i = 0; i < events.Length; i++)
                        {
                            var evt = events[i];
                            string originalFn = evt.functionName;

                            if (_migrateOriginalFunctionNameToStringParam)
                            {
                                bool shouldMove = !_onlyWhenStringParamEmpty || string.IsNullOrEmpty(evt.stringParameter);
                                if (shouldMove && !string.IsNullOrEmpty(originalFn) && originalFn != _targetFunctionName)
                                {
                                    if (string.IsNullOrEmpty(evt.stringParameter))
                                        evt.stringParameter = originalFn;
                                    else
                                        evt.stringParameter = $"{originalFn}|{evt.stringParameter}";
                                }
                            }

                            if (evt.functionName != _targetFunctionName)
                            {
                                evt.functionName = _targetFunctionName;
                                anyChanged = true;
                                changedEvents++;
                            }

                            events[i] = evt;
                        }
                    }

                    if (anyChanged)
                    {
                        Undo.RecordObject(clip, "Redirect Animation Events");
                        AnimationUtility.SetAnimationEvents(clip, events);
                        EditorUtility.SetDirty(clip);
                        changedClips++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }

            EditorUtility.DisplayDialog(
                "Done",
                $"Clips changed: {changedClips}\n" +
                $"Events modified: {changedEvents}\n" +
                $"Events added: {addedEvents}",
                "OK");
        }

        /// <summary>
        /// 현재 Selection에서 AnimationClip 수집:
        /// - 직접 선택된 AnimationClip
        /// - AnimatorController(애셋)에서 참조하는 모든 클립
        /// - GameObject가 가진 Animator/Animation 및 (옵션) 자식들의 Animator/Animation이 참조하는 모든 클립
        /// </summary>
        private static IEnumerable<AnimationClip> GatherClipsFromSelection(bool includeInactiveChildren)
        {
            var result = new List<AnimationClip>();

            foreach (var obj in Selection.objects)
            {
                switch (obj)
                {
                    case AnimationClip clip:
                        if (clip != null) result.Add(clip);
                        break;

                    case AnimatorController ac:
                        result.AddRange(GetClipsFromRuntimeController(ac));
                        break;

                    case RuntimeAnimatorController rc:
                        result.AddRange(GetClipsFromRuntimeController(rc));
                        break;

                    case GameObject go:
                        result.AddRange(CollectClipsFromGameObjectHierarchy(go, includeInactiveChildren));
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// GameObject 본인 포함 하위 오브젝트를 순회하며
        /// Animator/Animation에서 참조하는 모든 AnimationClip을 수집.
        /// </summary>
        private static IEnumerable<AnimationClip> CollectClipsFromGameObjectHierarchy(GameObject root, bool includeInactive)
        {
            var list = new List<AnimationClip>();
            if (root == null) return list;

            // 본인 + 자식 Transform 순회
            foreach (var t in root.GetComponentsInChildren<Transform>(includeInactive))
            {
                var go = t.gameObject;

                // Animator 기반 (Mecanim)
                if (go.TryGetComponent(out Animator animator) && animator.runtimeAnimatorController != null)
                {
                    list.AddRange(GetClipsFromRuntimeController(animator.runtimeAnimatorController));
                }

                // Legacy Animation 컴포넌트
                if (go.TryGetComponent<Animation>(out var legacy))
                {
                    foreach (AnimationState s in legacy)
                    {
                        if (s?.clip != null) list.Add(s.clip);
                    }
                }
            }

            // root 자신도 포함 (위 루프에 root 포함되므로 중복 방지 차원에서 생략 가능)
            return list.Where(c => c != null);
        }

        private static IEnumerable<AnimationClip> GetClipsFromRuntimeController(RuntimeAnimatorController rc)
        {
            // AnimatorController / AnimatorOverrideController 모두 처리됨
            var clips = rc?.animationClips;
            return clips != null ? clips.Where(c => c != null) : Enumerable.Empty<AnimationClip>();
        }
    }
}
#endif
