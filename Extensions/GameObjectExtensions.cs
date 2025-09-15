using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Extensions
{
    public static class GameObjectExtensions
    {
        public static Vector3 GetPosition(this GameObject gameObject) => gameObject.transform.position;
        public static Vector3 GetLocalPosition(this GameObject gameObject) => gameObject.transform.localPosition;
        public static Vector3 GetLocalScale(this GameObject gameObject) => gameObject.transform.localScale;
        public static Vector3 GetLossyScale(this GameObject gameObject) => gameObject.transform.lossyScale;
        
        public static float GetPositionX(this GameObject gameObject) => gameObject.transform.position.x;
        public static float GetPositionY(this GameObject gameObject) => gameObject.transform.position.y;
        public static float GetPositionZ(this GameObject gameObject) => gameObject.transform.position.z;
        
        public static float GetLocalPositionX(this GameObject gameObject) => gameObject.transform.localPosition.x;
        public static float GetLocalPositionY(this GameObject gameObject) => gameObject.transform.localPosition.y;
        public static float GetLocalPositionZ(this GameObject gameObject) => gameObject.transform.localPosition.z;
        
        public static float GetLocalScaleX(this GameObject gameObject) => gameObject.transform.localScale.x;
        public static float GetLocalScaleY(this GameObject gameObject) => gameObject.transform.localScale.y;
        public static float GetLocalScaleZ(this GameObject gameObject) => gameObject.transform.localScale.z;

        public static float GetLossyScaleX(this GameObject gameObject) => gameObject.transform.lossyScale.x; 
        public static float GetLossyScaleY(this GameObject gameObject) => gameObject.transform.lossyScale.y; 
        public static float GetLossyScaleZ(this GameObject gameObject) => gameObject.transform.lossyScale.z; 
        
        public static void SetPositionX(this GameObject gameObject, float x) => gameObject.transform.SetPositionX(x);
        public static void SetPositionY(this GameObject gameObject, float y) => gameObject.transform.SetPositionY(y);
        public static void SetPositionZ(this GameObject gameObject, float z) => gameObject.transform.SetPositionZ(z);

        public static void SetLocalPositionX(this GameObject gameObject, float x) => gameObject.transform.SetLocalPositionX(x);
        public static void SetLocalPositionY(this GameObject gameObject, float y) => gameObject.transform.SetLocalPositionX(y);
        public static void SetLocalPositionZ(this GameObject gameObject, float z) => gameObject.transform.SetLocalPositionZ(z);

        public static void SetLocalScaleX(this GameObject gameObject, float x) => gameObject.transform.SetLocalScaleX(x);
        public static void SetLocalScaleY(this GameObject gameObject, float y) => gameObject.transform.SetLocalScaleY(y);
        public static void SetLocalScaleZ(this GameObject gameObject, float z) => gameObject.transform.SetLocalScaleZ(z);
       
        public static void SetParent(this GameObject gameObject, GameObject parentObject, bool worldPositionStays = true)
            => gameObject.transform.SetParent(parentObject.transform, worldPositionStays);

        public static void SetParent(this GameObject gameObject, Transform parentTransform, bool worldPositionStays = true)
            => gameObject.transform.SetParent(parentTransform, worldPositionStays);

        public static GameObject[] GetChildGameObjectsAll(this GameObject gameObject)
        {
            List<GameObject> result = new();

            // Stack을 사용해 반복적으로 자식 오브젝트를 탐색
            Stack<Transform> stack = new();
            stack.Push(gameObject.transform);  // 부모 오브젝트부터 시작

            while (stack.TryPop(out Transform current))
            {
                result.Add(current.gameObject);

                // 모든 자식 오브젝트를 스택에 추가
                foreach (Transform child in current)
                {
                    stack.Push(child);
                }
            }

            return result.ToArray();
        }

        public static GameObject[] GetChildGameObjects(this GameObject gameObject)
        {
            GameObject[] gameObjects = new GameObject[gameObject.transform.childCount + 1];
            gameObjects[0] = gameObject;
            
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                gameObjects[i + 1] = gameObject.transform.GetChild(i).gameObject;
            }

            return gameObjects;
        }

        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            if (!gameObject.TryGetComponent(out T component))
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }
    }
}
