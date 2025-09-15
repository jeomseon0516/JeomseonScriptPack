using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Attribute
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false), Conditional("UNITY_EDITOR")]
    public sealed class SelectableSerializeFieldAttribute : PropertyAttribute {}
}
