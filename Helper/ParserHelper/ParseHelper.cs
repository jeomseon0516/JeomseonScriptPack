using System.Collections;
using System.Collections.Generic;
using System;

namespace Jeomseon.Helper
{
    public static class ParseHelper
    {
        public static bool TryParse<T>(string value, out T result) where T : IComparable, IEquatable<T>
        {
            T defaultValue = default(T);
            bool isOk = false;

            result = (T)(object)(defaultValue switch
            {
                byte => (isOk = byte.TryParse(value, out byte byteValue)) ? byteValue : default(byte),
                sbyte => (isOk = sbyte.TryParse(value, out sbyte sbyteValue)) ? sbyteValue : default(sbyte),
                char => (isOk = char.TryParse(value, out char charValue)) ? charValue : default(char),
                short => (isOk = short.TryParse(value, out short shortValue)) ? shortValue : default(short),
                ushort => (isOk = ushort.TryParse(value, out ushort ushortValue)) ? ushortValue : default(ushort),
                int => (isOk = int.TryParse(value, out int intValue)) ? intValue : default(int),
                uint => (isOk = uint.TryParse(value, out uint uintValue)) ? uintValue : default(uint),
                float => (isOk = float.TryParse(value, out float floatValue)) ? floatValue : default(float),
                long => (isOk = long.TryParse(value, out long longValue)) ? longValue : default(long),
                ulong => (isOk = ulong.TryParse(value, out ulong ulongValue)) ? ulongValue : default(ulong),
                double => (isOk = double.TryParse(value, out double doubleValue)) ? doubleValue : default(double),
                decimal => (isOk = decimal.TryParse(value, out decimal decimalValue)) ? decimalValue : default(decimal),
                _ => defaultValue
            });

            return isOk;
        }

        public static T GetTypeToMaxValue<T>(T value) where T : IComparable, IEquatable<T> => (T)(object)(value switch
        {
            byte => byte.MaxValue,
            sbyte => sbyte.MaxValue,
            char => char.MaxValue,
            short => short.MaxValue,
            ushort => ushort.MaxValue,
            int => int.MaxValue,
            uint => uint.MaxValue,
            float => float.MaxValue,
            long => long.MaxValue,
            ulong => ulong.MaxValue,
            double => double.MaxValue,
            decimal => decimal.MaxValue,
            _ => default(object)
        });

        public static T GetTypeToMinValue<T>(T value) where T : IComparable, IEquatable<T> => (T)(object)(value switch
        {
            byte => byte.MinValue,
            sbyte => sbyte.MinValue,
            char => char.MinValue,
            short => short.MinValue,
            ushort => ushort.MinValue,
            int => int.MinValue,
            uint => uint.MinValue,
            float => float.MinValue,
            long => long.MinValue,
            ulong => ulong.MinValue,
            double => double.MinValue,
            decimal => decimal.MinValue,
            _ => default(object)
        });
    }
}