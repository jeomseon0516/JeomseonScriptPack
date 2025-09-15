using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Attribute
{
    public sealed class Vector2RangeAttribute : PropertyAttribute
    {
        public float Min { get; }
        public float Max { get; }

        public Vector2RangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}

