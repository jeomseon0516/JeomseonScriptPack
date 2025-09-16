#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Jeomseon.UI.Components;
using static Jeomseon.Editor.EditorGUIHelper;
using static Jeomseon.Editor.EditorReflectionHelper;

[CustomEditor(typeof(HorizontalSelector))]
internal sealed class HorizontalSelectorEditor : Editor
{
    private HorizontalSelector _horizontalSelector = null;

    private SerializedProperty _leftButton;
    private SerializedProperty _rightButton;

    private SerializedProperty _viewport;
    private SerializedProperty _content;

    private SerializedProperty _onChangedValue;
    private SerializedProperty _selectedIndex;

    [MenuItem("GameObject/Create/UI/Horizontal Selector")]
    public static void CreateRangeAdjustMent(MenuCommand menuCommand)
    {
        GameObject horizontalSelctorPrefab = Resources.Load<GameObject>("HorizontalSelector");
        GameObject newUIElement = Instantiate(horizontalSelctorPrefab, (menuCommand.context as GameObject).transform);
        newUIElement.name = "HorizontalSelector";

        Undo.RegisterCreatedObjectUndo(newUIElement, "Create Horizontal Selector");
        Selection.activeObject = newUIElement;
    }

    private void OnEnable()
    {
        _horizontalSelector = target as HorizontalSelector;
        _leftButton = serializedObject.FindProperty(GetBackingFieldName("LeftButton"));
        _rightButton = serializedObject.FindProperty(GetBackingFieldName("RightButton"));
        _viewport = serializedObject.FindProperty(GetBackingFieldName("Viewport"));
        _content = serializedObject.FindProperty(GetBackingFieldName("Content"));
        _onChangedValue = serializedObject.FindProperty(GetBackingFieldName("OnChangedValue"));
        _selectedIndex = serializedObject.FindProperty("_selectedIndex");
    }

    public override void OnInspectorGUI()
    {

        EditorGUILayout.LabelField("Viewport Option");
        ActionEditorVertical(() =>
        {
            using EditorGUI.ChangeCheckScope scope = new();
            EditorGUILayout.PropertyField(_viewport, new("Viewport"), true);
            EditorGUILayout.PropertyField(_content, new("Content"), true);
            
            if (scope.changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }, GUI.skin.box);


        serializedObject.Update();

        if (_viewport.objectReferenceValue && _content.objectReferenceValue)
        {
            using EditorGUI.ChangeCheckScope scope = new();
            EditorGUILayout.LabelField("Value");
            ActionEditorVertical(() =>
            {
                RectTransform rectTransform = _horizontalSelector.transform as RectTransform;
                RectTransform viewport = _viewport.objectReferenceValue as RectTransform;
                RectTransform content = _content.objectReferenceValue as RectTransform;

                _horizontalSelector.InitRectTransforms();

                EditorGUILayout.IntSlider(_selectedIndex, 0, content.childCount - 1);

                content.localPosition = _horizontalSelector.GetTargetPosition(
                    _selectedIndex.intValue,
                    content.childCount,
                    content.sizeDelta.x,
                    viewport.sizeDelta.x);

            }, GUI.skin.box);

            EditorGUILayout.LabelField("Buttons");
            ActionEditorVertical(() =>
            {
                EditorGUILayout.PropertyField(_leftButton, new GUIContent("Left Button"), true);
                EditorGUILayout.PropertyField(_rightButton, new GUIContent("Right Button"), true);
            }, GUI.skin.box);

            EditorGUILayout.PropertyField(_onChangedValue);

            if (scope.changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif