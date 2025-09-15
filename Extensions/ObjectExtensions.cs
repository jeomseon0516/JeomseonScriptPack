using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Jeomseon.Extensions
{
    public static class ObjectExtensions
    {
        public static bool Is<TCast>(this object obj, Action<TCast> castCall)
        {
            if (obj is not TCast castClass) return false;
            
            castCall?.Invoke(castClass);
            return true;
        }

        public static T GetFieldByName<T>(this object obj, string fieldName, BindingFlags flags)
        {
            return obj.GetFieldByName(fieldName, flags) is T t ? t : default(T);
        }

        public static object GetFieldByName(this object obj, string fieldName, BindingFlags flags)
        {
            return obj
                .GetType()
                .GetField(fieldName, flags)?
                .GetValue(obj);;
        }

        public static bool TryGetFieldByName<T>(this object obj, string fieldName, BindingFlags flags, out T field)
        {
            if (obj.GetFieldByName(fieldName, flags) is T t)
            {
                field = t;
                return true;
            }
  
            field = default(T);
            return false;
        }

        public static bool TryGetFieldByName(this object obj, string fieldName, BindingFlags flags, out object field)
        {
            FieldInfo info = obj.GetType().GetField(fieldName, flags);
            
            if (info is null)
            {
                field = null;
                return false;
            }

            field = info.GetValue(obj);
            return true;
        }
    }
}
