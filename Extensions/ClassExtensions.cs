using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Extensiosn
{
    public static class ClassExtensions 
    {
        public static bool IsNotNull<T>(this T obj, Action<T> aciton) where T : class 
        { 
            if (obj != null) { aciton?.Invoke(obj); return true; } else { return false; } 
        }
    }
}
