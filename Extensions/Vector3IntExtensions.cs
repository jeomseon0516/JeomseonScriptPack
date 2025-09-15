using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Extensions
{
    public static class Vector3IntExtensions
    {
        public static Vector3Int Abs(Vector3Int vec) => new(
            Mathf.Abs(vec.x),
            Mathf.Abs(vec.y),
            Mathf.Abs(vec.z));
    }
}
