#if UNITY_EDITOR
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Jeomseon.Editor;
using Jeomseon.Editor.Extensions;
using Jeomseon.Helper;

namespace Jeomseon.Attribute.Editor
{
    [CustomPropertyDrawer(typeof(OnChangedValueByValueAttribute))]
    internal sealed class OnChangedValueByValueDrawer : PropertyDrawer
    {
        private const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly Dictionary<object, MethodInfo> _cachedMethodInfos = new();
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (Application.isPlaying) return;

            OnChangedValueByValueAttribute castedAttribute = (attribute as OnChangedValueByValueAttribute)!;
            
            string path = property.GetParentPropertyPath();
            SerializedProperty parentProp = property.serializedObject.FindProperty(path);
            object target = parentProp.objectReferenceValue;
            
            if (!_cachedMethodInfos.TryGetValue(target, out MethodInfo methodInfo))
            {
                methodInfo = target.GetType().GetMethod(castedAttribute.MethodName, FLAGS);
                _cachedMethodInfos.Add(target, methodInfo);
            }
            
            using EditorGUI.ChangeCheckScope scope = new();
            EditorGUI.PropertyField(position, property, label, true);

            if (scope.changed)
            {
                if (methodInfo is not null)
                {
                    methodInfo.Invoke(target, null);
                }
                else
                {
                    Debug.LogWarning($"Method {castedAttribute.MethodName} not found on {target.GetType().Name}");
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
#endif
