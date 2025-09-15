#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Jeomseon.Attribute.Editor
{
    [CustomPropertyDrawer(typeof(OnwMaxAttribute), true)]
    internal sealed class OnwMaxAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            OnwMaxAttribute maxAttribute = (attribute as OnwMaxAttribute)!;

            EditorGUI.BeginChangeCheck();
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                    {
                        float value = EditorGUI.FloatField(position, label, property.floatValue);
                        if (EditorGUI.EndChangeCheck())
                        {
                            property.floatValue = Mathf.Max(value, maxAttribute.Max);
                        }
                        break;
                    }
                case SerializedPropertyType.Integer:
                    {
                        int value = EditorGUI.IntField(position, label, property.intValue);
                        if (EditorGUI.EndChangeCheck())
                        {
                            property.intValue = Mathf.Max(value, (int)maxAttribute.Max);
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
#endif
