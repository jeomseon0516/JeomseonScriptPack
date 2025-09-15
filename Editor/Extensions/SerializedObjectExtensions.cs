#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Jeomseon.Editor.Extensions
{
    public static class SerializedObjectExtensions
    {
        public static SerializedProperty GetProperty(this SerializedObject serializedObject, string propertyName)
        {
            return serializedObject.FindProperty(propertyName) ??
                serializedObject.FindProperty(EditorReflectionHelper.GetBackingFieldName(propertyName));
        }

        public static SerializedProperty GetPropertyByPath(this SerializedObject serializedObject, string propPath)
        {
            string[] paths = propPath.Split('.');
            SerializedProperty prop = null;
            if (paths.Length > 0)
            {
                prop = serializedObject.FindProperty(paths[0]);
                
                for (int i = 1; i < paths.Length; i++)
                {
                    prop = prop.FindPropertyRelative(paths[i]);
                }
            }

            return prop;
        }
    }
}
#endif
