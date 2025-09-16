#if SERIALIZEDREFERENCEDROPDOWN_INSTALLED && UNIRX_INSTALLED
using UnityEngine;

namespace Jeomseon.UnityReactive
{
    
    [System.Serializable]
    public class MinIntProcessor : IValueProcessor
    {
        [field: SerializeField] public int Min { get; private set; }
        public T Process<T>(T value)
        {
            return value is int intValue && Mathf.Max(intValue, Min) is T newValue ? newValue : value;
        }

        public MinIntProcessor(int min) => Min = min;
    }
    
    [System.Serializable]
    public class MaxIntProcessor : IValueProcessor
    {
        [field: SerializeField] public int Max { get; private set; }
        public T Process<T>(T value)
        {
            return value is int intValue && Mathf.Min(intValue, Max) is T newValue ? newValue : value;
        }

        public MaxIntProcessor(int min) => Max = min;
    }
    
    [System.Serializable]
    public class ClampIntProcessor : IValueProcessor
    {
        [field: SerializeField] public int Min { get; private set; }
        [field: SerializeField] public int Max { get; private set; }
        public T Process<T>(T value)
        {
            return value is int intValue && Mathf.Clamp(intValue, Min, Max) is T newValue ? newValue : value;
        }

        public ClampIntProcessor(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }
    
    public interface IValueProcessor
    {
        public T Process<T>(T value);
    }
}
#endif