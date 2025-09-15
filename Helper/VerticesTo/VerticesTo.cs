using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Jeomseon.Helper
{
    public static class VerticesTo
    {
        public static float GetHeightFromVertices(GameObject obj)
        {
            return GetTotalSize(obj).y;
        }

        public static float GetZWidthFromVertices(GameObject obj)
        {
            return GetTotalSize(obj).z;
        }

        public static float GetXWidthFromVertices(GameObject obj)
        {
            return GetTotalSize(obj).x;
        }

        public static Vector3 GetMinPoint(GameObject obj)
        {
            Renderer[] renderers = obj
                .GetComponentsInChildren<Renderer>()
                .Where(renderer => renderer is not ParticleSystemRenderer)
                .ToArray();

            if (renderers.Length == 0)
            {
                Debug.Log("vertices not found!");
                return Vector3.zero;
            }

            Bounds totalBounds = renderers[0].bounds;

            foreach (Renderer renderer in renderers)
            {
                totalBounds.Encapsulate(renderer.bounds);
            }

            return totalBounds.min;
        }

        public static Vector3 GetTotalSize(GameObject obj)
        {
            Renderer[] renderers = obj
                .GetComponentsInChildren<Renderer>()
                .Where(renderer => renderer is not ParticleSystemRenderer)
                .ToArray();

            if (renderers.Length == 0)
            {
                Debug.Log("vertices not found!");
                return Vector3.zero;
            }

            Bounds totalBounds = renderers[0].bounds;

            foreach (Renderer renderer in renderers)
            {
                totalBounds.Encapsulate(renderer.bounds);
            }

            return totalBounds.size;
        }
    }
}