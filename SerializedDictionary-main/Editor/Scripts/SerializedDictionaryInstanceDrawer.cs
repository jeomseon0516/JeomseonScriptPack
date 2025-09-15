using AYellowpaper.SerializedCollections.Editor.Data;
using AYellowpaper.SerializedCollections.Editor.States;
using AYellowpaper.SerializedCollections.KeysGenerators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AYellowpaper.SerializedCollections.Editor
{
    public class SerializedDictionaryInstanceDrawer
    {
        private readonly FieldInfo _fieldInfo;
        private readonly ReorderableList _unexpandedList;
        private readonly SingleEditingData _singleEditingData;
        private readonly FieldInfo _keyFieldInfo;
        private GUIContent _label;
        private Rect _totalRect;
        private readonly GUIStyle _keyValueStyle;
        private readonly PropertyData _propertyData;
        private bool _propertyListSettingsInitialized = false;
        private readonly List<int> _pagedIndices;
        private readonly PagingElement _pagingElement;
        private int _lastListSize = -1;
        private readonly IReadOnlyList<KeyListGeneratorData> _keyGeneratorsWithoutWindow;
        private readonly IReadOnlyList<KeyListGeneratorData> _keyGeneratorsWithWindow;
        private readonly SearchField _searchField;
        private GUIContent _shortDetailsContent;
        private GUIContent _detailsContent;
        private bool _showSearchBar = false;
        private readonly bool _isReadOnlyKey = false;
        private readonly bool _isReadOnlyValue = false;
        private readonly bool _isLocked = false;
        private ListState _activeState;

        internal SerializedProperty ListProperty { get; private set; }
        internal ReorderableList ReorderableList { get; private set; }
        internal string SearchText { get; private set; } = string.Empty;
        internal SearchListState SearchState { get; private set; }
        internal DefaultListState DefaultState { get; private set; }

        private class SingleEditingData
        {
            public bool IsValid => LookupTable != null;
            public IKeyable LookupTable;

            public void Invalidate()
            {
                LookupTable = null;
            }
        }

        public SerializedDictionaryInstanceDrawer(SerializedProperty property, FieldInfo fieldInfo)
        {
            _fieldInfo = fieldInfo;
            ListProperty = property.FindPropertyRelative(SerializedDictionaryDrawer.SerializedListName);

            _keyValueStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                padding = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.MiddleCenter
            };

            DefaultState = new DefaultListState(this);
            SearchState = new SearchListState(this);
            _activeState = DefaultState;

            SerializedDictionaryAttribute dictionaryAttribute = _fieldInfo.GetCustomAttribute<SerializedDictionaryAttribute>();

            _isReadOnlyKey = dictionaryAttribute?.IsReadOnlyKey ?? false;
            _isReadOnlyValue = dictionaryAttribute?.IsReadOnlyValue ?? false;
            _isLocked = dictionaryAttribute?.IsLocked ?? false;

            _propertyData = SCEditorUtility.GetPropertyData(ListProperty);
            _propertyData.GetElementData(SCEditorUtility.KeyFlag).Settings.DisplayName = dictionaryAttribute?.KeyName ?? "Key";
            _propertyData.GetElementData(SCEditorUtility.ValueFlag).Settings.DisplayName = dictionaryAttribute?.ValueName ?? "Value";

            SavePropertyData();

            _pagingElement = new PagingElement();
            _pagedIndices = new List<int>();
            updatePaging();

            ReorderableList = makeList();
            _unexpandedList = makeUnexpandedList();
            _searchField = new SearchField();

            FieldInfo listField = _fieldInfo.FieldType.GetField(SerializedDictionaryDrawer.SerializedListName, BindingFlags.Instance | BindingFlags.NonPublic)!;
            Type entryType = listField.FieldType.GetGenericArguments()[0];
            _keyFieldInfo = entryType.GetField(SerializedDictionaryDrawer.KeyName);

            _singleEditingData = new SingleEditingData();

            IReadOnlyList<KeyListGeneratorData> keyGenerators = KeyListGeneratorCache.GetPopulatorsForType(_keyFieldInfo.FieldType);
            _keyGeneratorsWithWindow = keyGenerators.Where(x => x.NeedsWindow).ToList();
            _keyGeneratorsWithoutWindow = keyGenerators.Where(x => !x.NeedsWindow).ToList();

            UpdateAfterInput();
        }

        public void OnGUI(Rect position, GUIContent label)
        {
            _totalRect = position;
            _label = new GUIContent(label);

            EditorGUI.BeginChangeCheck();
            DoList(position);
            if (EditorGUI.EndChangeCheck())
            {
                ListProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        public float GetPropertyHeight(GUIContent label)
        {
            return !ListProperty.isExpanded ?
                SerializedDictionaryDrawer.TopHeaderClipHeight :
                ReorderableList.GetHeight();
        }

        private void DoList(Rect position)
        {
            if (ListProperty.isExpanded)
                ReorderableList.DoList(position);
            else
            {
                using (new GUI.ClipScope(new Rect(0, position.y, position.width + position.x, SerializedDictionaryDrawer.TopHeaderClipHeight)))
                {
                    _unexpandedList.DoList(position.WithY(0));
                }
            }
        }

        private void ProcessState()
        {
            ListState newState = _activeState.OnUpdate();
            if (newState != null && newState != _activeState)
            {
                _activeState.OnExit();
                _activeState = newState;
                newState.OnEnter();
            }
        }

        private SerializedProperty GetElementProperty(SerializedProperty property, bool fieldFlag)
        {
            return property.FindPropertyRelative(fieldFlag == SerializedDictionaryDrawer.KeyFlag ? SerializedDictionaryDrawer.KeyName : SerializedDictionaryDrawer.ValueName);
        }

        internal static float CalculateHeightOfElement(SerializedProperty property, bool drawKeyAsList, bool drawValueAsList)
        {
            SerializedProperty keyProperty = property.FindPropertyRelative(SerializedDictionaryDrawer.KeyName);
            SerializedProperty valueProperty = property.FindPropertyRelative(SerializedDictionaryDrawer.ValueName);
            return Mathf.Max(SCEditorUtility.CalculateHeight(keyProperty, drawKeyAsList), SCEditorUtility.CalculateHeight(valueProperty, drawValueAsList));
        }

        private void UpdateAfterInput()
        {
            InitializeSettingsIfNeeded();
            ProcessState();
            CheckPaging();
            int elementsPerPage = EditorUserSettings.Get().ElementsPerPage;
            int pageCount = Mathf.Max(1, Mathf.CeilToInt((float)DefaultState.ListSize / elementsPerPage));
            toggleSearchBar(_propertyData.AlwaysShowSearch || SCEditorUtility.ShouldShowSearch(pageCount));
        }

        private void InitializeSettingsIfNeeded()
        {
            void InitializeSettings(bool fieldFlag)
            {
                Type[] genericArgs = _fieldInfo.FieldType.GetGenericArguments();
                SerializedProperty firstProperty = ListProperty.GetArrayElementAtIndex(0);
                (DisplayType displayType, bool canToggleListDrawer) = createDisplaySettings(GetElementProperty(firstProperty, fieldFlag), genericArgs[fieldFlag == SCEditorUtility.KeyFlag ? 0 : 1]);
                ElementSettings settings = _propertyData.GetElementData(fieldFlag).Settings;
                settings.DisplayType = displayType;
                settings.HasListDrawerToggle = canToggleListDrawer;
            }

            if (!_propertyListSettingsInitialized && ListProperty.minArraySize > 0)
            {
                _propertyListSettingsInitialized = true;
                InitializeSettings(SCEditorUtility.KeyFlag);
                InitializeSettings(SCEditorUtility.ValueFlag);
                SavePropertyData();
            }
        }

        private void CheckPaging()
        {
            // TODO: Is there a better solution to check for Revert/delete/add?
            if (_lastListSize != _activeState.ListSize)
            {
                _lastListSize = _activeState.ListSize;
                UpdateSingleEditing();
                updatePaging();
            }
        }

        private void SavePropertyData()
        {
            SCEditorUtility.SavePropertyData(ListProperty, _propertyData);
        }

        private void UpdateSingleEditing()
        {
            switch (ListProperty.serializedObject.isEditingMultipleObjects)
            {
                case true when _singleEditingData.IsValid:
                    _singleEditingData.Invalidate();
                    break;
                case false when !_singleEditingData.IsValid:
                    {
                        object dictionary = SCEditorUtility.GetPropertyValue(ListProperty, ListProperty.serializedObject.targetObject);
                        _singleEditingData.LookupTable = getLookupTable(dictionary);
                        break;
                    }
            }
        }

        private static IKeyable getLookupTable(object dictionary)
        {
            PropertyInfo propInfo = dictionary.GetType().GetProperty(SerializedDictionaryDrawer.LookupTableName, BindingFlags.Instance | BindingFlags.NonPublic);
            return (IKeyable)propInfo!.GetValue(dictionary);
        }

        private void updatePaging()
        {
            int elementsPerPage = EditorUserSettings.Get().ElementsPerPage;
            _pagingElement.PageCount = Mathf.Max(1, Mathf.CeilToInt((float)_activeState.ListSize / elementsPerPage));

            _pagedIndices.Clear();
            _pagedIndices.Capacity = Mathf.Max(elementsPerPage, _pagedIndices.Capacity);

            int startIndex = (_pagingElement.Page - 1) * elementsPerPage;
            int endIndex = Mathf.Min(startIndex + elementsPerPage, _activeState.ListSize);
            for (int i = startIndex; i < endIndex; i++)
                _pagedIndices.Add(i);

            string shortDetailsString = _activeState.ListSize + " " + (_pagedIndices.Count == 1 ? "Element" : "Elements");
            string detailsString = _pagingElement.PageCount > 1
                ? $"{_pagedIndices[0] + 1}..{_pagedIndices.Last() + 1} / {_activeState.ListSize} Elements"
                : shortDetailsString;
            _detailsContent = new GUIContent(detailsString);
            _shortDetailsContent = new GUIContent(shortDetailsString);
        }

        private ReorderableList makeList()
        {
            ReorderableList list = new ReorderableList(_pagedIndices, typeof(int), true, true, !_isLocked, !_isLocked);
            list.onAddCallback += OnAdd;
            list.onRemoveCallback += OnRemove;
            list.onReorderCallbackWithDetails += OnReorder;
            list.drawElementCallback += OnDrawElement;
            list.elementHeightCallback += OnGetElementHeight;
            list.drawHeaderCallback += OnDrawHeader;
            list.drawNoneElementCallback += OnDrawNoneElement;
            return list;
        }

        private ReorderableList makeUnexpandedList()
        {
            ReorderableList list = new(SerializedDictionaryDrawer.NoEntriesList, typeof(int))
            {
                drawHeaderCallback = drawUnexpandedHeader
            };
            return list;
        }

        private void toggleSearchBar(bool flag)
        {
            _showSearchBar = flag;
            ReorderableList.headerHeight = SerializedDictionaryDrawer.TopHeaderClipHeight + SerializedDictionaryDrawer.KeyValueHeaderHeight + (_showSearchBar ? SerializedDictionaryDrawer.SearchHeaderHeight : 0);
            if (!_showSearchBar)
            {
                if (_searchField.HasFocus())
                    GUI.FocusControl(null);
                SearchText = string.Empty;
            }
        }

        private void OnDrawNoneElement(Rect rect)
        {
            EditorGUI.LabelField(rect, EditorGUIUtility.TrTextContent(_activeState.NoElementsText));
        }

        private (DisplayType displayType, bool canToggleListDrawer) createDisplaySettings(SerializedProperty property, Type type)
        {
            bool hasCustomEditor = SCEditorUtility.HasDrawerForType(type);

            bool isGenericWithChildren = property.propertyType == SerializedPropertyType.Generic && property.hasVisibleChildren;
            bool isArray = property.isArray && property.propertyType != SerializedPropertyType.String;
            bool canToggleListDrawer = isArray || (isGenericWithChildren && hasCustomEditor);
            DisplayType displayType = DisplayType.PropertyNoLabel;
            if (canToggleListDrawer)
                displayType = DisplayType.Property;
            else if (isGenericWithChildren)
                displayType = DisplayType.List;
            return (displayType, canToggleListDrawer);
        }

        private void drawUnexpandedHeader(Rect rect)
        {
            EditorGUI.BeginProperty(rect, _label, ListProperty);
            ListProperty.isExpanded = EditorGUI.Foldout(rect.WithX(rect.x - 5), ListProperty.isExpanded, _label, true);

            GUIStyle detailsStyle = EditorStyles.miniLabel;
            Rect detailsRect = rect.AppendRight(0).AppendLeft(detailsStyle.CalcSize(_shortDetailsContent).x);
            GUI.Label(detailsRect, _shortDetailsContent, detailsStyle);

            EditorGUI.EndProperty();
        }

        private void doPaging(Rect rect)
        {
            EditorGUI.BeginChangeCheck();
            _pagingElement.OnGUI(rect);
            if (EditorGUI.EndChangeCheck())
            {
                ReorderableList.ClearSelection();
                updatePaging();
            }
        }

        private void OnDrawHeader(Rect rect)
        {
            Rect topRect = rect.WithHeight(SerializedDictionaryDrawer.TopHeaderHeight);
            Rect adjustedTopRect = topRect.WithXAndWidth(_totalRect.x + 1, _totalRect.width - 1);

            doMainHeader(adjustedTopRect.CutLeft(topRect.x - adjustedTopRect.x));
            if (_showSearchBar)
            {
                adjustedTopRect = adjustedTopRect.AppendDown(SerializedDictionaryDrawer.SearchHeaderHeight);
                DoSearch(adjustedTopRect);
            }
            DoKeyValueRect(adjustedTopRect.AppendDown(SerializedDictionaryDrawer.KeyValueHeaderHeight));

            UpdateAfterInput();
        }

        private void doMainHeader(Rect rect)
        {
            Rect lastTopRect = rect.AppendRight(0).WithHeight(EditorGUIUtility.singleLineHeight);

            lastTopRect = lastTopRect.AppendLeft(20);
            doOptionsButton(lastTopRect);
            lastTopRect = lastTopRect.AppendLeft(5);

            if (_pagingElement.PageCount > 1)
            {
                lastTopRect = lastTopRect.AppendLeft(_pagingElement.GetDesiredWidth());
                doPaging(lastTopRect);
            }

            GUIStyle detailsStyle = EditorStyles.miniLabel;
            lastTopRect = lastTopRect.AppendLeft(detailsStyle.CalcSize(_detailsContent).x, 5);
            GUI.Label(lastTopRect, _detailsContent, detailsStyle);

            if (!_singleEditingData.IsValid)
            {
                lastTopRect = lastTopRect.AppendLeft(lastTopRect.height + 5);
                GUIContent guicontent = EditorGUIUtility.TrIconContent(EditorGUIUtility.Load("d_console.infoicon") as Texture, "Conflict checking, duplicate key removal and populators not supported in multi object editing mode.");
                GUI.Label(lastTopRect, guicontent);
            }

            EditorGUI.BeginProperty(rect, _label, ListProperty);
            ListProperty.isExpanded = EditorGUI.Foldout(rect.WithXAndWidth(rect.x - 5, lastTopRect.x - rect.x), ListProperty.isExpanded, _label, true);
            EditorGUI.EndProperty();
        }

        private void doOptionsButton(Rect rect)
        {
            Rect screenRect = GUIUtility.GUIToScreenRect(rect);
            if (GUI.Button(rect, EditorGUIUtility.IconContent("pane options@2x"), EditorStyles.iconButton))
            {
                GenericMenu gm = new GenericMenu();
                SCEditorUtility.AddGenericMenuItem(gm, false, !_isLocked && ListProperty.minArraySize > 0, new GUIContent("Clear"), () => QueueAction(ClearList));
                SCEditorUtility.AddGenericMenuItem(gm, false, true, new GUIContent("Remove Conflicts"), () => QueueAction(RemoveConflicts));
                SCEditorUtility.AddGenericMenuItem(gm, false, _keyGeneratorsWithWindow.Count > 0, new GUIContent("Bulk Edit..."), () => OpenKeysGeneratorSelectorWindow(screenRect));
                if (_keyGeneratorsWithoutWindow.Count > 0)
                {
                    gm.AddSeparator(string.Empty);
                    foreach (KeyListGeneratorData generatorData in _keyGeneratorsWithoutWindow)
                    {
                        SCEditorUtility.AddGenericMenuItem(gm, false, true, new GUIContent(generatorData.Name), OnPopulatorDataSelected, generatorData);
                    }
                }
                gm.AddSeparator(string.Empty);
                SCEditorUtility.AddGenericMenuItem(gm, _propertyData.AlwaysShowSearch, true, new GUIContent("Always Show Search"), ToggleAlwaysShowSearchPropertyData);
                gm.AddItem(new GUIContent("Preferences..."), false, () => SettingsService.OpenUserPreferences(EditorUserSettingsProvider.PreferencesPath));
                gm.DropDown(rect);
            }
        }

        private void OnPopulatorDataSelected(object userData)
        {
            KeyListGeneratorData data = (KeyListGeneratorData)userData;
            KeyListGenerator so = (KeyListGenerator)ScriptableObject.CreateInstance(data.GeneratorType);
            so.hideFlags = HideFlags.DontSave;
            ApplyPopulatorQueued(so, ModificationType.Add);
        }

        private void OpenKeysGeneratorSelectorWindow(Rect rect)
        {
            KeyListGeneratorSelectorWindow window = ScriptableObject.CreateInstance<KeyListGeneratorSelectorWindow>();
            window.Initialize(_keyGeneratorsWithWindow, _keyFieldInfo.FieldType);
            window.ShowAsDropDown(rect, new Vector2(400, 200));
            window.OnApply += ApplyPopulatorQueued;
        }

        private void ToggleAlwaysShowSearchPropertyData()
        {
            _propertyData.AlwaysShowSearch = !_propertyData.AlwaysShowSearch;
            SavePropertyData();
        }

        private void DoKeyValueRect(Rect rect)
        {
            float width = EditorGUIUtility.labelWidth + 22;
            Rect leftRect = rect.WithWidth(width);
            Rect rightRect = leftRect.AppendRight(rect.width - width);

            if (Event.current.type == EventType.Repaint && _propertyData != null)
            {
                _keyValueStyle.Draw(leftRect, EditorGUIUtility.TrTextContent(_propertyData.GetElementData(SerializedDictionaryDrawer.KeyFlag).Settings.DisplayName), false, false, false, false);
                _keyValueStyle.Draw(rightRect, EditorGUIUtility.TrTextContent(_propertyData.GetElementData(SerializedDictionaryDrawer.ValueFlag).Settings.DisplayName), false, false, false, false);
            }

            if (ListProperty.minArraySize > 0)
            {
                DoDisplayTypeToggle(leftRect, SerializedDictionaryDrawer.KeyFlag);
                DoDisplayTypeToggle(rightRect, SerializedDictionaryDrawer.ValueFlag);
            }

            EditorGUI.DrawRect(rect.AppendDown(1, -1), SerializedDictionaryDrawer.BorderColor);
        }

        private void DoSearch(Rect rect)
        {
            EditorGUI.DrawRect(rect.AppendLeft(1), SerializedDictionaryDrawer.BorderColor);
            EditorGUI.DrawRect(rect.AppendRight(1, -1), SerializedDictionaryDrawer.BorderColor);
            EditorGUI.DrawRect(rect.AppendDown(1, -1), SerializedDictionaryDrawer.BorderColor);

            SearchText = _searchField.OnToolbarGUI(rect.CutTop(2).CutHorizontal(6), SearchText);
        }

        private void ApplyPopulatorQueued(KeyListGenerator populator, ModificationType modificationType)
        {
            object[] array = populator.GetKeys(_keyFieldInfo.FieldType).OfType<object>().ToArray();
            QueueAction(() => ApplyPopulator(array, modificationType));
        }

        private void QueueAction(EditorApplication.CallbackFunction action)
        {
            EditorApplication.delayCall += action;
        }

        private void ApplyPopulator(IEnumerable<object> elements, ModificationType modificationType)
        {
            foreach (Object targetObject in ListProperty.serializedObject.targetObjects)
            {
                Undo.RecordObject(targetObject, "Populate");
                object dictionary = SCEditorUtility.GetPropertyValue(ListProperty, targetObject);
                IKeyable lookupTable = getLookupTable(dictionary);

                if (modificationType == ModificationType.Add)
                    AddElements(lookupTable, elements);
                else if (modificationType == ModificationType.Remove)
                    RemoveElements(lookupTable, elements);
                else if (modificationType == ModificationType.Confine)
                    ConfineElements(lookupTable, elements);

                lookupTable.RecalculateOccurences();
                PrefabUtility.RecordPrefabInstancePropertyModifications(targetObject);
            }

            ListProperty.serializedObject.Update();
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        private static void AddElements(IKeyable lookupTable, IEnumerable<object> elements)
        {
            foreach (object key in elements)
            {
                IReadOnlyList<int> occurences = lookupTable.GetOccurences(key);
                if (occurences.Count > 0)
                    continue;
                lookupTable.AddKey(key);
            }
        }

        private static void ConfineElements(IKeyable lookupTable, IEnumerable<object> elements)
        {
            HashSet<object> keysToRemove = lookupTable.Keys.OfType<object>().ToHashSet();
            foreach (object key in elements)
                keysToRemove.Remove(key);

            RemoveElements(lookupTable, keysToRemove);
        }

        private static void RemoveElements(IKeyable lookupTable, IEnumerable<object> elements)
        {
            IOrderedEnumerable<int> indicesToRemove = elements.SelectMany(x => lookupTable.GetOccurences(x)).OrderByDescending(index => index);
            foreach (int index in indicesToRemove)
            {
                lookupTable.RemoveAt(index);
            }
        }

        private void ClearList()
        {
            ListProperty.ClearArray();
            ListProperty.serializedObject.ApplyModifiedProperties();
        }

        private void RemoveConflicts()
        {
            foreach (Object targetObject in ListProperty.serializedObject.targetObjects)
            {
                Undo.RecordObject(targetObject, "Remove Conflicts");
                object dictionary = SCEditorUtility.GetPropertyValue(ListProperty, targetObject);
                IKeyable lookupTable = getLookupTable(dictionary);

                List<int> duplicateIndices = new();

                foreach (object key in lookupTable.Keys)
                {
                    IReadOnlyList<int> occurences = lookupTable.GetOccurences(key);
                    for (int i = 1; i < occurences.Count; i++)
                        duplicateIndices.Add(occurences[i]);
                }

                foreach (int indexToRemove in duplicateIndices.OrderByDescending(x => x))
                {
                    lookupTable.RemoveAt(indexToRemove);
                }

                lookupTable.RecalculateOccurences();
                PrefabUtility.RecordPrefabInstancePropertyModifications(targetObject);
            }

            ListProperty.serializedObject.Update();
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        private void DoDisplayTypeToggle(Rect contentRect, bool fieldFlag)
        {
            ElementData displayData = _propertyData.GetElementData(fieldFlag);

            if (displayData.Settings.HasListDrawerToggle)
            {
                Rect rightRectToggle = new Rect(contentRect);
                rightRectToggle.x += rightRectToggle.width - 18;
                rightRectToggle.width = 18;
                EditorGUI.BeginChangeCheck();
                bool newValue = GUI.Toggle(rightRectToggle, displayData.IsListToggleActive, SerializedDictionaryDrawer.DisplayTypeToggleContent, EditorStyles.toolbarButton);
                if (EditorGUI.EndChangeCheck())
                {
                    displayData.IsListToggleActive = newValue;
                    SavePropertyData();
                }
            }
        }

        private float OnGetElementHeight(int index)
        {
            int actualIndex = _pagedIndices[index];
            SerializedProperty element = _activeState.GetPropertyAtIndex(actualIndex);
            return CalculateHeightOfElement(element, _propertyData.GetElementData(SerializedDictionaryDrawer.KeyFlag).EffectiveDisplayType == DisplayType.List, _propertyData.GetElementData(SerializedDictionaryDrawer.ValueFlag).EffectiveDisplayType == DisplayType.List ? true : false);
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            const int lineLeftSpace = 2;
            const int lineWidth = 1;
            const int lineRightSpace = 12;
            const int totalSpace = lineLeftSpace + lineWidth + lineRightSpace;

            int actualIndex = _pagedIndices[index];

            SerializedProperty kvp = _activeState.GetPropertyAtIndex(actualIndex);
            Rect keyRect = rect.WithSize(EditorGUIUtility.labelWidth - lineLeftSpace, EditorGUIUtility.singleLineHeight);
            Rect lineRect = keyRect.WithXAndWidth(keyRect.x + keyRect.width + lineLeftSpace, lineWidth).WithHeight(rect.height);
            Rect valueRect = keyRect.AppendRight(rect.width - keyRect.width - totalSpace, totalSpace);

            SerializedProperty keyProperty = kvp.FindPropertyRelative(SerializedDictionaryDrawer.KeyName);
            SerializedProperty valueProperty = kvp.FindPropertyRelative(SerializedDictionaryDrawer.ValueName);

            Color prevColor = GUI.color;
            if (_singleEditingData.IsValid)
            {
                object keyObject = _keyFieldInfo.GetValue(_singleEditingData.LookupTable.GetKeyAt(actualIndex));
                IReadOnlyList<int> occurences = _singleEditingData.LookupTable.GetOccurences(keyObject);

                if (occurences.Count > 1)
                {
                    GUI.color = occurences[0] == actualIndex &&
                        SerializedCollectionsUtility.IsValidKey(keyObject) ?
                            Color.yellow :
                            Color.red;
                }
            }

            ElementData keyDisplayData = _propertyData.GetElementData(SerializedDictionaryDrawer.KeyFlag);
            if (_isReadOnlyKey)
            {
                GUI.enabled = false;
            }
            DrawGroupedElement(keyRect, 20, keyProperty, keyDisplayData.EffectiveDisplayType);
            GUI.enabled = true;

            EditorGUI.DrawRect(lineRect, new Color(36 / 255f, 36 / 255f, 36 / 255f));
            GUI.color = prevColor;

            ElementData valueDisplayData = _propertyData.GetElementData(SerializedDictionaryDrawer.ValueFlag);
            if (_isReadOnlyValue)
            {
                GUI.enabled = false;
            }
            DrawGroupedElement(valueRect, lineRightSpace, valueProperty, valueDisplayData.EffectiveDisplayType);
            GUI.enabled = true;
        }

        private void DrawGroupedElement(Rect rect, int spaceForProperty, SerializedProperty property, DisplayType displayType)
        {
            using (new LabelWidth(rect.width * 0.4f))
            {
                float height = SCEditorUtility.CalculateHeight(property.Copy(), displayType);
                Rect groupRect = rect.CutLeft(-spaceForProperty).WithHeight(height);
                GUI.BeginGroup(groupRect);

                Rect elementRect = new(spaceForProperty, 0, rect.width, height);
                _activeState.DrawElement(elementRect, property, displayType);

                DrawInvisibleProperty(rect.WithWidth(spaceForProperty), property);

                GUI.EndGroup();
            }
        }

        internal static void DrawInvisibleProperty(Rect rect, SerializedProperty property)
        {
            const int propertyOffset = 5;

            GUI.BeginClip(rect.CutLeft(-propertyOffset));
            EditorGUI.BeginProperty(rect, GUIContent.none, property);
            EditorGUI.EndProperty();
            GUI.EndClip();
        }

        internal static void DrawElement(Rect rect, SerializedProperty property, DisplayType displayType, Action<SerializedProperty> BeforeDrawingCallback = null, Action<SerializedProperty> AfterDrawingCallback = null)
        {
            switch (displayType)
            {
                case DisplayType.Property:
                    BeforeDrawingCallback?.Invoke(property);
                    EditorGUI.PropertyField(rect, property, true);
                    AfterDrawingCallback?.Invoke(property);
                    break;
                case DisplayType.PropertyNoLabel:
                    BeforeDrawingCallback?.Invoke(property);
                    EditorGUI.PropertyField(rect, property, GUIContent.none, true);
                    AfterDrawingCallback?.Invoke(property);
                    break;
                case DisplayType.List:
                    Rect childRect = rect.WithHeight(0);
                    foreach (SerializedProperty prop in SCEditorUtility.GetChildren(property.Copy()))
                    {
                        childRect = childRect.AppendDown(EditorGUI.GetPropertyHeight(prop, true));
                        BeforeDrawingCallback?.Invoke(prop);
                        EditorGUI.PropertyField(childRect, prop, true);
                        AfterDrawingCallback?.Invoke(prop);
                    }
                    break;
                default:
                    break;
            }
        }

        private void OnAdd(ReorderableList list)
        {
            int targetIndex = list.selectedIndices.Count > 0 && list.selectedIndices[0] >= 0 ? list.selectedIndices[0] : 0;
            int actualTargetIndex = targetIndex < _pagedIndices.Count ? _pagedIndices[targetIndex] : 0;
            _activeState.InserElementAt(actualTargetIndex);
        }

        private void OnReorder(ReorderableList list, int oldIndex, int newIndex)
        {
            updatePaging();
            ListProperty.MoveArrayElement(_pagedIndices[oldIndex], _pagedIndices[newIndex]);
        }

        private void OnRemove(ReorderableList list)
        {
            _activeState.RemoveElementAt(_pagedIndices[list.index]);
            updatePaging();
        }
    }
}