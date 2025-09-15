#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Jeomseon.Editor.Window
{
    public class UIAnchorSetter : EditorWindow
    {
        [MenuItem("Jeomseon/Tool/UI Anchor Setter Tool")]
        private static void showWindow()
        {
            GetWindow<UIAnchorSetter>(nameof(UIAnchorSetter));
        }

        private RectTransform _selectedUI = null;

        private void OnGUI()
        {
            GUILayout.Label("Anchor Setter Tool", EditorStyles.boldLabel);

            _selectedUI = EditorGUILayout.ObjectField("UI Object", _selectedUI, typeof(RectTransform), true) as RectTransform;

            if (_selectedUI && GUILayout.Button("Set Anchors for Child"))
            {
                foreach (RectTransform rectTransform in _selectedUI.GetComponentsInChildren<RectTransform>(false))
                {
                    if (rectTransform == _selectedUI) continue;
                    
                    RectTransform parent = (rectTransform.parent as RectTransform)!;
                    if (!parent) return;

                    Vector2 newAnchorMin = new(
                        rectTransform.anchorMin.x + rectTransform.offsetMin.x / parent.rect.width,
                        rectTransform.anchorMin.y + rectTransform.offsetMin.y / parent.rect.height);

                    Vector2 newAnchorMax = new(
                        rectTransform.anchorMax.x + rectTransform.offsetMax.x / parent.rect.width,
                        rectTransform.anchorMax.y + rectTransform.offsetMax.y / parent.rect.height);

                    rectTransform.anchorMin = newAnchorMin;
                    rectTransform.anchorMax = newAnchorMax;
                    rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;
                }
                
                EditorUtility.SetDirty(_selectedUI);
            }
        }
    }
}
#endif