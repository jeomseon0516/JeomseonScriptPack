#if UNITY_EDITOR
using UnityEngine;

namespace Jeomseon.Editor.Window
{
    using Editor = UnityEditor.Editor;

    internal class CustomInspectorEditorWindow
    {
        private Editor _editor = null;
        private Object _currentTarget = null;

        public void OnInspectorGUI(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (_editor == null || _currentTarget != target)
            {
                createEditor(target);
            }

            if (_editor != null)
            {
                _editor.OnInspectorGUI();
            }
        }

        private void createEditor(Object target)
        {
            destroyEditor();

            _currentTarget = target;
            _editor = Editor.CreateEditor(target);
        }

        public void OnDisable()
        {
            destroyEditor();
        }

        private void destroyEditor()
        {
            if (_editor != null)
            {
                Object.DestroyImmediate(_editor);
                _editor = null;
                _currentTarget = null;
            }
        }
    }
}
#endif