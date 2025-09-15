#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Jeomseon.Editor.GUI
{
    public sealed class EditorDropdownController<T>
    {
        public IReadOnlyDictionary<string, T> Options => _options;
        public KeyValuePair<string, T> SelectedItem { get; private set; } = default;

        private readonly string _label = string.Empty;
        private readonly T _default = default;
        private readonly Dictionary<string, T> _options;

        public void Dropdown(Action<T> onChangeValue)
        {
            EditorGUIHelper.ActionEditorHorizontal(() =>
            {
                EditorGUILayout.LabelField(_label, GUILayout.ExpandWidth(true));
                if (EditorGUILayout.DropdownButton(
                    new(string.IsNullOrEmpty(SelectedItem.Key) ? _label : SelectedItem.Key),
                    FocusType.Keyboard))
                {
                    GenericMenu menu = new();

                    foreach (KeyValuePair<string, T> optionKvp in _options)
                    {
                        menu.AddItem(
                            new GUIContent(optionKvp.Key),
                            SelectedItem.Key == optionKvp.Key,
                            () =>
                            {
                                SelectedItem = optionKvp;
                                onChangeValue.Invoke(optionKvp.Value);
                            });
                    }

                    if (_default is not null && !_default.Equals(default))
                    {
                        menu.AddItem(
                            new GUIContent("NONE"),
                            false,
                            () =>
                            {
                                SelectedItem = default;
                                onChangeValue.Invoke(_default);
                            });
                    }

                    menu.ShowAsContext();
                }
            });
        }

        public void SetSelectedItem(T value)
        {
            SelectedItem = Options.FirstOrDefault(kvp => kvp.Value.Equals(value));
        }

        private EditorDropdownController() { }
        public EditorDropdownController(string label, Dictionary<string, T> options, T defaultValue = default)
        {
            _label = label;
            _default = defaultValue;
            _options = options;
            SelectedItem = _options.FirstOrDefault();
        }
    }
}
#endif