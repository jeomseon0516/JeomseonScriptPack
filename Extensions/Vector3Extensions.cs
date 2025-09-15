using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Extensions
{
    public static class Vector3Extensions
    {
        public static Color ToColor(this Vector3 vec) => new(vec.x, vec.y, vec.z);
        public static Vector3 Abs(this Vector3 vec) => new(
            Mathf.Abs(vec.x),
            Mathf.Abs(vec.y),
            Mathf.Abs(vec.z));
    }
}

