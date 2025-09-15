using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jeomseon.Helper;
using System;

namespace Jeomseon.Extensions
{
    public static class NumericExtensions
    {
        public static T Min<T>(this T value, T min) where T : IComparable<T>
        {
            return NumericsHelper.Min(value, min);
        }

        public static T Max<T>(this T value, T min) where T : IComparable<T>
        {
            return NumericsHelper.Max(value, min);
        }

        public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T>
        {
            return NumericsHelper.Clamp(value, min, max);
        }
    }
}

