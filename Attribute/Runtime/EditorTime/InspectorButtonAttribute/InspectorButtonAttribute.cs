using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// .. 인스펙터의 버튼을 누르면 메서드를 호출합니다
/// </summary>
namespace Jeomseon.Attribute
{
    using Attribute = System.Attribute;

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false), Conditional("UNITY_EDITOR")]
    public sealed class InspectorButtonAttribute : Attribute
    {
        public string ButtonName { get; } = string.Empty;

        public InspectorButtonAttribute(string buttonName)
        {
            ButtonName = buttonName;
        }
    }
}
