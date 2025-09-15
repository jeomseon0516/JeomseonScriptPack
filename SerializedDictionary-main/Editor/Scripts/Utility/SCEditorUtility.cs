using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using System.Linq;
using AYellowpaper.SerializedCollections.Editor.Data;
using UnityEngine;
using System.Collections;

namespace AYellowpaper.SerializedCollections.Editor
{
    internal static class SCEditorUtility
    {
        public const string EditorPrefsPrefix = "SC_";
        public const bool KeyFlag = true;
        public const bool ValueFlag = false;

        public static bool GetPersistentBool(string path, bool defaultValue)
        {
            return EditorPrefs.GetBool(EditorPrefsPrefix + path, defaultValue);
        }

        public static bool HasKey(string path)
        {
            return EditorPrefs.HasKey( EditorPrefsPrefix + path );
        }

        public static void SetPersistentBool(string path, bool value)
        {
            EditorPrefs.SetBool(EditorPrefsPrefix + path, value);
        }

        public static float CalculateHeight(SerializedProperty property, DisplayType displayType)
        {
            return CalculateHeight(property, displayType == DisplayType.List);
        }

        public static float CalculateHeight(SerializedProperty property, bool drawAsList)
        {
            if (drawAsList)
            {
                float height = 0;
                foreach (SerializedProperty child in GetChildren(property))
                    height += EditorGUI.GetPropertyHeight(child, true);
                return height;
            }

            return EditorGUI.GetPropertyHeight(property, true);
        }

        public static IEnumerable<SerializedProperty> GetChildren(SerializedProperty property, bool recursive = false)
        {
            if (!property.hasVisibleChildren)
            {
                yield return property;
                yield break;
            }

            SerializedProperty end = property.GetEndProperty();
            property.NextVisible(true);
            do
            {
                yield return property;
            } while (property.NextVisible(recursive) && !SerializedProperty.EqualContents(property, end));
        }

        public static int GetActualArraySize(SerializedProperty arrayProperty)
        {
            return GetChildren(arrayProperty).Count() - 1;
        }

        public static PropertyData GetPropertyData(SerializedProperty property)
        {
            PropertyData data = new PropertyData();
            string json = EditorPrefs.GetString(EditorPrefsPrefix + property.propertyPath, null);
            if (json != null)
                EditorJsonUtility.FromJsonOverwrite(json, data);

            return data;
        }

        public static void SavePropertyData(SerializedProperty property, PropertyData propertyData)
        {
            string json = EditorJsonUtility.ToJson(propertyData);
            EditorPrefs.SetString(EditorPrefsPrefix + property.propertyPath, json);
        }

        public static bool ShouldShowSearch(int pages)
        {
            EditorUserSettings settings = EditorUserSettings.Get();
            return settings.AlwaysShowSearch || pages >= settings.PageCountForSearch;
        }

        public static bool HasDrawerForType(Type type)
        {
            return typeof(SerializedProperty)
                .Assembly
                .GetType("UnityEditor.ScriptAttributeUtility")?
                .GetMethod("GetDrawerTypeForType", BindingFlags.Static | BindingFlags.NonPublic)?
                .Invoke(null, new object[] { type, false }) != null;
        }

        internal static void AddGenericMenuItem(GenericMenu genericMenu, bool isOn, bool isEnabled, GUIContent content, GenericMenu.MenuFunction action)
        {
            if (isEnabled)
                genericMenu.AddItem(content, isOn, action);
            else
                genericMenu.AddDisabledItem(content);
        }

        internal static void AddGenericMenuItem(GenericMenu genericMenu, bool isOn, bool isEnabled, GUIContent content, GenericMenu.MenuFunction2 action, object userData)
        {
            if (isEnabled)
                genericMenu.AddItem(content, isOn, action, userData);
            else
                genericMenu.AddDisabledItem(content);
        }

        internal static bool TryGetTypeFromProperty(SerializedProperty property, out Type type)
        {
            try
            {
                Type classType = typeof(EditorGUI).Assembly.GetType("UnityEditor.ScriptAttributeUtility");
                MethodInfo methodInfo = classType.GetMethod("GetFieldInfoFromProperty", BindingFlags.Static | BindingFlags.NonPublic);
                object[] parameters = new object[] { property, null };
                methodInfo.Invoke(null, parameters);
                type = (Type) parameters[1];
                return true;
            }
            catch
            {
                type = null;
                return false;
            }
        }

        public static object GetPropertyValue(SerializedProperty prop, object target)
        {
            string path = prop.propertyPath.Replace(".Array.data[", "[");
            string[] elements = path.Split('.');
            foreach (string element in elements.Take(elements.Length - 1))
            {
                if (element.Contains("["))
                {
                    string elementName = element[..element.IndexOf("[")];
                    int index = Convert.ToInt32(element[element.IndexOf("[")..].Replace("[", "").Replace("]", ""));
                    target = GetValue(target, elementName, index);
                }
                else
                {
                    target = GetValue(target, element);
                }
            }
            return target;
        }

        public static object GetValue(object source, string name)
        {
            if (source == null) return null;

            Type type = source.GetType();
            FieldInfo f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (f == null)
            {
                PropertyInfo p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p == null)
                    return null;
                return p.GetValue(source, null);
            }
            return f.GetValue(source);
        }

        public static object GetValue(object source, string name, int index)
        {
            IEnumerable enumerable = GetValue(source, name) as IEnumerable;
            IEnumerator enm = enumerable.GetEnumerator();
            while (index-- >= 0)
                enm.MoveNext();
            return enm.Current;
        }
    }
}