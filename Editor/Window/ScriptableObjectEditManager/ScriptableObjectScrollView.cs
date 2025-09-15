#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Jeomseon.Extensions;

namespace Jeomseon.Editor.Window
{
    internal class ScriptableObjectScrollView : VisualElement
    {
        public event Action<ScriptableObjectButton> OnSelectObject;

        public ScrollView View { get; } = new()
        {
            style =
            {
                flexDirection = FlexDirection.Column,
                flexGrow = 1
            },
            mode = ScrollViewMode.Vertical
        };

        public IReadOnlyList<ScriptableObjectButton> ScriptableObjects => _scriptableObjects;
        protected readonly List<ScriptableObjectButton> _scriptableObjects = new();

        protected ScriptableObjectButton _selectedObject = null;

        internal virtual void OnEnable()
        {
            if (_selectedObject is not null)
            {
                _selectedObject.style.backgroundColor = ColorUtility.TryParseHtmlString("#3E3E3E", out Color color) ? color : Color.gray;
            }

            _selectedObject = null;
        }

        internal virtual void OnDisable()
        {
        }

        protected virtual void OnInitialize()
        {
            
        }
        
        internal void Initialize()
        {
            OnSelectObject += selectedObject =>
            {
                if (_selectedObject is not null)
                {
                    _selectedObject.style.backgroundColor = ColorUtility.TryParseHtmlString("#3E3E3E", out Color color) ? color : Color.gray;
                }

                _selectedObject = selectedObject;
                _selectedObject.style.backgroundColor = _selectedObject.selection.selectionColor;
            };

            VisualElement header = CreateHeader();
            if (header is not null)
            {
                Add(header);
            }
            ToolbarSearchField searchField = new()
            {
                style =
                {
                    alignSelf = Align.Center,
                    width = new(Length.Percent(95))
                }
            };
            searchField.RegisterValueChangedCallback(BuildBySearchString);
            Add(searchField);
            Add(View);

            OnInitialize();
        }

        internal void ClearList()
        {
            View.Clear();
            _scriptableObjects.Clear();
        }

        public void AddRange(IEnumerable<ScriptableObject> objects)
        {
            ScriptableObjectButton[] soArray = objects
                .Select(CreateButton)
                .ToArray();

            soArray.ForEach(View.Add);
            _scriptableObjects.AddRange(soArray);
            
            sortElements();
        }

        public void AddSo(ScriptableObject obj)
        {
            ScriptableObjectButton button = CreateButton(obj);
            View.Add(button);
            _scriptableObjects.Add(button);
            
            sortElements();
        }

        public void RemoveSo(ScriptableObject obj)
        {
            int index = _scriptableObjects.FindIndex(button => button.ScriptableObject == obj);

            if (index > -1)
            {
                ScriptableObjectButton soButton = _scriptableObjects[index];
                _scriptableObjects.RemoveAt(index);
                soButton.RemoveFromHierarchy();
            }
        }

        protected virtual void BuildBySearchString(ChangeEvent<string> evt)
        {
            string searchString = evt?.newValue ?? string.Empty;
            if (!string.IsNullOrEmpty(searchString) && !string.IsNullOrWhiteSpace(searchString))
            {
                foreach (IGrouping<bool, ScriptableObjectButton> group in _scriptableObjects
                    .GroupBy(so => so.name.Contains(searchString)))
                {
                    foreach (ScriptableObjectButton button in group)
                    {
                        if (group.Key)
                        {
                            if (!View.Contains(button))
                            {
                                View.Add(button);
                            }
                        }
                        else
                        {
                            button.RemoveFromHierarchy();
                        }
                    }
                }
            }
            else
            {
                _scriptableObjects
                    .Where(button => !View.Contains(button))
                    .ForEach(View.Add);
            }
            
            sortElements();
        }

        private void sortElements()
        {
            View.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        protected virtual ScriptableObjectButton CreateButton(ScriptableObject so)
        {
            ScriptableObjectButton button = new(so)
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    flexGrow = 1,
                    height = 40,
                    borderBottomWidth = 0,
                    borderTopWidth = 0,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    marginBottom = 0,
                    marginTop = 0,
                    marginLeft = 0,
                    marginRight = 0,
                    paddingBottom = 0,
                    paddingTop = 0,
                    paddingLeft = 0,
                    paddingRight = 0,
                    borderBottomLeftRadius = 0,
                    borderTopLeftRadius = 0,
                    borderBottomRightRadius = 0,
                    borderTopRightRadius = 0,
                    whiteSpace = WhiteSpace.Normal,
                    backgroundColor = ColorUtility.TryParseHtmlString("#3E3E3E", out Color color) ? color : Color.gray
                },
                text = so.name,
            };
            button.clicked += () => OnSelectObject?.Invoke(button);

            return button;
        }

        protected virtual VisualElement CreateHeader()
        {
            return null;
        }

        public ScriptableObjectScrollView()
        {
            style.borderBottomColor = Color.black;
            style.borderTopColor = Color.black;
            style.borderLeftColor = Color.black;
            style.borderRightColor = Color.black;
            style.borderBottomWidth = 1;
            style.borderTopWidth = 1;
            style.borderLeftWidth = 1;
            style.borderRightWidth = 1;
            style.width = new(Length.Percent(25));
            style.minWidth = 150f;
            style.maxWidth = 350f;
        }
    }
}
#endif