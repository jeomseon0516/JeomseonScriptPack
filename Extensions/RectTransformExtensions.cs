using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Jeomseon.Extensions
{
    public static class RectTransformExtensions
    {
        private static readonly Vector3[] _corners = new Vector3[4] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };

        public static Vector2 ToScreenSize(this RectTransform rectTransform)
            => rectTransform.rect.size * rectTransform.lossyScale;

        public static bool CheckInClickPointer(this RectTransform rectTransform, Camera camera, in Vector2 mousePosition)
            => rectTransform.rect.Contains(GetScreenToLocalPoint(rectTransform, camera, mousePosition));

        public static Vector2 GetScreenToLocalPoint(this RectTransform rectTransform, Camera camera, in Vector2 screenPoint)
        {
            Vector2 worldPosition = camera.ScreenToWorldPoint(screenPoint);
            Vector2 localPosition = rectTransform.InverseTransformPoint(worldPosition);

            return localPosition;
        }

        public static Vector2 GetWorldRectSize(this RectTransform rectTransform)
        {
            Vector3[] corners = rectTransform.GetWorldCorners();
            Vector2 worldSize = new(Vector2.Distance(corners[0], corners[3]),
                                    Vector2.Distance(corners[0], corners[1]));

            return worldSize;
        }

        public static float GetWorldWidth(this RectTransform rectTransform)
        {
            Vector3[] corners = rectTransform.GetWorldCorners();
            float worldWidth = Vector2.Distance(corners[0], corners[3]);

            return worldWidth;
        }

        public static float GetWorldHeight(this RectTransform rectTransform)
        {
            Vector3[] corners = rectTransform.GetWorldCorners();
            float worldHeight = Vector2.Distance(corners[0], corners[1]);
            
            return worldHeight;
        }

        public static float GetScreenWidth(this RectTransform rectTransform, Camera camera)
        {
            Vector3[] corners = rectTransform.GetWorldCorners();

            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 bottomRight = RectTransformUtility.WorldToScreenPoint(camera, corners[3]);

            float screenWidth = Mathf.Abs(bottomLeft.x - bottomRight.x);
            return screenWidth;
        }

        public static float GetScreenHeight(this RectTransform rectTransform, Camera camera)
        {
            Vector3[] corners = rectTransform.GetWorldCorners();

            Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[1]);
            Vector2 bottomRight = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);

            float screenHeight = Mathf.Abs(topLeft.y - bottomRight.y);
            return screenHeight;
        }

        public static Vector2 GetWorldToScreenSize(this RectTransform rectTransform, Camera camera)
        {
            Vector3[] corners = rectTransform.GetWorldCorners();

            Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[1]);
            Vector2 bottomRight = RectTransformUtility.WorldToScreenPoint(camera, corners[3]);

            return new(Mathf.Abs(bottomRight.x - topLeft.x),
                       Mathf.Abs(topLeft.y - bottomRight.y));
        }

        public static float GetInverseLocalWidth(this RectTransform rectTransform, Transform target)
        {
            Vector3[] worldCorners = rectTransform.GetWorldCorners();
            Vector2 localTopLeft = rectTransform.parent.InverseTransformPoint(worldCorners[1]);
            Vector2 localBottomRight = rectTransform.parent.InverseTransformPoint(worldCorners[3]);
            
            return Mathf.Abs(localBottomRight.x - localTopLeft.x);
        }

        public static float GetParentSpaceWidth(this RectTransform rectTransform)
        {
            return rectTransform.GetParentSpaceXMax() - rectTransform.GetParentSpaceXMin();
        }

        public static float GetParentSpaceHeight(this RectTransform rectTransform)
        {
            return rectTransform.GetParentSpaceYMax() - rectTransform.GetParentSpaceYMin();
        }

        public static Vector2 GetParentSpaceSize(this RectTransform rectTransform)
        {
            return new(
                rectTransform.GetParentSpaceWidth(),
                rectTransform.GetParentSpaceHeight());
        }
        
        public static Vector2 GetParentRelativeSize(this RectTransform rectTransform)
        {
            // 부모 RectTransform 가져오기
            RectTransform parentRectTransform = rectTransform.parent as RectTransform;

            return parentRectTransform == null ?
                rectTransform.rect.size : 
                new(rectTransform.rect.width * parentRectTransform.localScale.x, 
                    rectTransform.rect.height * parentRectTransform.localScale.y);
        }

        public static float GetParentSpaceXMin(this RectTransform rectTransform)
        {
            RectTransform parentRectTransform = rectTransform.parent as RectTransform;

            return !parentRectTransform ?
                rectTransform.rect.xMin :
                rectTransform.anchoredPosition.x
                    + rectTransform.pivot.x * rectTransform.rect.width
                    - rectTransform.anchorMin.x * parentRectTransform.rect.size.x;
        }

        public static float GetParentSpaceXMax(this RectTransform rectTransform)
        {
            RectTransform parentRectTransform = rectTransform.parent as RectTransform;

            return !parentRectTransform ?
                rectTransform.rect.xMax :
                rectTransform.anchoredPosition.x
                    + (1 - rectTransform.pivot.x) * rectTransform.rect.width
                    + rectTransform.anchorMin.x * parentRectTransform.rect.size.x;
        }
        
        public static float GetParentSpaceYMin(this RectTransform rectTransform)
        {
            RectTransform parentRectTransform = rectTransform.parent as RectTransform;

            return !parentRectTransform ?
                rectTransform.rect.yMin :
                rectTransform.anchoredPosition.y
                    + rectTransform.pivot.y * rectTransform.rect.height
                    - rectTransform.anchorMin.y * parentRectTransform.rect.size.y;
        }

        public static float GetParentSpaceYMax(this RectTransform rectTransform)
        {
            RectTransform parentRectTransform = rectTransform.parent as RectTransform;

            return !parentRectTransform ?
                rectTransform.rect.yMax :
                rectTransform.anchoredPosition.y
                    + (1 - rectTransform.pivot.y) * rectTransform.rect.height
                    + rectTransform.anchorMin.y * parentRectTransform.rect.size.y;
        }
        
        public static float GetParentRelativeWidth(this RectTransform rectTransform)
        {
            // 부모 RectTransform 가져오기
            RectTransform parentRectTransform = rectTransform.parent as RectTransform;
            
            return parentRectTransform == null ? 
                rectTransform.rect.width : 
                rectTransform.rect.width * parentRectTransform.localScale.x;
        }

        public static float GetParentRelativeHeight(this RectTransform rectTransform)
        {
            RectTransform parentRectTransform = rectTransform.parent as RectTransform;

            return parentRectTransform == null ?
                rectTransform.rect.height :
                rectTransform.rect.height * parentRectTransform.localScale.y;
        }
        
        public static float GetInverseLocalHeight(this RectTransform rectTransform, Transform target)
        {
            Vector3[] worldCorners = rectTransform.GetWorldCorners();
            Vector2 localTopLeft = rectTransform.parent.InverseTransformPoint(worldCorners[1]);
            Vector2 localBottomRight = rectTransform.parent.InverseTransformPoint(worldCorners[3]);
            
            return Mathf.Abs(localBottomRight.y - localTopLeft.y);
        }

        public static Vector2 GetInverseLocalSize(this RectTransform rectTransform, Transform target)
        {
            Vector3[] worldCorners = rectTransform.GetWorldCorners();
            Vector2 localTopLeft = rectTransform.parent.InverseTransformPoint(worldCorners[1]);
            Vector2 localBottomRight = rectTransform.parent.InverseTransformPoint(worldCorners[3]);

            return new(
                Mathf.Abs(localBottomRight.x - localTopLeft.x),
                Mathf.Abs(localTopLeft.y - localBottomRight.y));
        }
        
        public static Vector3[] GetWorldCorners(this RectTransform rectTransform)
        {
            rectTransform.GetWorldCorners(_corners);
            return _corners;
        }

        public static float GetChildWidthSum(this RectTransform rectTransform)
        {
            return rectTransform.Cast<RectTransform>().Sum(child => child.rect.width);
        }

        public static float GetChildHeightSum(this RectTransform rectTransform)
        {
            return rectTransform.Cast<RectTransform>().Sum(child => child.rect.height);
        }

        public static Vector2 GetChildSizeSum(this RectTransform rectTransform)
        {
            return new(rectTransform.GetChildWidthSum(), rectTransform.GetChildHeightSum());
        }
        
        /// <summary>
        /// .. 어떤 UI 컨텐츠가 있을때 내부의 요소들을 가로로 일렬로 정렬 시킵니다 요소들이 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="elements"></param>
        /// <returns></returns>
        public static Vector3[] GetHorizontalSortedPosition(this RectTransform parent)
        {
            Vector3[] positionArray = new Vector3[parent.childCount];
            Vector2 parentPivotSize = new(parent.rect.width * parent.pivot.x, parent.rect.height * parent.pivot.y);
            float nowXPosition = 0;

            for (int i = 0; i < positionArray.Length; i++)
            {
                RectTransform child = parent.GetChild(i).transform as RectTransform;
                
                if (child)
                {
                    Vector2 childPivotSize = new(child.rect.width * child.pivot.x, child.rect.height * child.pivot.y);
                    positionArray[i] = new(
                        nowXPosition - parentPivotSize.x + childPivotSize.x, 
                        -parentPivotSize.y + parent.rect.height * 0.5f - child.rect.height * (child.pivot.y - 0.5f), 
                        0f);
                    nowXPosition += child.rect.width;
                }
            }

            return positionArray;
        }

        public static Vector3[] GetVerticalSortedPosition(this RectTransform parent)
        {
            Vector3[] positionArray = new Vector3[parent.childCount];
            Vector2 parentPivotSize = new(parent.rect.width * parent.pivot.x, parent.rect.height * parent.pivot.y);
            float nowYPosition = 0;

            for (int i = 0; i < positionArray.Length; i++)
            {
                RectTransform child = parent.GetChild(i).transform as RectTransform;
                
                if (child)
                {
                    Vector2 childPivotSize = new(child.rect.width * child.pivot.x, child.rect.height * child.pivot.y);
                    positionArray[i] = new(
                        -parentPivotSize.x + parent.rect.width * 0.5f - child.rect.width * (child.pivot.x - 0.5f), 
                        nowYPosition - parentPivotSize.y + childPivotSize.y, 
                        0f);
                    nowYPosition += child.rect.height;
                }
            }

            return positionArray;
        }
    }
}
