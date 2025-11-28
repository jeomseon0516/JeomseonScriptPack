#if UNITY_EDITOR && !UNITY_6000_0_OR_NEWER
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using Jeomseon.Helper;

namespace Jeomseon.Attribute.Editor
{
    using Object = UnityEngine.Object;
    using Editor = UnityEditor.Editor;

    /// <summary>
    /// .. 리플렉션을 통해 정상적으로는 접근할 수 없는 InspectorWindow에 접근합니다
    /// 타사 라이브러리와의 충돌을 고려하여 InspectorWindow 인스펙터에 추가 기능을 담당하는 VisualElement를 Add합니다
    /// CustomEditor(typeof(MonoBehaviour)), CustomEditor(typeof(ScriptableObject)) 와 같은 경우들은 MonoBehaviour, ScriptableObject를 상속받는 더 구체적인 클래스의 커스텀 에디터가
    /// 구현되어있으면 target으로  MonoBehaviour, ScriptableObject를 불러오지 못하는 경우가 발생합니다
    /// 커스텀 모노비하이비어, 커스텀 스크립터블 오브젝트를 구현해서 해당 클래스를 상속시키고 CustomEditor의 타겟으로 삼으면 충돌문제가 발생하지 않지만
    /// 추가 기능을 사용하려면 커스텀 클래스들을 상속받아야 한다는 약속되지 않은 규칙이 생기므로 해당 스크립트를 통해 기능들을 구현합니다
    /// 리플렉션을 통해 정상적으로는 접근할 수 없는 클래스에 접근하기 때문에 버전에 따라 동작하지 않는 경우가 발생할 수 있습니다
    /// 다른 에디터 버전에서 사용한다면 버전별 업데이트가 필요할 수 있습니다
    /// </summary>
    [InitializeOnLoad]
    internal static class InspectorWindowModifier
    {
        // target InstanceID -> drawer list
        private static readonly Dictionary<string, List<IObjectEditorAttributeDrawer>> _attributeDrawers = new();
        private static bool _isDelay = false;

        static InspectorWindowModifier()
        {
            Initialize();
        }

        private static void Initialize()
        {
            Debug.Log("Modify Inspector!");

            EditorCoroutineUtility.StartCoroutineOwnerless(RunModifier());

            static IEnumerator RunModifier()
            {
                // 한 프레임 늦게 실행해서 Inspector가 완전히 만들어진 뒤에 후킹
                yield return null;
                ModifyInspector();
            }
        }

        private static void ModifyInspector()
        {
            // InspectorWindow 타입을 얻어옴
            Type inspectorWindowType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            if (inspectorWindowType == null)
                return;

            // 모든 InspectorWindow 인스턴스를 얻어옴
            IEnumerable<EditorWindow> inspectors = Resources
                .FindObjectsOfTypeAll(inspectorWindowType)
                .OfType<EditorWindow>();

            foreach (EditorWindow inspector in inspectors)
            {
                // 내부 비공개 editorsElement 대신, 공식 rootVisualElement 사용
                VisualElement rootVisualElement = inspector.rootVisualElement;
                if (rootVisualElement == null)
                    continue;

                rootVisualElement.RegisterCallback<GeometryChangedEvent>(_ =>
                    CallCustomDrawerMethods(rootVisualElement));

                // 최초 1회 즉시 호출
                CallCustomDrawerMethods(rootVisualElement);
            }

            // 선택 변경 시 강제로 리빌드하는 용도로 쓰던 것으로 보이는 코드 (현재는 사용 안 함)
            static IEnumerator IETrackSelectionChanged(VisualElement visualElement)
            {
                Object selectedObject = null;
                Type scriptableObjectType = typeof(ScriptableObject);

                while (true)
                {
                    Object o = selectedObject;

                    yield return new WaitUntil(() =>
                        o != Selection.activeObject &&
                        Selection.activeObject != null &&
                        Selection.activeObject.GetType().IsSubclassOf(scriptableObjectType));

                    selectedObject = Selection.activeObject;
                    float originalWidth = visualElement.resolvedStyle.width;
                    SetVisualElementWidth(visualElement, originalWidth + 1);
                    EditorCoroutineUtility.StartCoroutineOwnerless(SetOriginalWidth(visualElement, originalWidth));
                }

                static IEnumerator SetOriginalWidth(VisualElement visualElement, float originalWidth)
                {
                    yield return null;
                    SetVisualElementWidth(visualElement, originalWidth);
                }

                static void SetVisualElementWidth(VisualElement visualElement, float width)
                {
                    visualElement.style.width = width;
                    visualElement.style.alignSelf = Align.Auto;

                    // alignSelf에 값을 대입하면서 로그 찍던, 수상한 부분 수정
                    Debug.Log($"Inspector root width adjusted to {width}.");
                }
            }
        }

        private static void CallCustomDrawerMethods(VisualElement root)
        {
            // 한 프레임에 너무 자주 호출되는 것 방지
            if (_isDelay)
                return;

            _isDelay = true;
            EditorCoroutineUtility.StartCoroutineOwnerless(IEWaitSetIsDelayToFalse());

            // .. 인스펙터 엘리먼트 쿼리로 검색
            List<VisualElement> editorElements =
                root.Query<VisualElement>(null, "unity-inspector-element").ToList();

            foreach (VisualElement editorElement in editorElements)
            {
                // 이미 우리 컨테이너가 붙어 있으면 중복 추가 방지
                IMGUIContainer existingContainer =
                    editorElement.Q<IMGUIContainer>("onw-custom-attribute-drawer");

                if (existingContainer != null)
                    continue;

                Editor editor = null;

                // Unity 버전에 따라 내부 구현이 달라질 수 있으므로 방어적 리플렉션
                try
                {
                    PropertyInfo editorProperty = editorElement
                        .GetType()
                        .GetProperty("editor", BindingFlags.NonPublic | BindingFlags.Instance);

                    if (editorProperty != null)
                        editor = editorProperty.GetValue(editorElement) as Editor;
                }
                catch (Exception ex)
                {
                    // 전체 에디터를 죽이지 않도록 한 번만 로그 찍는 식으로 관리해도 좋음
                    Debug.LogException(ex);
                }

                if (editor == null)
                    continue;

                // .. 타겟이 모노비하이비어거나 스크립터블 오브젝트 일 경우만 처리
                if (editor.target is not (MonoBehaviour or ScriptableObject))
                    continue;

                string key = editor.target.GetInstanceID().ToString();

                // Editor마다 드로어 인스턴스 생성
                if (!_attributeDrawers.TryGetValue(key, out List<IObjectEditorAttributeDrawer> drawers))
                {
                    drawers = new List<IObjectEditorAttributeDrawer>(
                        ReflectionHelper.CreateChildClassesFromType<IObjectEditorAttributeDrawer>());

                    _attributeDrawers.Add(key, drawers);

                    // 사실상 Awake 개념
                    drawers.ForEach(drawer => drawer.OnEnable(editor));
                }

                // 실제로 IMGUI 컨테이너를 추가
                IMGUIContainer drawerContainer = new IMGUIContainer(
                    () => drawers.ForEach(drawer => drawer.OnInspectorGUI(editor)))
                {
                    // 중복 방지를 위한 이름
                    name = "onw-custom-attribute-drawer"
                };

                editorElement.Add(drawerContainer);
            }

            static IEnumerator IEWaitSetIsDelayToFalse()
            {
                yield return null;
                _isDelay = false;
            }
        }
    }
}
#endif
