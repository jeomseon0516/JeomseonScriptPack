#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using Jeomseon.Extensions;

namespace Jeomseon.Editor.Extensions
{
    public static class ButtonExtensionsForVisualElement
    {

        /// <summary>
        /// .. backgroundColor변경시 마우스 이벤트로 인한 버튼 View 상호작용이 일어나지 않는 현상을 해결합니다
        /// </summary>
        /// <param name="button"></param>
        public static void SetChangedColorButtonEvent(this Button button)
        {
            Color defaultColor = button.style.backgroundColor.value;
            const float HOVER_OFFSET = 0.1f;
            const float CLICK_OFFSET = -0.05f;

            button.UnregisterCallback<MouseEnterEvent>(onEnterEvent);
            button.UnregisterCallback<MouseLeaveEvent>(onLeaveEvent);
            button.UnregisterCallback<MouseDownEvent>(onDownEvent, TrickleDown.TrickleDown);
            button.UnregisterCallback<MouseUpEvent>(onUpEvent, TrickleDown.TrickleDown);

            button.RegisterCallback<MouseEnterEvent>(onEnterEvent);
            button.RegisterCallback<MouseLeaveEvent>(onLeaveEvent);
            button.RegisterCallback<MouseDownEvent>(onDownEvent, TrickleDown.TrickleDown);
            button.RegisterCallback<MouseUpEvent>(onUpEvent, TrickleDown.TrickleDown);

            void onEnterEvent(MouseEnterEvent evt)
            {
                button.style.backgroundColor = defaultColor.AdjustValue(HOVER_OFFSET);
            }

            void onLeaveEvent(MouseLeaveEvent evt)
            {
                button.style.backgroundColor = defaultColor;
            }

            void onDownEvent(MouseDownEvent evt)
            {
                button.style.backgroundColor = defaultColor.AdjustValue(CLICK_OFFSET);
            }
                
            void onUpEvent(MouseUpEvent evt)
            {
                button.style.backgroundColor = defaultColor;
            }
        }
    }
}
#endif