using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jeomseon.SafeArea
{
    /// <summary>
    /// RectTransform 기반 Safe Area 자동 적용기.
    /// 
    /// 동작:
    /// - 씬 로드 시, 모든 Canvas를 스캔
    /// - 각 Canvas 아래에 "SafeAreaRoot" GameObject를 자동 생성하고
    ///   기존 자식들을 SafeAreaRoot 밑으로 이동
    /// - SafeAreaRoot GameObject에 SafeAreaRoot 컴포넌트를 자동 부착
    /// 
    /// 결과:
    /// Canvas
    ///  └─ SafeAreaRoot (SafeAreaRoot 컴포넌트)
    ///       └─ 기존 모든 UI
    /// 
    /// 별도 태그/이름/컴포넌트 설정 없이 완전 자동으로 SafeArea 대응.
    /// 
    /// 예외:
    /// - Canvas에 태그 "IgnoreSafeAreaCanvas"가 달려있으면 해당 Canvas는 건드리지 않음.
    /// </summary>
    public static class SafeAreaAutoApplier
    {
        private const string SafeAreaRootName = "SafeAreaRoot";
        private const string IgnoreCanvasTag = "IgnoreSafeAreaCanvas";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyToAllCanvases();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyToAllCanvases();
        }

        /// <summary>
        /// 씬 내 모든 Canvas에 대해 SafeAreaRoot 감싸기 작업을 수행.
        /// </summary>
        public static void ApplyToAllCanvases()
        {
            // Unity 버전별 Canvas 검색
#if UNITY_2023_1_OR_NEWER
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var canvases = Object.FindObjectsOfType<Canvas>(true); // includeInactive = true
#endif

            foreach (var canvas in canvases)
            {
                if (canvas == null)
                    continue;

                // World Space Canvas는 보통 SafeArea 의미가 애매하니, 필요하면 건너뛴다.
                if (canvas.renderMode == RenderMode.WorldSpace)
                    continue;

                // 이 Canvas는 SafeArea 처리하지 않도록 태그로 제외하고 싶을 때
                if (canvas.gameObject.tag == IgnoreCanvasTag)
                    continue;

                WrapCanvasChildrenWithSafeAreaRoot(canvas);
            }
        }

        /// <summary>
        /// 주어진 Canvas의 자식들을 SafeAreaRoot로 감싸고 SafeAreaRoot 컴포넌트를 부착한다.
        /// </summary>
        private static void WrapCanvasChildrenWithSafeAreaRoot(Canvas canvas)
        {
            var canvasTransform = canvas.transform as RectTransform;
            if (canvasTransform == null)
                return;

            // 이미 SafeAreaRoot 컴포넌트가 자식 중 하나에 있으면, 그걸 존중하고 추가 래핑은 하지 않음.
            var existingSafeAreaRoot = canvasTransform.GetComponentInChildren<SafeAreaRoot>(true);
            if (existingSafeAreaRoot != null)
                return;

            // 새 SafeAreaRoot GameObject 생성
            var safeRootGO = new GameObject(SafeAreaRootName);
            var safeRootRect = safeRootGO.AddComponent<RectTransform>();

            // Canvas의 바로 아래 자식으로 세팅
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
                    continue; // 방금 만든 SafeAreaRoot 자신은 건너뜀
                children.Add(child);
            }

            foreach (var child in children)
            {
                child.SetParent(safeRootRect, true);
            }

            // SafeAreaRoot 컴포넌트 부착 (기본: 네 방향 모두 SafeArea 적용)
            safeRootGO.AddComponent<SafeAreaRoot>();
        }
    }
}
