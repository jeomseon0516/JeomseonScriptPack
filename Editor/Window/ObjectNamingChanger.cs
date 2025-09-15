#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Jeomseon.Extensions;

namespace Jeomseon.Editor.Window
{
    public sealed class ObjectNamingChanger : EditorWindow
    {
        private enum NameChangeState : byte
        {
            ONLY_CURRENT,
            CHILD_ALL
        }

        private enum ChangeMode : byte
        {
            REPLACE,
            ADD
        }

        private enum AffixState : byte
        {
            PREFIX,
            SUFFIX
        }

        private GameObject _targetObject = null;
        private ChangeMode _changeMode = ChangeMode.REPLACE;
        private NameChangeState _nameChangeState = NameChangeState.ONLY_CURRENT;
        private AffixState _affixState = AffixState.PREFIX;
        private string _replaceTargetText = string.Empty;
        private string _replaceChangedTargetText = string.Empty;
        private string _affixText = string.Empty;

        [MenuItem("Jeomseon/Tool/Object Naming Changer")]
        private static void init()
        {
            ObjectNamingChanger objectNamingChanger = CreateWindow<ObjectNamingChanger>();
            objectNamingChanger.Show();
        }
        
        private void OnGUI()
        {
            _targetObject = EditorGUILayout.ObjectField(
                new GUIContent("Target Object"),
                _targetObject,
                typeof(GameObject),
                true) as GameObject;

            if (_targetObject)
            {
                using (EditorGUILayout.HorizontalScope _ = new())
                {
                    _changeMode = (ChangeMode)EditorGUILayout.EnumPopup("Change Mode", _changeMode);
                    _nameChangeState = (NameChangeState)EditorGUILayout.EnumPopup("Name Change State", _nameChangeState);
                }

                EditorGUILayout.Space(10);
                
                switch (_changeMode)
                {
                    case ChangeMode.REPLACE:
                        onGUIByReplaceMode();
                        break;
                    case ChangeMode.ADD:
                        onGUIByAddMode();
                        break;
                }
            }
        }
        
        private void onGUIByReplaceMode()
        {
            _replaceTargetText = EditorGUILayout.TextField("Target Text", _replaceTargetText);
            _replaceChangedTargetText = EditorGUILayout.TextField("Changed Text", _replaceChangedTargetText);

            if (GUILayout.Button("Replace") && !string.IsNullOrEmpty(_replaceTargetText))
            {
                getGameObjectByNameChangeState(go => go.name = go.name.Replace(_replaceTargetText, _replaceChangedTargetText));
            }
        }

        private void onGUIByAddMode()
        {
            _affixState = (AffixState)EditorGUILayout.EnumPopup(_affixState);
            
            _affixText = EditorGUILayout.TextField("Affix Text", _affixText);

            if (GUILayout.Button("Affix") && !string.IsNullOrEmpty(_affixText))
            {
                switch (_affixState)
                {
                    case AffixState.PREFIX:
                        getGameObjectByNameChangeState(go => go.name = $"{_affixText}_{go.name}");
                        break;
                    case AffixState.SUFFIX:
                        getGameObjectByNameChangeState(go => go.name += $"_{_affixText}");
                        break;
                }
            }
        }

        private void getGameObjectByNameChangeState(Action<GameObject> goAction)
        {
            switch (_nameChangeState)
            {
                case NameChangeState.ONLY_CURRENT:
                    goAction?.Invoke(_targetObject);
                    break;
                case NameChangeState.CHILD_ALL:
                    if (goAction is null) return;
                    _targetObject
                        .GetComponentsInChildren<Transform>()
                        .Select(transform => transform.gameObject)
                        .ForEach(goAction);
                    break;
            }
        }
    }
}
#endif