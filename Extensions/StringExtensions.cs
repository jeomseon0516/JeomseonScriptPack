using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Extensions
{
    public static class StringExtensions
    {
        public static int FindIndex(this string str, char chr)
        {
            int index = -1;

            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] != chr) continue;

                index = i;
                break;
            }

            return index;
        }
    }
}
