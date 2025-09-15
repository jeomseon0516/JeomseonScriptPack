#if UNITY_EDITOR
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
        static InspectorWindowModifier()
        {
            initialize();
        }

        private static readonly Dictionary<string, List<IObjectEditorAttributeDrawer>> _attributeDrawers = new();
        private static bool _isDelay = false;

        private static void initialize() 
        {
            Debug.Log("Modify Inspector!");
            
            EditorCoroutineUtility.StartCoroutineOwnerless(runModifier());

            static IEnumerator runModifier()
            {
                yield return null;
                modifyInspector();
            }
        }

        private static void modifyInspector()
        {
            // InspectorWindow 타입을 얻어옴
            Type inspectorWindowType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            if (inspectorWindowType == null) return;

            // 모든 InspectorWindow 인스턴스를 얻어옴
            IEnumerable<EditorWindow> inspectors = Resources
                .FindObjectsOfTypeAll(inspectorWindowType)
                .OfType<EditorWindow>();

            foreach (EditorWindow inspector in inspectors)
            {
                // rootVisualElement를 리플렉션으로 가져옴 (2022.3.29f1) 기준 인스펙터 에디터 VisualElement는 editorsElement 프로퍼티
                PropertyInfo rootVisualElementProperty = inspectorWindowType.GetProperty("editorsElement", BindingFlags.NonPublic | BindingFlags.Instance);

                if (rootVisualElementProperty?.GetValue(inspector) is VisualElement rootVisualElement)
                {
                    rootVisualElement.RegisterCallback<GeometryChangedEvent>(_ =>
                       callCustomDrawerMethods(rootVisualElement));

                    callCustomDrawerMethods(rootVisualElement);
                }
            }

            static IEnumerator iETrackSelectionChanged(VisualElement visualElement)
            {
                Object selectedObject = null;
                Type scriptableObjectType = typeof(ScriptableObject);

                while (true)
                {
                    Object o = selectedObject;
                    yield return new WaitUntil(() => o != Selection.activeObject &&
                                                     Selection.activeObject != null &&
                                                     Selection.activeObject.GetType().IsSubclassOf(scriptableObjectType));

                    selectedObject = Selection.activeObject;
                    float originalWidth = visualElement.resolvedStyle.width;
                    setVisualElementWidth(visualElement, originalWidth + 1);
                    EditorCoroutineUtility.StartCoroutineOwnerless(setOriginalWidth(visualElement, originalWidth));
                }

                static IEnumerator setOriginalWidth(VisualElement visualElement, float originalWidth)
                {
                    yield return null;
                    setVisualElementWidth(visualElement, originalWidth);
                }

                static void setVisualElementWidth(VisualElement visualElement, float width)
                {
                    visualElement.style.width = width;
                    Debug.Log(visualElement.style.alignSelf = Align.Auto);
                }
            }
        }

        private static void callCustomDrawerMethods(VisualElement root)
        {
            if (_isDelay) return;

            _isDelay = true;
            EditorCoroutineUtility.StartCoroutineOwnerless(iEWaitSetIsDelayToFalse());
            List<VisualElement> editorElements = root.Query<VisualElement>(null, "unity-inspector-element").ToList(); // .. 인스펙터 엘리먼트 쿼리로 검색

            foreach (VisualElement editorElement in editorElements)
            {
                if (editorElement.Q<IMGUIContainer>("onw-custom-attribute-drawer") != null) continue; // .. 중첩 방지 안해두면 2번이상호출된후 무한 호출반복 

                // .. (2022.3.29f1) 기준 editor 프로퍼티
                PropertyInfo editorProperty = editorElement
                    .GetType()
                    .GetProperty("editor", BindingFlags.NonPublic | BindingFlags.Instance);

                if (editorProperty?.GetValue(editorElement) is Editor editor && // .. 에디터 찾아오기
                    editor.target is MonoBehaviour or ScriptableObject)        // .. 타겟이 모노비하이비어거나 스크립터블 오브젝트 일 경우
                {
                    IMGUIContainer iMGUIContainer = editorElement.Q<IMGUIContainer>("onw-custom-attribute-drawer");

                    // .. Editor마다 드로어 인스턴스 생성 적용되는 오브젝트마다 처리되는 데이터의 양이 다를 수 있으므로
                    if (!_attributeDrawers.TryGetValue(editor.target.GetInstanceID().ToString(), out List<IObjectEditorAttributeDrawer> drawers))
                    {
                        drawers = new(ReflectionHelper.CreateChildClassesFromType<IObjectEditorAttributeDrawer>()); // .. 드로어를 상속받는 클래스들의 인스턴스 생성 후 반환
                        _attributeDrawers.Add(editor.target.GetInstanceID().ToString(), drawers); // .. 추가
                        drawers.ForEach(drawer => drawer.OnEnable(editor)); // .. Enable 호출 사실상 Awake와 같다
                    }

                    if (iMGUIContainer is null)
                    {
                        iMGUIContainer = new(() => drawers.ForEach(drawer => drawer.OnInspectorGUI(editor)))
                        {
                            name = "onw-custom-attribute-drawer" // .. 컨테이너에 중첩방지용 이름 부여
                        };
                        editorElement.Add(iMGUIContainer);
                    }
                }
            }

            static IEnumerator iEWaitSetIsDelayToFalse()
            {
                yield return null;
                _isDelay = false;
            }
        }
    }
}
#endif