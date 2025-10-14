using System;
using System.Reflection;
using UnityEngine;

namespace Jeomseon.ObjectPool
{
    internal static class PoolInitializer
    {
        internal static void Initialize(object obj)
        {
            if (obj == null) return;

            // Component이면 자기 자신만 초기화
            if (obj is Component comp)
            {
                initializeComponent(comp);
            }
            // GameObject이면 붙어 있는 모든 Component 초기화
            else if (obj is GameObject go)
            {
                foreach (var c in go.GetComponents<Component>())
                    initializeComponent(c);
            }
        }

        private static void initializeComponent(Component comp)
        {
            var type = comp.GetType();

            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var prop in props)
            {
                var attr = prop.GetCustomAttribute<PoolInitAttribute>();
                if (attr == null) continue;

                if (!prop.CanWrite) continue; // 읽기 전용이면 패스

                if (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string))
                    prop.SetValue(comp, attr.DefaultValue ?? Activator.CreateInstance(prop.PropertyType));
                else if (typeof(Component).IsAssignableFrom(prop.PropertyType))
                {
                    var mono = comp as MonoBehaviour;
                    if (mono != null)
                    {
                        prop.SetValue(comp, null);
                    }
                }
            }

            // 필드 처리 (기존 방식)
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<PoolInitAttribute>();
                if (attr == null) continue;

                if (field.FieldType.IsValueType || field.FieldType == typeof(string))
                    field.SetValue(comp, attr.DefaultValue ?? Activator.CreateInstance(field.FieldType));
                else if (typeof(Component).IsAssignableFrom(field.FieldType))
                {
                    var mono = comp as MonoBehaviour;
                    if (mono != null)
                    {
                        field.SetValue(comp, null);
                    }
                }
            }
        }
    }
}
