#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Jeomseon.Attribute.Editor
{
    [CustomPropertyDrawer(typeof(Vector2RangeAttribute), true)]
    internal sealed class Vector2RangePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Vector2RangeAttribute range = attribute as Vector2RangeAttribute;

            Rect labelRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, label);

            Rect sliderRectX = new(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
            Rect sliderRectY = new(position.x, sliderRectX.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);

            Vector2 vector = property.vector2Value;

            EditorGUI.indentLevel++;
            vector.x = EditorGUI.Slider(sliderRectX, "X", vector.x, range.Min, range.Max);
            vector.y = EditorGUI.Slider(sliderRectY, "Y", vector.y, range.Min, range.Max);
            EditorGUI.indentLevel--;

            property.vector2Value = vector;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 2;
        }
    }
}
#endif