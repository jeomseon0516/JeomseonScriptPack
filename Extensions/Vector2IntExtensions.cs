using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Extensions
{
    public static class Vector2IntExtensions
    {
        public static Vector2Int Abs(this Vector2Int vec) => new(
            Mathf.Abs(vec.x),
            Mathf.Abs(vec.y));
    }
}
