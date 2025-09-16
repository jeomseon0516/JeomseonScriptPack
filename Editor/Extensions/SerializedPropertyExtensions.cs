#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Jeomseon.Editor.Extensions
{
    using Attribute = System.Attribute;

    public static class SerializedPropertyExtensions
    {
        public static Type GetPropertyType(this SerializedProperty prop)
        {
            Object targetObject = prop?.serializedObject.targetObject;
            return !targetObject ? null : EditorReflectionHelper.GetPropertyType(targetObject, prop.propertyPath);
        }

        public static Type GetParentType(this SerializedProperty prop)
        {
            // SerializedObject의 targetObject (루트 객체)를 가져옴
            Type rootType = prop.serializedObject.targetObject.GetType();
            Type targetType = rootType;

            // propertyPath를 '.' 기준으로 나누어 부모 필드를 추적
            string[] fieldNames = prop.propertyPath.Replace(".Array.data[", "[").Split('.');

            // 마지막 필드 이전까지 추적해서 부모 필드의 타입을 얻음
            for (int i = 0; i < fieldNames.Length - 1; i++)
            {
                if (fieldNames[i].Contains("["))
                {
                    // 배열 또는 리스트 타입 추적 (배열 요소의 타입을 찾음)
                    string fieldName = fieldNames[i][..fieldNames[i].IndexOf("[", StringComparison.Ordinal)];
                    FieldInfo field = targetType?.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                    // 배열이나 리스트라면 요소 타입을 추적함
                    if (field is not null)
                    {
                        targetType = field.FieldType.IsArray ? field.FieldType.GetElementType() : field.FieldType.GetGenericArguments()[0];
                    }
                }
                else
                {
                    // 일반 필드라면 해당 필드의 타입을 추적
                    if (targetType is not null)
                    { 
                        FieldInfo field = targetType.GetField(fieldNames[i], BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                       targetType = field?.FieldType;
                    }
                }
            }

            return targetType ?? rootType; // 부모 필드의 타입 반환
        }
        
        // SerializedProperty의 부모 PropertyPath를 얻는 메서드
        public static string GetParentPropertyPath(this SerializedProperty prop)
        {
            string path = prop.propertyPath;

            // 마지막 필드를 제외하고 부모 경로 반환
            int lastDotIndex = path.LastIndexOf('.');
            return lastDotIndex == -1 ? string.Empty : path[..lastDotIndex];

        }

        public static bool IsNestedAttribute<TAttributeType>(this SerializedProperty prop) where TAttributeType : Attribute
        {
            return EditorReflectionHelper.IsNestedAttribute<TAttributeType>(
                prop.serializedObject.targetObject,
                prop.propertyPath);
        }

        public static object GetPropertyValue(this SerializedProperty prop) => prop?.propertyType switch
        {
            SerializedPropertyType.Generic or SerializedPropertyType.ManagedReference => prop.managedReferenceValue,
            SerializedPropertyType.Integer or SerializedPropertyType.LayerMask or SerializedPropertyType.Character => prop.intValue,
            SerializedPropertyType.Boolean => prop.boolValue,
            SerializedPropertyType.Float => prop.floatValue,
            SerializedPropertyType.String => prop.stringValue,
            SerializedPropertyType.Color => prop.colorValue,
            SerializedPropertyType.ObjectReference => prop.objectReferenceValue,
            SerializedPropertyType.Enum => prop.enumNames[prop.enumValueIndex],
            SerializedPropertyType.Vector2 => prop.vector2Value,
            SerializedPropertyType.Vector3 => prop.vector3Value,
            SerializedPropertyType.Vector4 => prop.vector4Value,
            SerializedPropertyType.Rect => prop.rectValue,
            SerializedPropertyType.ArraySize => prop.arraySize,
            SerializedPropertyType.AnimationCurve => prop.animationCurveValue,
            SerializedPropertyType.Bounds => prop.boundsValue,
            SerializedPropertyType.Gradient => prop.gradientValue,
            SerializedPropertyType.Quaternion => prop.quaternionValue,
            SerializedPropertyType.ExposedReference => prop.exposedReferenceValue,
            SerializedPropertyType.FixedBufferSize => prop.fixedBufferSize,
            SerializedPropertyType.Vector2Int => prop.vector2IntValue,
            SerializedPropertyType.Vector3Int => prop.vector3IntValue,
            SerializedPropertyType.RectInt => prop.rectIntValue,
            SerializedPropertyType.BoundsInt => prop.boundsIntValue,
            SerializedPropertyType.Hash128 => prop.hash128Value,
            _ => null,
        };
    }
}
#endif