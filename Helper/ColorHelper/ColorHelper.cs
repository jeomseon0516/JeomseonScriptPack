using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Helper
{
    public static class ColorHelper
    {
        public static Color GetColorFrom255(byte r, byte g, byte b, byte a)
        {
            return new(getFrom255(r), getFrom255(g), getFrom255(b), getFrom255(a));
        }
        
        public static Color GetColorFrom255(byte r, byte g, byte b)
        {
            return new(getFrom255(r), getFrom255(g), getFrom255(b));
        }

        public static Color HexToColor(string hex)
        {
            hex = hex.Replace("#", "");

            return hex.Length switch
            {
                6 => GetColorFrom255(
                    fromSubstringToByte(hex[..2]),
                    fromSubstringToByte(hex.Substring(2, 2)),
                    fromSubstringToByte(hex.Substring(4, 2))),
                8 => GetColorFrom255(
                    fromSubstringToByte(hex[..2]),
                    fromSubstringToByte(hex.Substring(2, 2)),
                    fromSubstringToByte(hex.Substring(4, 2)),
                    fromSubstringToByte(hex.Substring(6, 2))),
                _ => Color.white
            };
            
            static byte fromSubstringToByte(string subString) => byte.Parse(subString, System.Globalization.NumberStyles.HexNumber);
        }
        
        private static float getFrom255(float v)
        {
            return v / 255f;
        }
    }
}

