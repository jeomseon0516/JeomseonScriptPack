#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// Bulk Component Remover (통합판)
/// - 선택한 GameObject(들) + 본인 포함 자식을 순회하여 특정 컴포넌트를 일괄 제거
/// - 컴포넌트 지정: (1) MonoScript 드래그(컴포넌트만 허용), (2) 타입명 입력, (3) 컴포넌트 전용 타입 브라우저
/// - Missing Script 제거 전용 모드
/// - 레이어/태그 필터, Include Inactive, Search Children
/// - 미리보기 및 Undo 지원
/// 메뉴: Window > Tools > Bulk Component Remover
/// </summary>
namespace Jeomseon.Editor
{
    public class BulkComponentRemoverWindow : EditorWindow
    {
        // ----------------------------------------------------------------------------
        // 상태
        // ----------------------------------------------------------------------------
        private MonoScript _monoScript;                 // 컴포넌트 MonoScript(컴포넌트만 허용)
        private bool _monoIsInvalidPick;                // 컴포넌트가 아닌 스크립트 선택 시 경고용
        private Type _resolvedType;                     // 최종 해석된 컴포넌트 타입(브라우저/MonoScript/문자열)
        private string _typeName = string.Empty;        // 직접 입력 타입명 (FQN 또는 간단 이름)

        private bool _missingOnly = false;              // Missing Script 제거 전용
        private bool _includeInactive = true;           // 비활성 포함
        private bool _searchInSelectionChildren = true; // 선택 오브젝트 자식 포함

        private bool _filterByLayer = false;
        private LayerMask _layerMask = ~0;              // 전체 레이어 기본값
        private bool _filterByTag = false;
        private int _tagIndex = 0;                      // InternalEditorUtility.tags 인덱스

        [Serializable]
        private class Match
        {
            public GameObject go;
            public Component component; // Missing Script일 때 null
            public bool isMissing;
            public override string ToString() => isMissing ? "[Missing Script]" : (component ? component.GetType().Name : "(null)");
        }

        private readonly List<Match> _matches = new();
        private Vector2 _scroll;

        // ----------------------------------------------------------------------------
        // 메뉴
        // ----------------------------------------------------------------------------
        [MenuItem("Jeomseon/Tools/Bulk Component Remover")]
        public static void Open()
        {
            var wnd = GetWindow<BulkComponentRemoverWindow>(true, "Bulk Component Remover");
            wnd.minSize = new Vector2(560, 480);
            wnd.Show();
        }

        // ----------------------------------------------------------------------------
        // GUI
        // ----------------------------------------------------------------------------
        private void OnGUI()
        {
            // 모드
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mode", EditorStyles.boldLabel);
            _missingOnly = EditorGUILayout.ToggleLeft(new GUIContent("Missing Script Cleanup Only", "Missing Script만 제거합니다 (타입 지정 무시)"), _missingOnly);

            // 대상 컴포넌트 지정
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(_missingOnly))
            {
                EditorGUILayout.LabelField("Target Component", EditorStyles.boldLabel);

                //// 1) 컴포넌트 타입 전용 브라우저 버튼
                //using (new EditorGUILayout.HorizontalScope())
                //{
                //    if (GUILayout.Button(new GUIContent("Browse Component Types...", "컴포넌트 타입 선택 팝업"), GUILayout.Width(220)))
                //    {
                //        var pickedType = ComponentTypePicker.ShowPicker();
                //        if (pickedType != null)
                //        {
                //            _resolvedType = pickedType;          // 타입 우선 사용
                //            _typeName = pickedType.FullName;     // 텍스트 입력에도 반영
                //            _monoScript = null;                  // MonoScript 선택은 비움
                //            _monoIsInvalidPick = false;
                //        }
                //    }
                //    if (_resolvedType != null)
                //    {
                //        EditorGUILayout.LabelField(new GUIContent($"Selected: {_resolvedType.FullName}"));
                //    }
                //}

                // 2) MonoScript 드래그 (컴포넌트만 허용)
                EditorGUI.BeginChangeCheck();
                var picked = (MonoScript)EditorGUILayout.ObjectField(new GUIContent("MonoScript", "Component 타입만 허용"), _monoScript, typeof(MonoScript), false);
                if (EditorGUI.EndChangeCheck())
                {
                    _monoIsInvalidPick = false;
                    if (picked == null)
                    {
                        _monoScript = null;
                    }
                    else
                    {
                        var cls = picked.GetClass();
                        if (cls != null && typeof(Component).IsAssignableFrom(cls))
                        {
                            _monoScript = picked;
                            _resolvedType = null; // MonoScript 우선 사용 시 타입 직접 선택은 초기화
                        }
                        else
                        {
                            _monoScript = null;
                            _monoIsInvalidPick = true;
                        }
                    }
                }
                if (_monoIsInvalidPick)
                    EditorGUILayout.HelpBox("선택한 MonoScript가 Component 타입이 아닙니다.", MessageType.Warning);

                // 3) 타입명 직접 입력
                using (new EditorGUILayout.HorizontalScope())
                {
                    _typeName = EditorGUILayout.TextField(new GUIContent("Type Name", "예: BoxCollider, UnityEngine.BoxCollider, Namespace.MyComponent"), _typeName);
                    if (GUILayout.Button("Find Type", GUILayout.Width(90)))
                    {
                        _resolvedType = ResolveType();
                    }
                }
            }

            // 범위/필터
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scope & Filters", EditorStyles.boldLabel);
            _includeInactive = EditorGUILayout.Toggle(new GUIContent("Include Inactive", "비활성 오브젝트 포함"), _includeInactive);
            _searchInSelectionChildren = EditorGUILayout.Toggle(new GUIContent("Search Children", "선택 오브젝트의 자식 포함"), _searchInSelectionChildren);

            _filterByLayer = EditorGUILayout.Toggle(new GUIContent("Filter by Layer", "특정 레이어만 대상"), _filterByLayer);
            if (_filterByLayer)
            {
                // 다중 선택 지원 LayerMask UI
                _layerMask = EditorGUILayoutLayerMask.LayerFieldMask(new GUIContent("Layer Mask"), _layerMask);
            }

            _filterByTag = EditorGUILayout.Toggle(new GUIContent("Filter by Tag", "특정 태그만 대상"), _filterByTag);
            if (_filterByTag)
            {
                var tags = InternalEditorUtility.tags;
                if (tags == null || tags.Length == 0)
                {
                    EditorGUILayout.HelpBox("등록된 태그가 없습니다. Project Settings > Tags and Layers에서 태그를 추가하세요.", MessageType.Info);
                }
                else
                {
                    _tagIndex = EditorGUILayout.Popup("Tag", Mathf.Clamp(_tagIndex, 0, tags.Length - 1), tags);
                }
            }

            // 선택 안내 및 액션
            EditorGUILayout.Space();
            DrawSelectionInfo();

            using (new EditorGUILayout.HorizontalScope())
            {
                UnityEngine.GUI.enabled = CanSearch();
                if (GUILayout.Button("Preview Matches")) Preview();

                UnityEngine.GUI.enabled = _matches.Count > 0;
                if (GUILayout.Button(new GUIContent("Remove Components", "미리보기 결과를 기준으로 제거"))) RemoveNow();
                UnityEngine.GUI.enabled = true;
            }

            // 결과 리스트
            EditorGUILayout.Space();
            DrawMatches();
        }

        private void DrawSelectionInfo()
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorGUILayout.HelpBox("하나 이상의 GameObject를 선택하세요.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Selected Objects: {selected.Length}");
            if (!CanSearch())
            {
                var msg = _missingOnly ? "Missing Script 모드에서 Preview를 실행하세요." : "제거할 컴포넌트 타입을 지정하세요 (MonoScript/Type Name/브라우저).";
                EditorGUILayout.HelpBox(msg, MessageType.Warning);
            }
        }

        private void DrawMatches()
        {
            EditorGUILayout.LabelField($"Matches: {_matches.Count}", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var m in _matches)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(m.go, typeof(GameObject), true);
                    GUILayout.Label(m.ToString(), GUILayout.Width(200));
                    using (new GUIEnabledScope(false))
                    {
                        EditorGUILayout.ObjectField(m.component, typeof(Component), true);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        // ----------------------------------------------------------------------------
        // 검색/해석/제거 로직
        // ----------------------------------------------------------------------------
        private bool CanSearch()
        {
            if (_missingOnly) return true;

            if (_resolvedType != null && typeof(Component).IsAssignableFrom(_resolvedType)) return true;

            if (_monoScript != null)
            {
                var t = _monoScript.GetClass();
                if (t != null && typeof(Component).IsAssignableFrom(t)) return true;
            }

            if (!string.IsNullOrWhiteSpace(_typeName))
            {
                var t = ResolveType();
                if (t != null) return true;
            }

            return false;
        }

        private Type ResolveType()
        {
            // 1) 팝업으로 이미 선택한 타입이 있으면 최우선 사용
            if (_resolvedType != null && typeof(Component).IsAssignableFrom(_resolvedType))
                return _resolvedType;

            // 2) MonoScript가 지정된 경우 (Component만 허용 검증됨)
            if (_monoScript != null)
            {
                var t = _monoScript.GetClass();
                if (t != null && typeof(Component).IsAssignableFrom(t)) return t;
            }

            // 3) 문자열로 입력된 타입명 해석
            var name = _typeName?.Trim();
            if (string.IsNullOrEmpty(name)) return null;

            // FQN 정확 매치
            var exact = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => SafeGetTypes(a))
                .FirstOrDefault(t => typeof(Component).IsAssignableFrom(t) && t.FullName == name);
            if (exact != null) return exact;

            // 간단 이름 매치/부분 매치 후보
            var candidates = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => SafeGetTypes(a))
                .Where(t => typeof(Component).IsAssignableFrom(t) && (t.Name == name || t.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0))
                .Distinct()
                .ToList();

            if (candidates.Count == 1) return candidates[0];
            if (candidates.Count > 1)
            {
                var options = candidates.Select(t => t.FullName).ToArray();
                var choice = PopupListWindow.Show("Select Component Type", options);
                if (choice >= 0 && choice < candidates.Count) return candidates[choice];
            }

            return null;
        }

        private void Preview()
        {
            if (!_missingOnly)
                _resolvedType = ResolveType();

            _matches.Clear();

            var selection = Selection.gameObjects;
            if (selection == null || selection.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "하나 이상의 GameObject를 선택하세요.", "OK");
                return;
            }

            foreach (var root in selection)
            {
                if (root == null) continue;
                foreach (var go in CollectTargets(root, _searchInSelectionChildren, _includeInactive))
                {
                    if (!PassesFilters(go)) continue;

                    if (_missingOnly)
                    {
                        var comps = go.GetComponents<Component>();
                        for (int i = 0; i < comps.Length; i++)
                        {
                            if (comps[i] == null)
                            {
                                _matches.Add(new Match { go = go, component = null, isMissing = true });
                            }
                        }
                    }
                    else
                    {
                        if (_resolvedType == null) continue;
                        var comps = go.GetComponents(_resolvedType);
                        foreach (var c in comps)
                        {
                            if (c == null) continue;
                            _matches.Add(new Match { go = go, component = c, isMissing = false });
                        }
                    }
                }
            }
        }

        private bool PassesFilters(GameObject go)
        {
            if (_filterByLayer)
            {
                if (((1 << go.layer) & _layerMask.value) == 0) return false;
            }
            if (_filterByTag)
            {
                var tags = InternalEditorUtility.tags;
                if (tags == null || tags.Length == 0) return false;
                var tag = tags[Mathf.Clamp(_tagIndex, 0, tags.Length - 1)];
                if (!go.CompareTag(tag)) return false;
            }
            return true;
        }

        private void RemoveNow()
        {
            if (_matches.Count == 0)
            {
                EditorUtility.DisplayDialog("No Matches", "제거할 컴포넌트가 없습니다. 먼저 Preview 하세요.", "OK");
                return;
            }

            var title = _missingOnly ? "Remove Missing Scripts" : "Remove Components";
            if (!EditorUtility.DisplayDialog(title,
                    $"총 {_matches.Count}개의 대상이 검색되었습니다. 되돌리기는 Undo(Ctrl/Cmd+Z)로 가능합니다.",
                    "Proceed", "Cancel"))
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();

            int removed = 0;
            if (_missingOnly)
            {
                var perGo = _matches.Select(m => m.go).Distinct();
                foreach (var go in perGo)
                {
                    if (go == null) continue;
                    int before = go.GetComponents<Component>().Count(c => c == null);
                    if (before == 0) continue;
                    Undo.RegisterFullObjectHierarchyUndo(go, "Remove Missing Scripts");
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    int after = go.GetComponents<Component>().Count(c => c == null);
                    removed += Mathf.Max(0, before - after);
                }
            }
            else
            {
                foreach (var m in _matches)
                {
                    if (m == null || m.component == null) continue;
                    Undo.DestroyObjectImmediate(m.component);
                    removed++;
                }
            }

            Undo.CollapseUndoOperations(group);

            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Done", $"Removed {removed} component(s).", "OK");
            _matches.Clear();
        }

        // ----------------------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------------------
        private static IEnumerable<GameObject> CollectTargets(GameObject root, bool includeChildren, bool includeInactive)
        {
            if (root == null) yield break;
            yield return root;
            if (!includeChildren) yield break;
            foreach (var t in root.GetComponentsInChildren<Transform>(includeInactive))
            {
                if (t == null) continue;
                if (t.gameObject == root) continue; // root는 이미 포함됨
                yield return t.gameObject;
            }
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly a)
        {
            try { return a.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }
            catch { return Array.Empty<Type>(); }
        }

        // 컴포넌트 타입 전용 선택 팝업 (TypeCache 사용)
        private static class ComponentTypePicker
        {
            public static Type ShowPicker()
            {
                var all = UnityEditor.TypeCache.GetTypesDerivedFrom<Component>()
                    .Where(t => !t.IsAbstract && t.IsPublic)
                    .OrderBy(t => t.Namespace)
                    .ThenBy(t => t.Name)
                    .ToList();

                var display = all.Select(t => string.IsNullOrEmpty(t.Namespace) ? t.Name : $"{t.Namespace}.{t.Name}").ToArray();
                int idx = PopupListWindow.Show("Select Component Type", display);
                if (idx >= 0 && idx < all.Count) return all[idx];
                return null;
            }
        }

        // 작은 팝업 리스트 유틸
        private class PopupListWindow : EditorWindow
        {
            private string[] _options;
            private Action<int> _onPick;
            private Vector2 _scroll;

            public static int Show(string title, string[] options)
            {
                int picked = -1;
                var wnd = CreateInstance<PopupListWindow>();
                wnd.titleContent = new GUIContent(title);
                wnd._options = options ?? Array.Empty<string>();
                wnd._onPick = i => { picked = i; wnd.Close(); };
                wnd.position = new Rect(Screen.width / 2f, Screen.height / 2f, 420f, Mathf.Min(400f, 24f * (options?.Length ?? 1) + 16f));
                wnd.ShowModal();
                return picked;
            }

            private void OnGUI()
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                for (int i = 0; i < _options.Length; i++)
                {
                    if (GUILayout.Button(_options[i], GUILayout.Height(22)))
                    {
                        _onPick?.Invoke(i);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private readonly struct GUIEnabledScope : IDisposable
        {
            private readonly bool _prev;
            public GUIEnabledScope(bool enabled)
            {
                _prev = UnityEngine.GUI.enabled;
                UnityEngine.GUI.enabled = enabled;
            }
            public void Dispose() { UnityEngine.GUI.enabled = _prev; }
        }
    }

    // LayerMask를 멀티 셀렉트로 보여주는 확장 유틸
    public static class EditorGUILayoutLayerMask
    {
        /// <summary>
        /// Unity가 기본 제공하지 않는 LayerMask용 GUI를 간단히 만들어 제공합니다 (MaskField 기반).
        /// </summary>
        public static LayerMask LayerFieldMask(GUIContent label, LayerMask selected)
        {
            var layers = Enumerable.Range(0, 32)
                .Select(i => new { index = i, name = LayerMask.LayerToName(i) })
                .Where(x => !string.IsNullOrEmpty(x.name))
                .ToArray();

            var names = layers.Select(l => l.name).ToArray();
            var indices = layers.Select(l => l.index).ToArray();

            int mask = 0; // UI용 인덱스 기반 임시 마스크
            for (int i = 0; i < indices.Length; i++)
            {
                if (((1 << indices[i]) & selected.value) != 0)
                    mask |= 1 << i;
            }

            int newMask = EditorGUILayout.MaskField(label, mask, names);

            int result = 0;
            for (int i = 0; i < indices.Length; i++)
            {
                if ((newMask & (1 << i)) != 0)
                    result |= 1 << indices[i];
            }
            selected.value = result;
            return selected;
        }
    }
}
#endif
