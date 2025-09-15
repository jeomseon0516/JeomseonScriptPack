using System;

namespace Jeomseon.Helper
{
    public static class NumericsHelper
    {
        // -----------------------------
        // Min
        // -----------------------------
        public static T Min<T>(T value, T min) where T : IComparable<T>
        {
            return value.CompareTo(min) < 0 ? min : value;
        }

        // -----------------------------
        // Max
        // -----------------------------
        public static T Max<T>(T value, T max) where T : IComparable<T>
        {
            return value.CompareTo(max) > 0 ? max : value;
        }

        // -----------------------------
        // Clamp
        // -----------------------------
        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0) return min;
            if (value.CompareTo(max) > 0) return max;
            return value;
        }
    }
}
