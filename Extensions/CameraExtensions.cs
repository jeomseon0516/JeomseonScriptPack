using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Extensions
{
    public static class CameraExtensions
    {
        public static Vector2 GetScreenCenterWorldPoint(this Camera camera)
        {
            Vector3 worldScreenCenterPoint = camera.ScreenToWorldPoint(new(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            return new(worldScreenCenterPoint.x, worldScreenCenterPoint.y);
        }
    }
}
