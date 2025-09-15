using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace Jeomseon.Attribute
{
    [AttributeUsage(AttributeTargets.Field), Conditional("UNITY_EDITOR")]
    public class LocalizedStringAttribute : PropertyAttribute
    {
        public bool CanSelectTable { get; }
        public string TableName { get; }
        public string EntryKey { get; }

        public LocalizedStringAttribute(bool canSelectTable = false, string tableName = null, string entryKey = null)
        {
            CanSelectTable = canSelectTable;
            TableName = tableName;
            EntryKey = entryKey;
        }

    }
}