using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Helper
{
    public static class RichTextFormatter
    {
        public static string Colorize(string text, string colorHex)
        {
            return $"<color={colorHex}>{text}</color>";
        }

        public static string Colorize(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
        }

        public static string SpriteIcon(int index)
        {
            return $"<sprite={index}>";
        }

        public static string Bold(string text)
        {
            return $"<b>{text}</b>";
        }

        public static string Italic(string text)
        {
            return $"<i>{text}</i>";
        }

        public static string Size(string text, int size)
        {
            return $"<size={size}>{text}</size>";
        }
    }
}