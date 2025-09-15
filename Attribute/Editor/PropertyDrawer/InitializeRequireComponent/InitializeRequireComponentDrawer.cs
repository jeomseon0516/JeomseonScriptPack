#if UNITY_EDITOR
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using Jeomseon.Editor.Extensions;
using Jeomseon.Editor;

namespace Jeomseon.Attribute.Editor
{
    [CustomPropertyDrawer(typeof(InitializeRequireComponentAttribute), false)]
    internal sealed class InitializeRequireComponentDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = true;

            Type requireComponentType = property.GetPropertyType();

            if (requireComponentType is null)
            {
                Debug.LogWarning("Component Type is null");
                return;
            }
            
            if (requireComponentType.IsAssignableFrom(typeof(Component)))
            {
                Debug.LogWarning("Type is not component");
                return;
            }
            
            if (!property.IsNestedAttribute<SerializeField>())
            {
                Debug.LogWarning("check in Contain SerializeField Attribute");
                return;
            }

            if (property.serializedObject.targetObject is not Component component)
            {
                Debug.LogWarning("This Attribute not in Component Context");
                return;
            }

            if (!property.objectReferenceValue)
            {
                if (!component.TryGetComponent(requireComponentType, out Component requireComponent))
                {
                    requireComponent = component.gameObject.AddComponent(requireComponentType);
                }
                
                if (property.objectReferenceValue != requireComponent)
                {
                    property.objectReferenceValue = requireComponent;
                    property.serializedObject.ApplyModifiedProperties();
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