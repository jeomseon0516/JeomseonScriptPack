#if UNITY_EDITOR && !UNITY_6000_0_OR_NEWER
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Jeomseon.Extensions;
using Jeomseon.Editor;

namespace Jeomseon.Attribute.Editor
{
    using Editor = UnityEditor.Editor;

    internal sealed class InspectorButtonAttributeDrawer : IObjectEditorAttributeDrawer
    {
        private readonly List<KeyValuePair<string, MethodInfo>> _buttonMethods = new();

        public void OnEnable(Editor editor)
        {
            _buttonMethods.AddRange(EditorReflectionHelper
                .GetMethodsFromAttribute<InspectorButtonAttribute>(editor.target)
                .Select(method =>
                    new KeyValuePair<string, MethodInfo>(
                        method.GetCustomAttribute<InspectorButtonAttribute>().ButtonName,
                        method)));
        }

        public void OnInspectorGUI(Editor editor)
        {
            _buttonMethods
                .Where(kvp => GUILayout.Button(kvp.Key))
                .ForEach(kvp => kvp.Value.Invoke(editor.target, null));
        }
    }
}
#elif UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Jeomseon.Attribute.Editor
{
    using Editor = UnityEditor.Editor;

    /// <summary>
    /// Unity 6000.x 이상에서 InspectorButtonAttribute를 처리하는 전역 드로어.
    /// InspectorWindow 내부 구조를 건드리지 않고
    /// Editor.finishedDefaultHeaderGUI 공식 API만 사용한다.
    /// </summary>
    [InitializeOnLoad]
    internal static class InspectorButtonHeaderDrawer
    {
        // 타입별로 버튼 메서드 캐시 (라벨, 메서드)
        private static readonly Dictionary<Type, List<(string label, MethodInfo method)>> _cache
            = new();

        static InspectorButtonHeaderDrawer()
        {
            // 모든 인스펙터 헤더가 그려진 뒤 호출되는 이벤트에 구독
            Editor.finishedDefaultHeaderGUI += OnFinishedDefaultHeaderGUI;
        }

        private static void OnFinishedDefaultHeaderGUI(Editor editor)
        {
            if (editor == null)
                return;

            // 대상이 MonoBehaviour 또는 ScriptableObject일 때만 처리
            var target = editor.target;
            if (target is not MonoBehaviour && target is not ScriptableObject)
                return;

            Type targetType = target.GetType();

            if (!_cache.TryGetValue(targetType, out var buttonMethods))
            {
                buttonMethods = CollectButtonMethods(targetType);
                _cache[targetType] = buttonMethods;
            }

            if (buttonMethods == null || buttonMethods.Count == 0)
                return;

            // 기본 헤더와의 간격 살짝
            GUILayout.Space(4f);

            // 한 줄에 버튼들 배치
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var (label, method) in buttonMethods)
                {
                    if (GUILayout.Button(label))
                    {
                        InvokeForAllTargets(editor, method);
                    }
                }
            }
        }

        /// <summary>
        /// 타입에서 InspectorButtonAttribute가 달린 메서드들을 수집.
        /// </summary>
        private static List<(string label, MethodInfo method)> CollectButtonMethods(Type type)
        {
            var list = new List<(string, MethodInfo)>();

            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var methods = type.GetMethods(flags);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<InspectorButtonAttribute>();
                if (attr == null)
                    continue;

                // 파라미터 있는 메서드는 무시 (필요하면 확장 가능)
                if (method.GetParameters().Length > 0)
                    continue;

                string label = string.IsNullOrEmpty(attr.ButtonName)
                    ? method.Name
                    : attr.ButtonName;

                list.Add((label, method));
            }

            return list;
        }

        /// <summary>
        /// 멀티 오브젝트 선택 시, 모든 target에 대해 메서드 호출.
        /// </summary>
        private static void InvokeForAllTargets(Editor editor, MethodInfo method)
        {
            var targets = editor.targets;
            foreach (var t in targets)
            {
                try
                {
                    method.Invoke(t, null);

                    // 변경사항 반영이 필요하면 MarkDirty
                    if (t is UnityEngine.Object obj)
                    {
                        EditorUtility.SetDirty(obj);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
#endif

