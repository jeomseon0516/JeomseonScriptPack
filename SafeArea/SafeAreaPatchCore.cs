using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.SafeArea
{
    /// <summary>
    /// Canvas 아래를 SafeAreaRoot로 감싸는 공통 로직.
    /// Runtime / Editor 양쪽에서 재사용.
    /// </summary>
    internal static class SafeAreaPatchCore
    {
        public const string SafeAreaRootName = "SafeAreaRoot";
        public const string IgnoreCanvasTag = "IgnoreSafeAreaCanvas";

        /// <summary>
        /// 주어진 Canvas를 SafeAreaRoot로 감싸고, SafeAreaRoot 컴포넌트를 부착한다.
        /// 이미 패치되어 있으면 그대로 두고 기존 SafeAreaRoot를 반환.
        /// </summary>
        public static GameObject EnsureSafeAreaRoot(Canvas canvas)
        {
            if (canvas == null)
                return null;

            var canvasTransform = canvas.transform as RectTransform;
            if (canvasTransform == null)
                return null;

            // WorldSpace Canvas는 기본적으로 스킵 (필요하면 분기 추가)
            if (canvas.renderMode == RenderMode.WorldSpace)
                return null;

            // 이 Canvas를 SafeArea 적용 대상에서 제외하고 싶을 때: 태그 IgnoreSafeAreaCanvas
            if (canvas.gameObject.tag == IgnoreCanvasTag)
                return null;

            // 이미 SafeAreaRoot 컴포넌트가 자식에 있으면, 그걸 사용
            var existingSafeAreaRoot = canvasTransform.GetComponentInChildren<SafeAreaRoot>(true);
            if (existingSafeAreaRoot != null)
                return existingSafeAreaRoot.gameObject;

            // 새 SafeAreaRoot GameObject 생성
            var safeRootGO = new GameObject(SafeAreaRootName);
            var safeRootRect = safeRootGO.AddComponent<RectTransform>();

            safeRootRect.SetParent(canvasTransform, false);
            safeRootRect.anchorMin = Vector2.zero;
            safeRootRect.anchorMax = Vector2.one;
            safeRootRect.pivot = new Vector2(0.5f, 0.5f);
            safeRootRect.offsetMin = Vector2.zero;
            safeRootRect.offsetMax = Vector2.zero;
            safeRootRect.localScale = Vector3.one;
            safeRootRect.localPosition = Vector3.zero;

            // 기존 Canvas 직속 자식들을 SafeAreaRoot 아래로 이동
            var children = new List<Transform>();
            for (int i = 0; i < canvasTransform.childCount; i++)
            {
                var child = canvasTransform.GetChild(i);
                if (child == safeRootRect.transform)
                    continue;
                children.Add(child);
            }

            foreach (var child in children)
            {
                child.SetParent(safeRootRect, true);
            }

            // SafeAreaRoot 컴포넌트 부착
            safeRootGO.AddComponent<SafeAreaRoot>();

            return safeRootGO;
        }
    }
}
