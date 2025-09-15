#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Jeomseon.Editor
{
    public static class EditorGUIHelper
    {
        public static void ActionHorizontal(Action action, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            action.Invoke();
            GUILayout.EndHorizontal();
        }

        public static void ActionHorizontal(Action action, GUIStyle guiStyle, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(guiStyle, options);
            action.Invoke();
            GUILayout.EndHorizontal();
        }

        public static void ActionEditorHorizontal(Action action, GUIStyle guiStyle, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal(guiStyle, options);
            action.Invoke();
            EditorGUILayout.EndHorizontal();
        }

        public static void ActionEditorHorizontal(Action action, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal(options);
            action.Invoke();
            EditorGUILayout.EndHorizontal();
        }

        public static void ActionEditorVertical(Action action, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(options);
            action.Invoke();
            EditorGUILayout.EndVertical();
        }

        public static void ActionEditorVertical(Action action, GUIStyle guiStyle, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(guiStyle, options);
            action.Invoke();
            EditorGUILayout.EndVertical();
        }

        public static void ActionVertical(Action action, params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical(options);
            action.Invoke();
            GUILayout.EndVertical();
        }

        public static void ActionVertical(Action action, GUIStyle guiStyle, params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical(guiStyle, options);
            action.Invoke();
            GUILayout.EndVertical();
        }

        public static void ActionEditorVerticalBox(GUIStyle guiStyle, ref Vector2 scrollPosition, Action action, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(guiStyle, options);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            action.Invoke();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        public static void ActionVerticalBox(GUIStyle guiStyle, ref Vector2 scrollPosition, Action action, params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical(guiStyle, options);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            action.Invoke();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// .. 어떤 데이터 리스트가 있을 때 해당 데이터들의 요소를 토글 기능을 가진 스크롤뷰에 수직으로 열거시키는 기능 입니다 열거된 데이터에서 토글 선택된 데이터 요소를 반환합니다.
        /// </summary>
        /// <typeparam name="T"> .. 데이터 요소의 타입 </typeparam>
        /// <param name="targets"> .. 열거시킬 데이터 리스트</param>
        /// <param name="text">.. 요소를 가져와서 해당 요소로 text를 쓸 수 있게하는 콜백 함수 입니다 </param>
        /// <param name="chooseTarget"> .. 열거된 데이터리스트 중에서 선택되어 있는 변수를 넣어주어야 합니다 </param>
        /// <param name="selectGUIStyle"> .. 어떤 요소가 선택됐을때 해당 요소를 GUI에 어떻게 표현할 것인지를 결정하는 변수 </param>
        /// <param name="options"> .. GUILayout메서드들에 인자값으로 들어가는 options와 같습니다. </param>
        /// <returns> .. 열거된 데이터 리스트 중에서 토글 선택된 요소 </returns>
        public static T SelectEnumeratedToggles<T>(IEnumerable<T> targets, Func<T, string> text, T chooseTarget, GUIStyle selectGUIStyle, GUIStyle defaultGUIStyle, params GUILayoutOption[] options)
        {
            T selectingTarget = chooseTarget;

            foreach (T target in targets)
            {
                bool isTargeting = chooseTarget?.Equals(target) ?? false;
                bool isSelected = GUILayout.Toggle(isTargeting, text.Invoke(target), isTargeting ? selectGUIStyle : defaultGUIStyle, options);

                if (isSelected && !isTargeting)
                {
                    selectingTarget = target;
                }
                if (!isSelected && isTargeting)
                {
                    selectingTarget = default;
                }
            }

            return selectingTarget;
        }

        /// <summary>
        /// .. guiStyle의 텍스쳐를 커스텀하여 사용하고 싶을때 사용할 수 있는 함수 입니다. 커스텀할 guiStyle을 인자값으로 넣어주어야 합니다.
        /// </summary>
        /// <param name="color"> .. 변경할 컬러 </param>
        /// <param name="guiStyle"> .. 커스텀 할 GUIStyle </param>
        /// <returns></returns>
        public static Texture2D GetTexture2D(Color32 color, GUIStyle guiStyle)
        {
            Color32[] pix = new Color32[guiStyle.border.horizontal * guiStyle.border.vertical];

            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = color;
            }

            Texture2D texture = new(guiStyle.border.horizontal, guiStyle.border.vertical);
            texture.SetPixels32(pix);
            texture.Apply();

            return texture;
        }

        public static void DisableEditorWindow<T>(ref T editorWindow) where T : EditorWindow
        {
            if (editorWindow)
            {
                editorWindow.Close();
            }
            editorWindow = null;
        }

        public static void OnErrorMessage(ref string errorMessage, GUIStyle labelStyle)
        {
            if (errorMessage is null) return;

            GUILayout.Label(errorMessage, labelStyle);
            Debug.LogWarning(errorMessage);

            if (GUILayout.Button("OK", GUILayout.Width(30)))
            {
                errorMessage = null;
            }
        }
    }
}
#endif