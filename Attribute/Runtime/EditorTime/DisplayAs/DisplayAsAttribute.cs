using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Attribute
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class), Conditional("UNITY_EDITOR")]
    public sealed class DisplayAsAttribute : PropertyAttribute
    {
        public string DisplayName { get; }

        public DisplayAsAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
