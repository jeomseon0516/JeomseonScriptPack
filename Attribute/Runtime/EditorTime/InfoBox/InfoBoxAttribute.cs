using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Jeomseon.Attribute
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true), Conditional("UNITY_EDITOR")]
    public sealed class InfoBoxAttribute : PropertyAttribute
    {
        public string Message { get; }
        public INFO_TYPE InfoType { get; }

        public InfoBoxAttribute(string message, INFO_TYPE infoType = INFO_TYPE.INFO)
        {
            Message = message;
            InfoType = infoType;
        }
    }

    public enum INFO_TYPE
    {
        INFO,
        WARNING,
        ERROR
    }
}