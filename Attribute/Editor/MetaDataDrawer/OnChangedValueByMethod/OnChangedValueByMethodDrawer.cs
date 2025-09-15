#if UNITY_EDITOR
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
            initialize(editor);
        }

        private void initialize(Editor editor)
        {
            if (Application.isPlaying) return;

            _observerMethods.Clear();
            _prevProperties.Clear();

            foreach (Action action in EditorReflectionHelper.GetActionsFromAttributeAllSearch<OnChangedValueByMethodAttribute>(editor.target))
            {
                OnChangedValueByMethodAttribute onValueChangedByMethodAttribute = action.Method.GetCustomAttribute<OnChangedValueByMethodAttribute>();

                foreach (string fieldName in onValueChangedByMethodAttribute.FieldName)
                {
                    if (!_observerMethods.TryGetValue(fieldName, out PropertyMethodPair methods))
                    {
                        FieldInfo targetField = getFieldInfo(action, fieldName) ?? getFieldInfo(action, EditorReflectionHelper.GetBackingFieldName(fieldName));

                        if (targetField == null)
                        {
                            Debug.LogWarning("Not found Target Property!");
                            continue;
                        }

                        methods = new(new(), targetField, action.Target);
                        _observerMethods.Add(fieldName, methods);
                    }

                    methods.Methods.Add(action);
                }
            }

            _prevProperties.AddRange(_observerMethods
                .Select(pair => new KeyValuePair<string, string>(
                    pair.Key,
                    GetPropertyValueFromObject(pair.Value.TargetField.GetValue(pair.Value.TargetInstance))?.ToString() ?? "NULL")));

            static FieldInfo getFieldInfo(Action action, string fieldName)
            {
                return action
                    .Target
                    .GetType()
                    .GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            }
        }

        public void OnInspectorGUI(Editor editor)
        {
            if (Application.isPlaying) return;

            bool isUpdate = false;
            using (StringBuilderPoolScope scope = new())
            {
                StringBuilder builder = scope.Get();

                for (int i = 0; i < _prevProperties.Count; i++)
                {
                    string key = _prevProperties[i].Key;
                    PropertyMethodPair propertyMethodPair = _observerMethods[key];
                    object target = propertyMethodPair.TargetField.GetValue(propertyMethodPair.TargetInstance);
                    bool isCollection = false;

                    builder.Append(GetPropertyValueFromObject(target)?.ToString() ?? "NULL");

                    if (target is ICollection collection)
                    {
                        isCollection = true;
                        builder.Append(computeCollectionState(collection));
                    }

                    string nowValue = builder.ToString();

                    if (_prevProperties[i].Value != nowValue)
                    {
                        isUpdate = isCollection;

                        EditorCoroutineUtility.StartCoroutineOwnerless(iEInvokeMethod(propertyMethodPair));
                        _prevProperties[i] = new(key, nowValue);
                    }
                }

                builder.Clear();
            }

            if (isUpdate)
            {
                initialize(editor);
            }

            static IEnumerator iEInvokeMethod(PropertyMethodPair propertyMethodPair)
            {
                yield return null;
                propertyMethodPair.Methods.ForEach(method => method.Invoke());
            }

            static string computeCollectionState(ICollection collection)
            {
                IEnumerable<string> elementStates = collection.Cast<object>().Select(e => e?.GetHashCode().ToString() ?? "null");
                return string.Join(",", elementStates);
            }
        }
    }
}
#endif