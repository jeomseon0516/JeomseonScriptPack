#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Jeomseon.Attribute.Editor
{
    [CustomPropertyDrawer(typeof(InfoBoxAttribute))]
    internal sealed class InfoBoxDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InfoBoxAttribute infoBoxAttribute = (attribute as InfoBoxAttribute)!;
            
            MessageType messageType = infoBoxAttribute.InfoType switch
            {
                INFO_TYPE.INFO => MessageType.Info,
                INFO_TYPE.WARNING => MessageType.Warning,
                INFO_TYPE.ERROR => MessageType.Error,
                _ => MessageType.None,
            };

            // 텍스트의 높이를 계산
            float textHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(infoBoxAttribute.Message), position.width);

            Rect helpBoxRect = new(
                position.x,
                position.y,
                position.width,
                textHeight);

            Rect propertyRect = new(
                position.x,
                position.y + textHeight + EditorGUIUtility.standardVerticalSpacing,
                position.width,
                EditorGUI.GetPropertyHeight(property, label, true));

            EditorGUI.HelpBox(helpBoxRect, infoBoxAttribute.Message, messageType);
            EditorGUI.PropertyField(propertyRect, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 텍스트의 높이를 계산
            InfoBoxAttribute infoBoxAttribute = (attribute as InfoBoxAttribute)!;
            float textHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(infoBoxAttribute.Message), EditorGUIUtility.currentViewWidth);
            
            return textHeight + EditorGUI.GetPropertyHeight(property, label, true) + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
#endif