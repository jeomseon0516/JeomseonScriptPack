#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using Jeomseon.Scope;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace Jeomseon.Attribute.Editor
{
    internal sealed class ComponentDropdown : TreeView
    {
        private sealed class ComponentDropdownPopupContent : PopupWindowContent
        {
            private readonly ComponentDropdown _dropdown;
            private readonly SearchField _searchField;
            private readonly GUIStyle _labelStyle;

            public ComponentDropdownPopupContent(ComponentDropdown dropdown)
            {
                _dropdown = dropdown;
                _labelStyle = new(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter
                };
                _searchField = new();
            }

            public override Vector2 GetWindowSize()
            {
                return new(300f, Mathf.Clamp(_dropdown.ItemCount * _dropdown.rowHeight, 200, 800));
            }

            public override void OnGUI(Rect rect)
            {
                float voidWidth = rect.width * 0.1f;
                rect.y += 3.0f;
                Rect searchRect = new(rect.x + voidWidth * 0.5f, rect.y, rect.width - voidWidth, EditorGUIUtility.singleLineHeight);

                _dropdown.searchString = _searchField.OnGUI(searchRect, _dropdown.searchString);

                EditorGUI.LabelField(
                    new(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 1.35f, rect.width, EditorGUIUtility.singleLineHeight),
                    "Nested Object",
                    _labelStyle);

                Rect treeViewRect = new(
                    rect.x,
                    rect.y + EditorGUIUtility.singleLineHeight * 2.5f,
                    rect.width,
                    rect.height - EditorGUIUtility.singleLineHeight * 2.5f);

                _dropdown.OnGUI(treeViewRect);
            }
        }

        public int ItemCount
        {
            get
            {
                return countItems(rootItem);

                static int countItems(TreeViewItem item)
                {
                    return item.children?.Aggregate(1, (count, child) => count + countItems(child)) ?? 1;
                }
            }
        }

        private readonly Action<GameObject> _onSelected;
        private readonly GameObject _rootObject;
        private readonly Type _defaultType;
        private readonly Type _filterType;
        private readonly Texture2D _gameObjectImage;
        private readonly Dictionary<int, GameObject> _itemsMap = new();
        private readonly ComponentDropdownPopupContent _content;
        private Texture2D _targetTexture = null;

        public ComponentDropdown(TreeViewState state, GameObject rootObject, Type filterType, Action<GameObject> onSelected) : base(state)
        {
            _rootObject = rootObject;
            _filterType = filterType;
            _onSelected = onSelected;
            rowHeight *= 1.5f;
            _defaultType = typeof(GameObject);
            _gameObjectImage = EditorGUIUtility.ObjectContent(null, _defaultType).image as Texture2D;
            _content = new(this);
            setUseHorizontalScroll(true);
            
            void setUseHorizontalScroll(bool value)
            {
                FieldInfo guiFieldInfo = typeof(TreeView).GetField("m_GUI", BindingFlags.Instance | BindingFlags.NonPublic);
                if (null == guiFieldInfo)
                {
                    throw new("TreeView API has changed.");
                }
                object gui = guiFieldInfo.GetValue(this);

                FieldInfo useHorizontalScrollFieldInfo = gui.GetType().GetField("m_UseHorizontalScroll", BindingFlags.Instance | BindingFlags.NonPublic);
                if (null == useHorizontalScrollFieldInfo)
                {
                    throw new("TreeView API has changed.");
                }
                useHorizontalScrollFieldInfo.SetValue(gui, value);
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            _itemsMap.Clear();
            TreeViewItem root = new(0, -1, "root");

            if (_filterType == typeof(GameObject))
            {
                addGameObjectToDropdown(root, _rootObject);
            }
            else
            {
                TreeViewItem treeRootItem = createItem(_rootObject, 0);

                Dictionary<GameObject, TreeViewItem> visited = new()
                {
                    {
                        _rootObject, treeRootItem
                    }
                };
                foreach (Component component in _rootObject.GetComponentsInChildren(_filterType, true))
                {
                    _itemsMap.Add(component.gameObject.GetInstanceID(), component.gameObject);
                    addComponentToDropdown(component.gameObject, visited);
                }

                setDepth(treeRootItem);

                static void setDepth(TreeViewItem item, int depth = 0)
                {
                    item.depth = depth;

                    if (item.hasChildren)
                    {
                        foreach (TreeViewItem treeViewItem in item.children)
                        {
                            setDepth(treeViewItem, depth + 1);
                        }
                    }
                }

                root.AddChild(treeRootItem);
            }
            
            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            IList<TreeViewItem> rows = base.BuildRows(root);
        
            if (!string.IsNullOrEmpty(searchString))
            {
                rows = rows
                    .Where(item => item.displayName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
        
            return rows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            TreeViewItem item = args.item;
        
            Rect labelRect = new(
                string.IsNullOrEmpty(searchString) ?
                    depthIndentWidth + args.item.depth * depthIndentWidth :
                    0,
                args.rowRect.y + (args.rowRect.height - EditorGUIUtility.singleLineHeight) * 0.5f,
                args.rowRect.width,
                EditorGUIUtility.singleLineHeight);
        
            GUIContent guiContent = new(item.displayName, getIconForItem(item));
        
            EditorGUI.LabelField(labelRect, guiContent);
        }
        
        protected override void DoubleClickedItem(int id)
        {
            if (!_itemsMap.TryGetValue(id, out GameObject @object)) return;

            _content.editorWindow.Close();
            _onSelected?.Invoke(@object);
        }

        public void Show(Rect rect)
        {
            Reload();
            ExpandAll();
            PopupWindow.Show(rect, _content);
        }

        private Texture2D getIconForItem(TreeViewItem item)
        {
            return _filterType == _defaultType ||
                   !_itemsMap.TryGetValue(item.id, out GameObject gameObject) ||
                   !gameObject.TryGetComponent(_filterType, out Component component) ?
                _gameObjectImage :
                getFilterTypeToTexture(component);

            Texture2D getFilterTypeToTexture(Component targetComponent)
            {
                if (!_targetTexture)
                {
                    _targetTexture = EditorGUIUtility.ObjectContent(targetComponent, _filterType).image as Texture2D;
                }

                return _targetTexture;
            }
        }

        private void addComponentToDropdown(GameObject selectedObject, Dictionary<GameObject, TreeViewItem> visited, TreeViewItem prevItem = null)
        {
            while (true)
            {
                if (visited.TryGetValue(selectedObject, out TreeViewItem advancedDropdownItem))
                {
                    if (prevItem is not null)
                    {
                        advancedDropdownItem.AddChild(prevItem);
                    }

                    return;
                }

                TreeViewItem gameObjectItem = createItem(selectedObject, 0);
                visited.Add(selectedObject, gameObjectItem);
                if (prevItem is not null)
                {
                    gameObjectItem.AddChild(prevItem);
                }

                selectedObject = selectedObject.transform.parent.gameObject;
                prevItem = gameObjectItem;
            }
        }

        private void addGameObjectToDropdown(TreeViewItem parent, GameObject gameObject, int depth = 0)
        {
            TreeViewItem gameObjectItem = createItem(gameObject, depth);

            parent.AddChild(gameObjectItem);
            _itemsMap.Add(gameObjectItem.id, gameObject);

            foreach (Transform child in gameObject.transform)
            {
                addGameObjectToDropdown(gameObjectItem, child.gameObject, depth + 1);
            }
        }

        private TreeViewItem createItem(GameObject gameObject, int depth)
        {
            return new(gameObject.GetInstanceID(), depth, buildString(gameObject));
        }

        private string buildString(GameObject go)
        {
            using StringBuilderPoolScope scope = new();
            StringBuilder builder = scope.Get();
            builder.Append(go.name);
            builder.Append(" (");
            builder.Append(_filterType == _defaultType || go.TryGetComponent(_filterType, out Component _) ? _filterType.Name : _defaultType.Name);
            builder.Append(")");
            string name = builder.ToString();
            builder.Clear();
            return name;
        }
    }
}
#endif