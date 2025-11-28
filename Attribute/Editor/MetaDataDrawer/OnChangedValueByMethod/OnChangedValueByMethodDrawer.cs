#if UNITY_EDITOR && !UNITY_6000_0_OR_NEWER
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using Jeomseon.Scope;
using Jeomseon.Editor;
using static Jeomseon.Editor.EditorHelper;

namespace Jeomseon.Attribute.Editor
{
    using Editor = UnityEditor.Editor;

    internal sealed class OnChangedValueByMethodDrawer : IObjectEditorAttributeDrawer
    {
        private readonly struct PropertyMethodPair
        {
            public List<Action> Methods { get; }
            public FieldInfo TargetField { get; }
            public object TargetInstance { get; }

            public PropertyMethodPair(List<Action> methods, FieldInfo targetField, object targetInstance)
            {
                Methods = methods;
                TargetField = targetField;
                TargetInstance = targetInstance;
            }
        }

        private readonly Dictionary<string, PropertyMethodPair> _observerMethods = new();
        private readonly List<KeyValuePair<string, string>> _prevProperties = new();

        public void OnEnable(Editor editor)
        {
            Initialize(editor);
        }

        private void Initialize(Editor editor)
        {
            if (Application.isPlaying) return;

            _observerMethods.Clear();
            _prevProperties.Clear();

            foreach (Action action in EditorReflectionHelper
                         .GetActionsFromAttributeAllSearch<OnChangedValueByMethodAttribute>(editor.target))
            {
                var attr = action.Method.GetCustomAttribute<OnChangedValueByMethodAttribute>();
                if (attr == null) continue;

                foreach (string fieldName in attr.FieldName)
                {
                    if (!_observerMethods.TryGetValue(fieldName, out PropertyMethodPair pair))
                    {
                        FieldInfo targetField =
                            GetFieldInfo(action, fieldName) ??
                            GetFieldInfo(action, EditorReflectionHelper.GetBackingFieldName(fieldName));

                        if (targetField == null)
                        {
                            Debug.LogWarning($"[OnChangedValueByMethodDrawer] Not found target field: {fieldName}");
                            continue;
                        }

                        pair = new PropertyMethodPair(new List<Action>(), targetField, action.Target);
                        _observerMethods.Add(fieldName, pair);
                    }

                    // struct지만 내부 List는 참조 타입이라 Add는 양쪽에서 공유됨
                    pair.Methods.Add(action);
                }
            }

            _prevProperties.AddRange(_observerMethods
                .Select(pair =>
                    new KeyValuePair<string, string>(
                        pair.Key,
                        GetPropertyValueFromObject(pair.Value.TargetField.GetValue(pair.Value.TargetInstance))
                            ?.ToString() ?? "NULL")));

            static FieldInfo GetFieldInfo(Action action, string fieldName)
            {
                if (action == null || action.Target == null || string.IsNullOrEmpty(fieldName))
                    return null;

                Type type = action.Target.GetType();

                return type.GetField(fieldName,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static |
                    BindingFlags.Instance);
            }
        }

        public void OnInspectorGUI(Editor editor)
        {
            if (Application.isPlaying) return;

            bool requireReinitialize = false;

            using (StringBuilderPoolScope scope = new())
            {
                StringBuilder builder = scope.Get();

                for (int i = 0; i < _prevProperties.Count; i++)
                {
                    string key = _prevProperties[i].Key;

                    if (!_observerMethods.TryGetValue(key, out PropertyMethodPair pair))
                        continue;

                    object targetValue = pair.TargetField.GetValue(pair.TargetInstance);
                    bool isCollection = false;

                    builder.Append(GetPropertyValueFromObject(targetValue)?.ToString() ?? "NULL");

                    if (targetValue is ICollection collection)
                    {
                        isCollection = true;
                        builder.Append(ComputeCollectionState(collection));
                    }

                    string nowValue = builder.ToString();
                    builder.Clear(); // ★ 중요: 프로퍼티마다 초기화

                    if (_prevProperties[i].Value != nowValue)
                    {
                        if (isCollection)
                            requireReinitialize = true;

                        EditorCoroutineUtility.StartCoroutineOwnerless(IEInvokeMethod(pair));
                        _prevProperties[i] = new KeyValuePair<string, string>(key, nowValue);
                    }
                }
            }

            if (requireReinitialize)
            {
                Initialize(editor);
            }

            static IEnumerator IEInvokeMethod(PropertyMethodPair pair)
            {
                // 한 프레임 늦게 호출 (기존 동작 유지)
                yield return null;

                pair.Methods.ForEach(method =>
                {
                    try
                    {
                        method.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                });
            }

            static string ComputeCollectionState(ICollection collection)
            {
                // 컬렉션 요소들의 HashCode를 이어붙여서 상태 문자열 생성
                IEnumerable<string> elementStates =
                    collection.Cast<object>()
                              .Select(e => e?.GetHashCode().ToString() ?? "null");

                return string.Join(",", elementStates);
            }
        }
    }
}

#elif UNITY_EDITOR && UNITY_6000_0_OR_NEWER

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Jeomseon.Scope;
using Jeomseon.Editor;
using static Jeomseon.Editor.EditorHelper;

namespace Jeomseon.Attribute.Editor
{
    using Editor = UnityEditor.Editor;

    /// <summary>
    /// Unity 6000.x 이상에서 OnChangedValueByMethodAttribute를 처리하는 전역 핸들러.
    /// InspectorWindow 내부 구조를 건드리지 않고 Editor.finishedDefaultHeaderGUI 기반으로 동작.
    /// </summary>
    [InitializeOnLoad]
    internal static class OnChangedValueByMethodHeaderWatcher
    {
        /// <summary>
        /// 타입별로, 어떤 메서드가 어떤 필드를 감시하는지 캐시
        /// (리플렉션 비용 최소화용)
        /// </summary>
        private sealed class MethodWatchInfo
        {
            public MethodInfo Method;
            public string[] FieldNames;
        }

        /// <summary>
        /// 인스턴스별 감시 상태
        /// </summary>
        private sealed class TargetWatchState
        {
            public UnityEngine.Object Target; // 실제 타겟
            public Dictionary<string, FieldWatchState> FieldStates = new();
        }

        private sealed class FieldWatchState
        {
            public FieldInfo Field;
            public List<Action> Methods = new();
            public string PrevValue;
        }

        // 타입 -> (OnChangedValueByMethod 붙은 메서드들)
        private static readonly Dictionary<Type, List<MethodWatchInfo>> _typeCache = new();

        // 인스턴스ID -> 감시 상태
        private static readonly Dictionary<int, TargetWatchState> _targetStates = new();

        static OnChangedValueByMethodHeaderWatcher()
        {
            // 헤더가 그려질 때마다 호출됨 (Repaint 시점에 안정적으로 들어옴)
            Editor.finishedDefaultHeaderGUI += OnFinishedDefaultHeaderGUI;
        }

        private static void OnFinishedDefaultHeaderGUI(Editor editor)
        {
            if (editor == null)
                return;

            if (Application.isPlaying)
                return;

            // MonoBehaviour / ScriptableObject 계열만 지원
            if (editor.target is not MonoBehaviour && editor.target is not ScriptableObject)
                return;

            // 멀티 선택 고려
            foreach (var t in editor.targets)
            {
                if (t is not UnityEngine.Object obj) continue;
                ProcessTarget(obj);
            }
        }

        private static void ProcessTarget(UnityEngine.Object target)
        {
            if (target == null)
                return;

            int id = target.GetInstanceID();

            if (!_targetStates.TryGetValue(id, out var state) || state.Target == null)
            {
                state = BuildWatchState(target);
                if (state == null)
                    return;

                _targetStates[id] = state;
            }

            UpdateWatchState(state);
        }

        /// <summary>
        /// 타겟에 대해 감시할 필드/메서드 관계를 초기화.
        /// </summary>
        private static TargetWatchState BuildWatchState(UnityEngine.Object target)
        {
            var type = target.GetType();

            if (!_typeCache.TryGetValue(type, out var methodInfos))
            {
                methodInfos = CollectMethodWatchInfos(type);
                _typeCache[type] = methodInfos;
            }

            if (methodInfos == null || methodInfos.Count == 0)
                return null;

            var state = new TargetWatchState { Target = target };

            foreach (var methodInfo in methodInfos)
            {
                // 이 타겟 인스턴스를 대상으로 하는 delegate
                var action = Delegate.CreateDelegate(typeof(Action), target, methodInfo.Method, false) as Action;
                if (action == null) continue;

                foreach (string fieldName in methodInfo.FieldNames)
                {
                    if (string.IsNullOrEmpty(fieldName))
                        continue;

                    if (!state.FieldStates.TryGetValue(fieldName, out var fieldState))
                    {
                        FieldInfo field =
                            GetField(type, fieldName) ??
                            GetField(type, EditorReflectionHelper.GetBackingFieldName(fieldName));

                        if (field == null)
                        {
                            Debug.LogWarning($"[OnChangedValueByMethodHeaderWatcher] Not found target field: {fieldName} on {type.Name}");
                            continue;
                        }

                        fieldState = new FieldWatchState
                        {
                            Field = field,
                            PrevValue = ComputeValueFingerprint(field.GetValue(target))
                        };

                        state.FieldStates.Add(fieldName, fieldState);
                    }

                    fieldState.Methods.Add(action);
                }
            }

            if (state.FieldStates.Count == 0)
                return null;

            return state;
        }

        /// <summary>
        /// 타입에서 속성 붙은 메서드들을 수집 (1회만 리플렉션).
        /// </summary>
        private static List<MethodWatchInfo> CollectMethodWatchInfos(Type type)
        {
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var result = new List<MethodWatchInfo>();

            foreach (var method in type.GetMethods(flags))
            {
                var attr = method.GetCustomAttribute<OnChangedValueByMethodAttribute>();
                if (attr == null)
                    continue;

                if (method.GetParameters().Length > 0)
                    continue;

                var info = new MethodWatchInfo
                {
                    Method = method,
                    FieldNames = attr.FieldName ?? Array.Empty<string>()
                };

                result.Add(info);
            }

            return result;
        }

        private static FieldInfo GetField(Type type, string fieldName)
        {
            if (type == null || string.IsNullOrEmpty(fieldName))
                return null;

            return type.GetField(fieldName,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.Static);
        }

        /// <summary>
        /// 한 타겟 인스턴스에 대해, 감시 중인 필드 값이 바뀌었는지 체크하고 메서드 호출.
        /// </summary>
        private static void UpdateWatchState(TargetWatchState state)
        {
            if (state.Target == null)
                return;

            var target = state.Target;

            using (StringBuilderPoolScope scope = new())
            {
                StringBuilder builder = scope.Get();

                foreach (var pair in state.FieldStates)
                {
                    var fieldState = pair.Value;
                    object value = fieldState.Field.GetValue(target);

                    string now = ComputeValueFingerprint(value, builder);

                    if (fieldState.PrevValue != now)
                    {
                        fieldState.PrevValue = now;

                        // 한 틱 뒤에 실행해서 인스펙터 그리기와 분리 (기존 코루틴 패턴과 비슷한 타이밍)
                        var methods = fieldState.Methods.ToArray();
                        EditorApplication.delayCall += () =>
                        {
                            foreach (var action in methods)
                            {
                                try
                                {
                                    action.Invoke();
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogException(ex);
                                }
                            }

                            // 변경사항 반영 필요 시
                            if (target != null)
                            {
                                EditorUtility.SetDirty(target);
                            }
                        };
                    }
                }
            }
        }

        /// <summary>
        /// 값의 "지문" 문자열을 생성해서 이전 값과 비교에 사용.
        /// </summary>
        private static string ComputeValueFingerprint(object value, StringBuilder reusableBuilder = null)
        {
            if (reusableBuilder == null)
                reusableBuilder = new StringBuilder();
            else
                reusableBuilder.Clear();

            reusableBuilder.Append(GetPropertyValueFromObject(value)?.ToString() ?? "NULL");

            if (value is ICollection collection)
            {
                reusableBuilder.Append('|');
                reusableBuilder.Append(collection.Count);

                // 너무 큰 컬렉션일 경우 전체를 다 돌면 부담이 될 수 있으므로
                // 필요하다면 여기서 상한(limit)를 두는 것도 고려 가능
                foreach (object element in collection)
                {
                    reusableBuilder.Append(',');
                    reusableBuilder.Append(element?.GetHashCode().ToString() ?? "null");
                }
            }

            return reusableBuilder.ToString();
        }

        // ComputeValueFingerprint를 초기 상태용으로도 사용하기 위해 오버로드 하나 더
        private static string ComputeValueFingerprint(object value)
        {
            return ComputeValueFingerprint(value, new StringBuilder());
        }
    }
}

#endif
