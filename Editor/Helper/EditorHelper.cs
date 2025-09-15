#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Jeomseon.Editor;

namespace Jeomseon.Editor
{
    public static class EditorHelper 
    {
        public static object GetPropertyValueFromObject(object obj) => obj switch
        {
            int intValue => intValue,
            bool boolValue => boolValue,
            float floatValue => floatValue,
            string stringValue => stringValue,
            Color colorValue => colorValue,
            UnityEngine.Object objectReferenceValue => objectReferenceValue,
            Enum enumValue => enumValue.ToString(),
            Vector2 vector2Value => vector2Value,
            Vector3 vector3Value => vector3Value,
            Vector4 vector4Value => vector4Value,
            Rect rectValue => rectValue,
            AnimationCurve animationCurveValue => animationCurveValue,
            Bounds boundsValue => boundsValue,
            Quaternion quaternionValue => quaternionValue,
            Vector2Int vector2IntValue => vector2IntValue,
            Vector3Int vector3IntValue => vector3IntValue,
            RectInt rectIntValue => rectIntValue,
            BoundsInt boundsIntValue => boundsIntValue,
            Hash128 hash128Value => hash128Value,
            IEnumerable enumerable => enumerable.OfType<object>().Count(),
            _ => null,
        };

        public static void ClosePreviewSceneByReference(Scene? scene)
        {
            if (scene is null) return;

            EditorSceneManager.CloseScene((Scene)scene, true);
        }

        public static void ClosePreviewSceneByReference(ref Scene? scene)
        {
            ClosePreviewSceneByReference(scene);
            scene = null;
        }
    }
}
#endif
