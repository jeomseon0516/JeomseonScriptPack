#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Editor.GUI
{
    using GUI = UnityEngine.GUI;

    public sealed class ToggleEnumerator<T>
    {
        public T ChooseTarget { get; private set; } = default;
        public bool IsInitializeGUIStyle => _selectedGUIStyle is not null && _defaultGUIStyle is not null;
        public bool IsInitializeOnGetDataList => _onGetDataList is not null;

        private Func<IEnumerable<T>> _onGetDataList = null;
        private GUIStyle _selectedGUIStyle = null;
        private GUIStyle _defaultGUIStyle = null;

        public void InitializeGUIStyle(GUIStyle selectedGUIStyle, GUIStyle defaultGUIStyle)
        {
            _selectedGUIStyle = selectedGUIStyle;
            _defaultGUIStyle = defaultGUIStyle;
        }

        public void InitializeGUIStyle(Color32 selectedStyleColor, Color32 defaultStyleColor)
        {
            _selectedGUIStyle = new(GUI.skin.box);
            _defaultGUIStyle = new(GUI.skin.box);

            _selectedGUIStyle.normal.background = EditorGUIHelper.GetTexture2D(selectedStyleColor, _selectedGUIStyle);
            _defaultGUIStyle.normal.background = EditorGUIHelper.GetTexture2D(defaultStyleColor, _defaultGUIStyle);
        }

        public T SelectEnumeratedToggles(Func<T, string> onSelectedText, params GUILayoutOption[] options)
        {
            T selectingTarget = ChooseTarget;

            foreach (T target in _onGetDataList?.Invoke())
            {
                bool isTargeting = ChooseTarget?.Equals(target) ?? false;
                bool isSelected = GUILayout.Toggle(
                    isTargeting,
                    onSelectedText.Invoke(target),
                    isTargeting ? _selectedGUIStyle : _defaultGUIStyle,
                    options);

                if (isSelected && !isTargeting)
                {
                    selectingTarget = target;
                }
                if (!isSelected && isTargeting)
                {
                    selectingTarget = default;
                }
            }

            ChooseTarget = selectingTarget;
            return ChooseTarget;
        }

        public void SetOnGetDataList(Func<IEnumerable<T>> onGetDataList)
        {
            _onGetDataList ??= onGetDataList;
        }
    }
}
#endif