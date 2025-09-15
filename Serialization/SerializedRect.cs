using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Jeomseon.Helper
{
    [System.Serializable]
    public struct SerializedRect
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public SerializedRect(in Rect rect)
        {
            X = rect.x;
            Y = rect.y;
            Width = rect.width;
            Height = rect.height;
        }

        public static implicit operator SerializedRect(in Rect rect)
        {
            return new(rect);
        }

        public static implicit operator Rect(in SerializedRect serializedRect)
        {
            return serializedRect.ToRect();
        }

        public Rect ToRect()
        {
            return new(X, Y, Width, Height);
        }
    }
}

