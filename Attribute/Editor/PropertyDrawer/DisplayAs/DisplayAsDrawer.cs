#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Jeomseon.Attribute;

namespace Jeomseon.Attribute.Editor
{
    [CustomPropertyDrawer(typeof(DisplayAsAttribute), true)]
    internal sealed class DisplayAsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DisplayAsAttribute displayAs = (attribute as DisplayAsAttribute)!;

            // 라벨 텍스트 설정
            label.text = displayAs.DisplayName;

            // 라벨의 너비를 동적으로 설정
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Min(
                EditorGUIUtility.currentViewWidth * 0.5f, 
                EditorStyles.label.CalcSize(label).x + 10);

            // 필드 그리기
            EditorGUI.PropertyField(position, property, label, true);

            // 원래 라벨 너비로 복구
            EditorGUIUtility.labelWidth = originalLabelWidth;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
#endif