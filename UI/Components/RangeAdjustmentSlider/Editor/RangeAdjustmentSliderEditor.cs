#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Jeomseon.UI.Components;
using static Jeomseon.Editor.EditorGUIHelper;
using static Jeomseon.Editor.EditorReflectionHelper;

[CustomEditor(typeof(RangeAdjustmentSlider))]
internal sealed class RangeAdjustmentSliderEditor : Editor
{
    private sealed class DivideOption
    {
        public SerializedProperty IsDivide { get; set; }
        public SerializedProperty DivideValue { get; set; }
        public string[] DivideOptions { get; } = { "NONE", "DIVIDE" };
    }

    private RangeAdjustmentSlider _rangeAdjustmentSlider = null;

    private SerializedProperty _leftHandle;
    private SerializedProperty _rightHandle;

    private SerializedProperty _backgroundBar;
    private SerializedProperty _frontBar;

    private SerializedProperty _leftValue;
    private SerializedProperty _rightValue;

    private SerializedProperty _leftIntValue;
    private SerializedProperty _rightIntValue;

    private SerializedProperty _handleSizeRatio;
    private SerializedProperty _targetCamera;

    private SerializedProperty _onChangedLeftIntValue;
    private SerializedProperty _onChangedRightIntValue;
    private SerializedProperty _onChangedLeftValue;
    private SerializedProperty _onChangedRightValue;

    private readonly DivideOption _divideOption = new();

    [MenuItem("GameObject/Create/UI/Range Adjustment Slider")]
    public static void CreateRangeAdjustment(MenuCommand menuCommand)
    {
        GameObject rangeAdjustmentSliderPrefab = Resources.Load<GameObject>("RangeAdjustmentSlider");
        GameObject newUIElement = Instantiate(rangeAdjustmentSliderPrefab, (menuCommand.context as GameObject).transform);
        newUIElement.name = "RangeAdjustmentSlider";

        Undo.RegisterCreatedObjectUndo(newUIElement, "Create Range Adjustment Slider");
        Selection.activeObject = newUIElement;
    }

    private void OnEnable()
    {
        _rangeAdjustmentSlider = target as RangeAdjustmentSlider;
        _leftHandle = serializedObject.FindProperty(GetBackingFieldName("LeftHandle"));
        _rightHandle = serializedObject.FindProperty(GetBackingFieldName("RightHandle"));
        _backgroundBar = serializedObject.FindProperty(GetBackingFieldName("BackgroundBar"));
        _frontBar = serializedObject.FindProperty(GetBackingFieldName("FrontBar"));
        _leftValue = serializedObject.FindProperty("_leftValue");
        _rightValue = serializedObject.FindProperty("_rightValue");
        _leftIntValue = serializedObject.FindProperty("_leftIntValue");
        _rightIntValue = serializedObject.FindProperty("_rightIntValue");
        _handleSizeRatio = serializedObject.FindProperty(GetBackingFieldName("HandleSizeRatio"));
        _divideOption.IsDivide = serializedObject.FindProperty(GetBackingFieldName("IsDivide"));
        _divideOption.DivideValue = serializedObject.FindProperty(GetBackingFieldName("DivideValue"));
        _targetCamera = serializedObject.FindProperty(GetBackingFieldName("TargetCamera"));
        _onChangedLeftIntValue = serializedObject.FindProperty(GetBackingFieldName("OnChangedLeftIntValue"));
        _onChangedRightIntValue = serializedObject.FindProperty(GetBackingFieldName("OnChangedRightIntValue"));
        _onChangedLeftValue = serializedObject.FindProperty(GetBackingFieldName("OnChangedLeftValue"));
        _onChangedRightValue = serializedObject.FindProperty(GetBackingFieldName("OnChangedRightValue"));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_targetCamera, new GUIContent("Camera"), true);

        ActionEditorVertical(() =>
        {
            _divideOption.IsDivide.boolValue = EditorGUILayout.Popup("Divide Mode", _divideOption.IsDivide.boolValue ? 1 : 0, _divideOption.DivideOptions) != 0;

            if (_divideOption.IsDivide.boolValue)
            {
                _divideOption.DivideValue.intValue = EditorGUILayout.IntSlider("Divide", _divideOption.DivideValue.intValue, 2, 999);
            }
        }, GUI.skin.box);

        EditorGUILayout.LabelField("Handle");
        ActionEditorVertical(() =>
        {
            EditorGUILayout.PropertyField(_leftHandle, new GUIContent("Left Handle"), true);
            EditorGUILayout.PropertyField(_rightHandle, new GUIContent("Right Handle"), true);

            _handleSizeRatio.floatValue = EditorGUILayout.Slider("Handle Size Ratio", _handleSizeRatio.floatValue, 1.5f, 4.0f);
        }, GUI.skin.box);

        EditorGUILayout.LabelField("Slider Image");
        ActionEditorVertical(() =>
        {
            EditorGUILayout.PropertyField(_backgroundBar, new GUIContent("Background Bar"), true);
            EditorGUILayout.PropertyField(_frontBar, new GUIContent("Front Bar"), true);
        }, GUI.skin.box);

        EditorGUILayout.LabelField("Value");
        ActionEditorVertical(() =>
        {
            if (_leftHandle.objectReferenceValue && _rightHandle.objectReferenceValue)
            {
                Image leftHandle = _leftHandle.objectReferenceValue as Image;
                Image rightHandle = _rightHandle.objectReferenceValue as Image;

                leftHandle.rectTransform.anchorMin = new(0.5f, 0.5f);
                leftHandle.rectTransform.anchorMax = new(0.5f, 0.5f);

                rightHandle.rectTransform.anchorMin = new(0.5f, 0.5f);
                rightHandle.rectTransform.anchorMax = new(0.5f, 0.5f);

                if (_backgroundBar.objectReferenceValue && _frontBar.objectReferenceValue)
                {
                    Image backgroundBar = _backgroundBar.objectReferenceValue as Image;
                    Image frontBar = _frontBar.objectReferenceValue as Image;

                    backgroundBar.rectTransform.anchorMin = new(0.5f, 0.5f);
                    backgroundBar.rectTransform.anchorMax = new(0.5f, 0.5f);

                    frontBar.rectTransform.anchorMin = new(0.5f, 0.5f);
                    frontBar.rectTransform.anchorMax = new(0.5f, 0.5f);

                    _rangeAdjustmentSlider.Init();

                    if (_divideOption.IsDivide.boolValue)
                    {
                        _leftIntValue.intValue  = Mathf.Clamp(
                            EditorGUILayout.IntSlider("Left Value", _leftIntValue.intValue, 0, _rightIntValue.intValue - 1),
                            0, _rightIntValue.intValue - 1);

                        _rightIntValue.intValue = Mathf.Clamp(
                            EditorGUILayout.IntSlider("Right Value", _rightIntValue.intValue, _leftIntValue.intValue + 1, _divideOption.DivideValue.intValue),
                            _leftIntValue.intValue + 1, _divideOption.DivideValue.intValue);

                        leftHandle.rectTransform.localPosition  = _rangeAdjustmentSlider.GetIntValueToLocalPosition(_leftIntValue.intValue);
                        rightHandle.rectTransform.localPosition = _rangeAdjustmentSlider.GetIntValueToLocalPosition(_rightIntValue.intValue);
                    }
                    else
                    {
                        _leftValue.floatValue  = EditorGUILayout.Slider("Left Value", _leftValue.floatValue, 0.0f, _rightValue.floatValue);
                        _rightValue.floatValue = EditorGUILayout.Slider("Right Value", _rightValue.floatValue, _leftValue.floatValue, 1.0f);

                        leftHandle.rectTransform.localPosition = _rangeAdjustmentSlider.GetValueToLocalPosition(_leftValue.floatValue);
                        rightHandle.rectTransform.localPosition = _rangeAdjustmentSlider.GetValueToLocalPosition(_rightValue.floatValue);
                    }
                }
                else
                {
                    ActionEditorVertical(() => EditorGUILayout.LabelField("Slider 이미지가 존재하지 않습니다. 이미지를 필드로 넣어주세요."), GUI.skin.box);
                }
            }
            else
            {
                ActionEditorVertical(() => EditorGUILayout.LabelField("Handle이 존재하지 않습니다. Handle을 필드로 넣어주세요."), GUI.skin.box);
            }

        }, GUI.skin.box);

        if (!_divideOption.IsDivide.boolValue) 
        {
            EditorGUILayout.PropertyField(_onChangedLeftValue);
            EditorGUILayout.PropertyField(_onChangedRightValue);
        }
        else
        {
            EditorGUILayout.PropertyField(_onChangedLeftIntValue);
            EditorGUILayout.PropertyField(_onChangedRightIntValue);
        }

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
