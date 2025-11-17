#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using Jeomseon.Editor;
using Jeomseon.Editor.Extensions;

namespace Jeomseon.Attribute.Editor
{
    using GUI = UnityEngine.GUI;

    [CustomPropertyDrawer(typeof(SelectableSerializeFieldAttribute), true)]
    internal sealed class SelectableSerializeFieldDrawer : PropertyDrawer
    {
        public readonly struct SelectableFieldPropState
        {
            public ComponentDropdown Dropdown { get; }
            public bool IsMonoBehaviour { get; }

            public SelectableFieldPropState(ComponentDropdown dropdown, bool isMonoBehaviour)
            {
                Dropdown = dropdown;
                IsMonoBehaviour = isMonoBehaviour;
            }
        }

        private readonly TreeViewState _dropdownState = new();
        private GUIContent _buttonContent;

        // SerializedProperty 인스턴스는 매 프레임 바뀌니까 propertyPath 기준으로 캐싱
        private readonly Dictionary<string, SelectableFieldPropState> _stateCache = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _buttonContent ??= new(EditorGUIUtility.IconContent("icon dropdown").image);

            // 이 Attribute는 MonoBehaviour 안에서만 유효
            if (property.serializedObject.targetObject is not MonoBehaviour monoBehaviour)
            {
                EditorGUI.HelpBox(position, "SelectableSerializeField는 MonoBehaviour에서만 사용할 수 있습니다.", MessageType.Error);
                return;
            }

            // 필드 타입 / 요소 타입 계산
            Type fieldType = fieldInfo.FieldType;
            bool isListType  = fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>);
            bool isArrayType = fieldType.IsArray;

            Type elementType = fieldType;
            if (isListType)
                elementType = fieldType.GetGenericArguments()[0];
            else if (isArrayType)
                elementType = fieldType.GetElementType();

            // 실제로 선택/할당할 타입 (단일 필드면 fieldType, 리스트/배열이면 elementType)
            Type referenceType = (isListType || isArrayType) ? elementType : fieldType;

            // UnityEngine.Object 가 아니면 사용할 수 없음
            if (!typeof(UnityEngine.Object).IsAssignableFrom(referenceType))
            {
                EditorGUI.HelpBox(position,
                    "SelectableSerializeField는 UnityEngine.Object 타입(또는 그 List/Array)에만 사용할 수 있습니다.",
                    MessageType.Error);
                return;
            }

            // 요소/단일 모두 SerializedProperty는 ObjectReference 여야 한다
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            string key = property.propertyPath;

            if (!_stateCache.TryGetValue(key, out var state))
            {
                var dropdown = new ComponentDropdown(_dropdownState, monoBehaviour.gameObject, referenceType, go =>
                {
                    if (referenceType == typeof(GameObject))
                    {
                        property.objectReferenceValue = go;
                    }
                    else
                    {
                        property.objectReferenceValue = go.GetComponent(referenceType);
                    }

                    property.serializedObject.ApplyModifiedProperties();
                });

                state = new SelectableFieldPropState(dropdown, true);
                _stateCache[key] = state;
            }

            if (!state.IsMonoBehaviour)
            {
                EditorGUI.HelpBox(position, "this object is not MonoBehaviour!", MessageType.Error);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            // ┌──── label ────┬─ btn ─┬─────── object field ───────┐
            Rect labelRect  = new(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            Rect buttonRect = new(labelRect.xMax, position.y, 18f, position.height);
            Rect fieldRect  = new(
                buttonRect.xMax + 2f,
                position.y,
                position.width - (buttonRect.xMax - position.x),
                position.height);

            EditorGUI.LabelField(labelRect, label);

            if (GUI.Button(buttonRect, _buttonContent))
            {
                state.Dropdown.Show(buttonRect);
            }

            GUI.enabled = false;
            EditorGUI.PropertyField(fieldRect, property, GUIContent.none, true);
            GUI.enabled = true;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 요소/단일 모두 Unity 기본 높이를 그대로 사용
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
#endif
