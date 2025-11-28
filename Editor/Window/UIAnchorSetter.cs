#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Linq;

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
                var rects = _selectedUI
                    .GetComponentsInChildren<RectTransform>(true)
                    .Where(rt =>
                        !rt.GetComponent<Canvas>() &&
                        (!rt.parent || !rt.parent.GetComponent<LayoutGroup>())
                    )
                    .ToArray();

                // 🔥 Undo 시작
                Undo.RecordObjects(rects, "Set UI Anchors");

                foreach (RectTransform rectTransform in rects)
                {
                    RectTransform parent = rectTransform.parent as RectTransform;
                    if (!parent) continue;

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

                // 변경 사항 저장
                EditorUtility.SetDirty(_selectedUI);
            }
        }
    }
}
#endif
