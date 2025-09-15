#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Jeomseon.Attribute;

namespace Jeomseon.Attribute.Editor
{
    [CustomPropertyDrawer(typeof(SpritePreviewAttribute))]
    internal sealed class SpritePreviewDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (attribute is not SpritePreviewAttribute spritePreviewAttribute) return;

            EditorGUI.PropertyField(
                new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                property,
                label);

            if (property.objectReferenceValue && property.objectReferenceValue is Sprite sprite)
            {
                Rect previewRect = new(
                    position.x,
                    position.y + EditorGUIUtility.singleLineHeight,
                    spritePreviewAttribute.Size,
                    spritePreviewAttribute.Size);

                EditorGUI.DrawPreviewTexture(
                    previewRect,
                    sprite.texture,
                    null,
                    ScaleMode.ScaleToFit);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SpritePreviewAttribute spritePreviewAttribute = attribute as SpritePreviewAttribute;
            float totalHeight = EditorGUIUtility.singleLineHeight;

            if (property.objectReferenceValue && property.objectReferenceValue is Sprite)
            {
                totalHeight += spritePreviewAttribute.Size + EditorGUIUtility.standardVerticalSpacing;
            }

            return totalHeight;
        }
    }
}
#endif