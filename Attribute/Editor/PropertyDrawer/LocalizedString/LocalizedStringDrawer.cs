#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEditor.Localization;

namespace Jeomseon.Attribute.Editor
{
    [CustomPropertyDrawer(typeof(LocalizedStringAttribute))]
    internal sealed class LocalizedStringDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, StringTableCollection> _tableCache = new();
        private static EntryAdvancedDropdown _entryDropdown = null;
        private static List<Locale> _localesCache;

        static LocalizedStringDrawer()
        {
            // 로케일 변경 이벤트 구독
            LocalizationEditorSettings.EditorEvents.LocaleAdded += onLocaleChanged;
            LocalizationEditorSettings.EditorEvents.LocaleRemoved += onLocaleChanged;
        }

        private static void onLocaleChanged(Locale locale)
        {
            // 로케일 캐시 무효화
            _localesCache = null;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            LocalizedStringAttribute attr = (LocalizedStringAttribute)attribute;

            if (property.propertyType != SerializedPropertyType.Generic || property.type != nameof(LocalizedString))
            {
                EditorGUI.LabelField(position, label.text, "LocalizedStringAttribute는 LocalizedString 타입에만 사용 가능합니다.");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            
            // 현재 위치 저장
            Rect currentPosition = position;

            // 레이블 그리기
            currentPosition.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(currentPosition, property.isExpanded, label);

            // 다음 줄로 이동
            currentPosition.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (property.isExpanded)
            {
                SerializedProperty tableReferenceProp = property.FindPropertyRelative("m_TableReference");
                SerializedProperty tableEntryReferenceProp = property.FindPropertyRelative("m_TableEntryReference");
                SerializedProperty tableNameProp = tableReferenceProp.FindPropertyRelative("m_TableCollectionName");
                SerializedProperty tableEntryKeyProp = tableEntryReferenceProp.FindPropertyRelative("m_Key");

                // 테이블 이름 및 엔트리 키 설정
                string tableName = "";

                if (!attr.CanSelectTable)
                {
                    tableName = attr.TableName;

                    if (string.IsNullOrEmpty(tableName))
                    {
                        tableName = tableNameProp.stringValue;
                        if (string.IsNullOrEmpty(tableName))
                        {
                            tableName = tableNameProp.stringValue = property.serializedObject.targetObject.GetType().Name;
                        }
                        else
                        {
                            if (tableName != property.serializedObject.targetObject.GetType().Name)
                            {
                                tableName = tableNameProp.stringValue = property.serializedObject.targetObject.GetType().Name;
                            }
                        }
                    }
                }
                else
                {
                    tableName = tableNameProp.stringValue;

                    tableName = EditorGUI.TextField(currentPosition, "Table Name", tableName);
                    currentPosition.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    if (tableName != tableNameProp.stringValue)
                    {
                        tableNameProp.stringValue = tableName;
                    }
                }

                string entryKey = attr.EntryKey;
                if (string.IsNullOrEmpty(entryKey))
                {
                    entryKey = tableEntryKeyProp.stringValue;
                }

                // 테이블 컬렉션 가져오기
                StringTableCollection tableCollection = getTableCollection(tableName);
                // 엔트리 가져오기
                SharedTableData.SharedTableEntry sharedTableEntry = null;
                if (tableCollection)
                {
                    sharedTableEntry = getSharedTableEntry(tableCollection, entryKey);
                    tableReferenceProp.FindPropertyRelative("m_TableCollectionName").stringValue = tableCollection.SharedData.TableCollectionName;

                    if (sharedTableEntry is null && !string.IsNullOrEmpty(attr.EntryKey))
                    {
                        sharedTableEntry = tableCollection.SharedData.AddKey(entryKey);
                        EditorUtility.SetDirty(tableCollection);
                        EditorUtility.SetDirty(tableCollection.SharedData);
                    }
                }

                tableEntryReferenceProp.FindPropertyRelative("m_KeyId").longValue = sharedTableEntry?.Id ?? 0;

                if (!tableCollection)
                {
                    EditorGUI.HelpBox(currentPosition, $"테이블 '{tableName}'이(가) 존재하지 않습니다.", MessageType.Error);
                    currentPosition.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    // 테이블 생성 버튼 제공
                    if (!string.IsNullOrEmpty(tableName))
                    {
                        if (GUI.Button(new(currentPosition.x, currentPosition.y, currentPosition.width, EditorGUIUtility.singleLineHeight), "테이블 생성"))
                        {
                            // 테이블 생성 로직
                            string folderPath = EditorUtility.OpenFolderPanel("테이블을 생성할 폴더를 선택하세요.", "Assets", "") + $"/{tableName}/";
                            if (!string.IsNullOrEmpty(folderPath))
                            {
                                folderPath = FileUtil.GetProjectRelativePath(folderPath);
                                if (string.IsNullOrEmpty(folderPath))
                                {
                                    Debug.LogError("선택한 폴더가 프로젝트 내에 없습니다.");
                                }
                                else
                                {
                                    createStringTable(tableName, folderPath);
                                }
                            }
                        }
                        currentPosition.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
                else if (sharedTableEntry is null)
                {
                    EditorGUI.HelpBox(currentPosition, $"엔트리 '{entryKey}'이(가) 테이블 '{tableName}'에 존재하지 않습니다.", MessageType.Error);
                    currentPosition.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }
                else
                {
                    // 로케일 목록 가져오기
                    List<Locale> locales = getLocales();

                    if (string.IsNullOrEmpty(attr.EntryKey))
                    {
                        SharedTableData tableData = tableCollection.SharedData;

                        float halfCurrentPositionWidth = currentPosition.width * 0.5f;
                        Rect keyTextRect = new(currentPosition.x, currentPosition.y, halfCurrentPositionWidth - EditorGUIUtility.standardVerticalSpacing, currentPosition.height);
                        Rect deleteButtonRect = new(currentPosition.x + halfCurrentPositionWidth, currentPosition.y, halfCurrentPositionWidth, currentPosition.height);
                        string key = EditorGUI.TextField(keyTextRect, "Entry Key", sharedTableEntry.Key);
                        if (key != sharedTableEntry.Key && !string.IsNullOrEmpty(key) && !tableData.Entries.Any(entry => entry != sharedTableEntry && entry.Key == key))
                        {
                            tableData.RenameKey(sharedTableEntry.Key, key);
                            tableEntryKeyProp.stringValue = key;
                            EditorUtility.SetDirty(tableData);
                        }

                        if (GUI.Button(deleteButtonRect, "Delete"))
                        {
                            tableData.RemoveKey(sharedTableEntry.Key);
                            tableEntryKeyProp.stringValue = "";
                            EditorUtility.SetDirty(tableData);
                        }

                        currentPosition.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }

                    foreach (Locale locale in locales)
                    {
                        // 해당 로케일의 테이블 가져오기
                        StringTable stringTable = tableCollection.GetTable(locale.Identifier) as StringTable;

                        if (stringTable)
                        {
                            // 엔트리 가져오기
                            // 엔트리가 없을 경우 생성
                            StringTableEntry entry = stringTable.GetEntry(sharedTableEntry.Id) ?? stringTable.AddEntry(sharedTableEntry.Id, "");
                            
                            EditorGUI.LabelField(currentPosition, locale.LocaleName);
                            currentPosition.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                            // 번역 값 표시 및 편집
                            Rect optionRect = new(currentPosition.x, currentPosition.y, currentPosition.width, EditorGUIUtility.singleLineHeight * 5 + EditorGUIUtility.standardVerticalSpacing * 2);
                            EditorGUI.DrawRect(optionRect, ColorUtility.TryParseHtmlString("#413a4f", out Color color) ? color : Color.black);

                            bool isSmart = EditorGUI.Toggle(currentPosition, "Is Smart", entry.IsSmart);
                            currentPosition.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                            string newValue = EditorGUI.TextArea(
                                new(currentPosition.x, currentPosition.y, currentPosition.width, EditorGUIUtility.singleLineHeight * 4), 
                                entry.Value);
                            currentPosition.y += EditorGUIUtility.singleLineHeight * 4 + EditorGUIUtility.standardVerticalSpacing;

                            if (isSmart != entry.IsSmart)
                            {
                                entry.IsSmart = isSmart;
                                EditorUtility.SetDirty(stringTable);
                                EditorUtility.SetDirty(tableCollection);
                            }

                            // 값이 변경되었을 경우 업데이트
                            if (newValue != entry.Value)
                            {
                                entry.Value = newValue;
                                EditorUtility.SetDirty(stringTable);
                                EditorUtility.SetDirty(tableCollection);
                            }
                        }
                        else
                        {
                            // 해당 로케일의 테이블이 없을 경우
                            currentPosition.height = EditorGUIUtility.singleLineHeight;
                            EditorGUI.LabelField(currentPosition, locale.LocaleName, "해당 로케일의 테이블이 존재하지 않습니다.");
                        }
                    }
                }

                if (tableCollection && string.IsNullOrEmpty(attr.EntryKey))
                {
                    Rect buttonRect = new(currentPosition.x, currentPosition.y, currentPosition.width, EditorGUIUtility.singleLineHeight);
                    // 엔트리 생성 버튼 제공
                    if (GUI.Button(buttonRect, "엔트리 선택"))
                    {
                        _entryDropdown = new(new(), getTableCollection(tableName), tableCollection.SharedData)
                        {
                            TargetProp = tableEntryKeyProp
                        };
                        _entryDropdown.Show(buttonRect);
                    }

                    currentPosition.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            // 인덴트 복원
            EditorGUI.indentLevel = indent;

            // 프로퍼티 종료
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = 0f;

            // 기본 레이블 높이
            totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (property.isExpanded)
            {
                LocalizedStringAttribute attr = (LocalizedStringAttribute)attribute;

                string tableName = attr.TableName ?? property.FindPropertyRelative("m_TableReference").FindPropertyRelative("m_TableCollectionName").stringValue;
                string entryKey = attr.EntryKey ?? property.FindPropertyRelative("m_TableEntryReference").FindPropertyRelative("m_Key").stringValue;

                StringTableCollection tableCollection = getTableCollection(tableName);
                SharedTableData.SharedTableEntry sharedTableEntry = null;

                if (attr.CanSelectTable)
                {
                    totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                if (tableCollection)
                {
                    sharedTableEntry = getSharedTableEntry(tableCollection, entryKey);
                }

                if (!tableCollection)
                {
                    if (!string.IsNullOrEmpty(tableName))
                    {
                        totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(attr.EntryKey))
                    {
                        totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }

                    if (sharedTableEntry is not null)
                    {
                        // 로케일 목록 가져오기
                        List<Locale> locales = getLocales();
                        totalHeight += locales.Sum(_ => EditorGUIUtility.singleLineHeight * 6 + EditorGUIUtility.standardVerticalSpacing * 3);
                    }
                }

                totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            return totalHeight;
        }

        private static StringTableCollection getTableCollection(string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) return null;

            if (_tableCache.TryGetValue(tableName, out StringTableCollection tableCollection))
            {
                return tableCollection;
            }

            tableCollection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            if (tableCollection)
            {
                _tableCache[tableName] = tableCollection;
            }

            return tableCollection;
        }

        private static SharedTableData.SharedTableEntry getSharedTableEntry(StringTableCollection tableCollection, string entryKey)
        {
            if (!tableCollection || string.IsNullOrEmpty(entryKey)) return null;

            // 캐시를 사용하지 않고 직접 엔트리 가져오기
            SharedTableData sharedTableData = tableCollection.SharedData;
            return sharedTableData.GetEntry(entryKey);
        }

        private static List<Locale> getLocales()
        {
            _localesCache ??= new(LocalizationEditorSettings.GetLocales());
            return _localesCache;
        }

        private static void createStringTable(string tableName, string folderPath)
        {
            // EditorApplication.delayCall을 사용하여 다음 에디터 사이클로 작업을 미룸
            EditorApplication.delayCall += () =>
            {
                // 테이블 컬렉션 생성
                StringTableCollection collection = LocalizationEditorSettings.CreateStringTableCollection(tableName, folderPath);

                // 에셋 데이터베이스 저장 및 갱신
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // 캐시 업데이트
                _tableCache[tableName] = collection;

                Debug.Log($"테이블 '{tableName}'이(가) '{folderPath}'에 생성되었습니다.");
            };
        }
    }
}
#endif