using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Extensions
{
    public static class Vector2Extensions
    {
        public static Vector2 Abs(this Vector2 vec) => new(
            Mathf.Abs(vec.x), 
            Mathf.Abs(vec.y));
    }
}

