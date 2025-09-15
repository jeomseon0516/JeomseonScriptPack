using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jeomseon.Extensions
{
    public static class VisualElementExtensions
    {
        public static VisualElement GetRootVisualElement(this VisualElement visualElement)
        {
            VisualElement current = visualElement;

            while (current?.parent is not null)
            {
                current = current.parent;
            }

            return current;
        }
    }
}
