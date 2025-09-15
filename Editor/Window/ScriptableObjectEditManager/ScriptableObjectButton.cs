#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jeomseon.Editor.Window
{
    internal sealed class ScriptableObjectButton : Button
    {
        public ScriptableObject ScriptableObject { get; private set; } = null;

        public ScriptableObjectButton(ScriptableObject scriptableObject, Action clickedEvent) : base(clickedEvent) 
        {
            ScriptableObject = scriptableObject;
        }

        public ScriptableObjectButton(ScriptableObject scriptableObject) : base() 
        {
            ScriptableObject = scriptableObject;
        }
    }
}
#endif
