#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Jeomseon.UI.Components;
using static Jeomseon.Editor.EditorGUIHelper;
using static Jeomseon.Editor.EditorReflectionHelper;

[CustomEditor(typeof(EnumeratedElements))]
internal sealed class EnumeratedElementsEditor : Editor
{
    private ContentSizeFitter _contentSizeFitter;
    private GridLayoutGroup _gridLayoutGroup;
    private RectTransform _content;
    private EnumeratedElements _enumeratedElements;

    private Vector2 _paddingScrollPosition = Vector2.zero;
    private Vector2 _spacingScrollPosition = Vector2.zero;

    private SerializedProperty _widthToHeightRatio;
    private SerializedProperty _elementSizeRatio;
    private SerializedProperty _elementSizeToSpacingXRatio;
    private SerializedProperty _elementSizeToSpacingYRatio;

    private SerializedProperty _paddingLeftRatio;
    private SerializedProperty _paddingRightRatio;
    private SerializedProperty _paddingTopRatio;
    private SerializedProperty _paddingBottomRatio;

    private void OnEnable()
    {
        _enumeratedElements = (target as EnumeratedElements)!;
        _contentSizeFitter = _enumeratedElements.GetComponent<ContentSizeFitter>();
        _gridLayoutGroup = _enumeratedElements.GetComponent<GridLayoutGroup>();
        _content = _enumeratedElements.GetComponent<RectTransform>();

        EnumeratedElements.InitGridLayoutGroup(_gridLayoutGroup);
        _contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _widthToHeightRatio = serializedObject.FindProperty(GetBackingFieldName("WidthToHeightRatio"));
        _elementSizeRatio = serializedObject.FindProperty(GetBackingFieldName("ElementSizeRatio"));
        _elementSizeToSpacingXRatio = serializedObject.FindProperty(GetBackingFieldName("ElementSizeToSpacingXRatio"));
        _elementSizeToSpacingYRatio = serializedObject.FindProperty(GetBackingFieldName("ElementSizeToSpacingYRatio"));
        _paddingLeftRatio = serializedObject.FindProperty(GetBackingFieldName("PaddingLeftRatio"));
        _paddingRightRatio = serializedObject.FindProperty(GetBackingFieldName("PaddingRightRatio"));
        _paddingTopRatio = serializedObject.FindProperty(GetBackingFieldName("PaddingTopRatio"));
        _paddingBottomRatio = serializedObject.FindProperty(GetBackingFieldName("PaddingBottomRatio"));
    }

    public override void OnInspectorGUI()
    {
        using EditorGUI.ChangeCheckScope scope = new();
        serializedObject.Update();

        EditorGUILayout.LabelField("Constraint Count");
        _gridLayoutGroup.constraintCount = EditorGUILayout.IntField(_gridLayoutGroup.constraintCount);

        EditorGUILayout.LabelField("Width To Height Ratio");
        _widthToHeightRatio.floatValue = EditorGUILayout.Slider(_widthToHeightRatio.floatValue, 0.1f, 2.0f); ;

        EditorGUILayout.LabelField("Element Size Ratio");
        _elementSizeRatio.floatValue = EditorGUILayout.Slider(_elementSizeRatio.floatValue, 0.75f, 1.25f);

        EditorGUILayout.LabelField("Element Size To Spacing Ratio");
        ActionEditorVerticalBox(GUI.skin.box, ref _spacingScrollPosition, () =>
        {
            _elementSizeToSpacingXRatio.floatValue = EditorGUILayout.Slider("X", _elementSizeToSpacingXRatio.floatValue, 0.0f, 0.25f);
            _elementSizeToSpacingYRatio.floatValue = EditorGUILayout.Slider("Y", _elementSizeToSpacingYRatio.floatValue, 0.0f, 0.25f);
        });

        EditorGUILayout.LabelField("Padding Ratio");
        ActionEditorVerticalBox(GUI.skin.box, ref _paddingScrollPosition, () =>
        {
            _paddingLeftRatio.floatValue = EditorGUILayout.Slider("Left", _paddingLeftRatio.floatValue, 0f, 0.1f);
            _paddingRightRatio.floatValue = EditorGUILayout.Slider("Right", _paddingRightRatio.floatValue, 0f, 0.1f);
            _paddingTopRatio.floatValue = EditorGUILayout.Slider("Top", _paddingTopRatio.floatValue, 0f, 0.1f);
            _paddingBottomRatio.floatValue = EditorGUILayout.Slider("Bottom", _paddingBottomRatio.floatValue, 0f, 0.1f);
        });

        _gridLayoutGroup.padding = EnumeratedElements.GetPadding(
            _content.rect.width,
            _paddingLeftRatio.floatValue,
            _paddingRightRatio.floatValue,
            _paddingTopRatio.floatValue,
            _paddingBottomRatio.floatValue);

        _gridLayoutGroup.cellSize = EnumeratedElements.GetCellSize(
            _content.rect.width,
            _elementSizeRatio.floatValue,
            _widthToHeightRatio.floatValue,
            _gridLayoutGroup.constraintCount);

        _gridLayoutGroup.spacing = EnumeratedElements.GetSpacing(
            _gridLayoutGroup.cellSize,
            _elementSizeToSpacingXRatio.floatValue,
            _elementSizeToSpacingYRatio.floatValue);

        if (scope.changed)
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif