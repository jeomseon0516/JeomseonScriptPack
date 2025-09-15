using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Extensions
{
    public static class ComponentExtensions
    {
        public static void SetActiveGameObject(this Component component, bool isActive) => component.gameObject.SetActive(isActive); 
        
        public static Vector3 GetPosition(this Component component) => component.transform.position;
        public static Vector3 GetLocalPosition(this Component component) => component.transform.localPosition;

        public static Vector3 GetLocalScale(this Component component) => component.transform.localScale;
        public static Vector3 GetLossyScale(this Component component) => component.transform.lossyScale;

        public static float GetPositionX(this Component component) => component.transform.position.x;
        public static float GetPositionY(this Component component) => component.transform.position.y;
        public static float GetPositionZ(this Component component) => component.transform.position.z;

        public static float GetLocalPositionX(this Component component) => component.transform.localPosition.x;
        public static float GetLocalPositionY(this Component component) => component.transform.localPosition.y;
        public static float GetLocalPositionZ(this Component component) => component.transform.localPosition.z;

        public static float GetLocalScaleX(this Component component) => component.transform.localScale.x;
        public static float GetLocalScaleY(this Component component) => component.transform.localScale.y;
        public static float GetLocalScaleZ(this Component component) => component.transform.localScale.z;
        
        public static float GetLossyScaleX(this Component component) => component.transform.lossyScale.x;
        public static float GetLossyScaleY(this Component component) => component.transform.lossyScale.y;
        public static float GetLossyScaleZ(this Component component) => component.transform.lossyScale.z;

        public static void SetParent(this Component component, GameObject parentObject, bool worldPositionStays)
            => component.transform.SetParent(parentObject.transform, worldPositionStays);
        public static void SetParent(this Component component, Transform parentTransform, bool worldPositionStays)
            => component.transform.SetParent(parentTransform, worldPositionStays);
        
        public static void SetPosition(this Component component, in Vector3 position)
            => component.transform.position = position;
        
        public static void SetPositionX(this Component component, float x)
            => component.transform.position = new(x, component.transform.position.y, component.transform.position.z);
        
        public static void SetPositionY(this Component component, float y)
            => component.transform.position = new(component.transform.position.x, y, component.transform.position.z);

        public static void SetPositionZ(this Component component, float z)
            => component.transform.position = new(component.transform.position.x, component.transform.position.y, z);

        public static void SetLocalPositionX(this Component component, float x)
            => component.transform.localPosition = new(x, component.transform.localPosition.y, component.transform.localPosition.z);

        public static void SetLocalPositionY(this Component component, float y)
            => component.transform.localPosition = new(component.transform.localPosition.x, y, component.transform.localPosition.z);

        public static void SetLocalPositionZ(this Component component, float z)
            => component.transform.localPosition = new(component.transform.localPosition.x, component.transform.localPosition.y, z);

        public static void SetLocalScale(this Component component, in Vector3 scale)
            => component.transform.localScale = scale;

        public static void SetLocalScaleX(this Component component, float scale)
            => component.transform.localScale = new(scale, component.transform.localScale.y, component.transform.localScale.z);

        public static void SetLocalScaleY(this Component component, float scale)
            => component.transform.localScale = new(component.transform.localScale.x, scale, component.transform.localScale.z);

        public static void SetLocalScaleZ(this Component component, float scale)
            => component.transform.localScale = new(component.transform.localScale.x, component.transform.localScale.y, scale);
    }
}
