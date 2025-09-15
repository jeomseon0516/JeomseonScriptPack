#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Jeomseon.Editor.GUI
{
    public sealed class EditorScrollController
    {
        private Vector2 _scrollPosition = Vector2.zero;

        public void ActionScrollSpace(Action action)
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            action.Invoke();
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
