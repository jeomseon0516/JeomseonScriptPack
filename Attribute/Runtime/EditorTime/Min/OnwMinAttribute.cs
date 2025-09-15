using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Jeomseon.Attribute
{
    [AttributeUsage(AttributeTargets.Field), Conditional("UNITY_EDITOR")]
    public class OnwMinAttribute : PropertyAttribute
    {
        public float Min { get; }

        public OnwMinAttribute(float min)
        {
            Min = min;
        }
    }
}
