#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Jeomseon.Attribute.Editor
{
    [CustomPropertyDrawer(typeof(OnwMinAttribute), true)]
    internal sealed class OnwMinAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            OnwMinAttribute minAttribute = (attribute as OnwMinAttribute)!;

            EditorGUI.BeginChangeCheck();
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                    float fValue = EditorGUI.FloatField(position, label, property.floatValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        property.floatValue = Mathf.Max(fValue, minAttribute.Min);
                    }
                    break;
                case SerializedPropertyType.Integer:
                    int iValue = EditorGUI.IntField(position, label, property.intValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        property.intValue = Mathf.Max(iValue, (int)minAttribute.Min);
                    }
                    break;
                default:
                    EditorGUI.PropertyField(position, property, label, true);
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