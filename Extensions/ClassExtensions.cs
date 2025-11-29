using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Extensiosn
{
    public static class ClassExtensions 
    {
        public static bool IsNotNull<T>(this T obj, Action<T> aciton) where T : class => obj is not null ? (aciton?.Invoke(obj), true) : false;
    }
}
