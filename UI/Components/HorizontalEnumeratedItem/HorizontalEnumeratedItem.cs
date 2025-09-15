using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("com.jeomseon.ui.horizontalenumerateditem.editor")]
#endif

namespace Jeomseon.UI.Components
{
    [DisallowMultipleComponent]
    public sealed class HorizontalEnumeratedItem : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        internal readonly struct ValueLimit
        {
            internal float Min { get; }
            internal float Max { get; }

            internal ValueLimit(float min, float max)
            {
                Min = min;
                Max = max;
            }
        }

        internal static readonly ValueLimit SpacingRatioLimit = new(0f, 1f);
        internal static readonly ValueLimit ItemHeightRatioFromContentHeightLimit = new(0.25f, 1f);
        internal static readonly ValueLimit ItemWidthRatioFromHeightLimit = new(0.5f, 1.5f);
        internal static readonly ValueLimit OnPointerUpCorrectionLimit = new(0.25f, 1f);
        internal static readonly ValueLimit ElasticityLimit = new(0.25f, 1f);

        [field: SerializeField] public RectTransform Viewport { get; private set; }
        [field: SerializeField] public RectTransform Content { get; private set; }

        /// <summary>
        /// .. 선택된 아이템이 변경될시 해당 아이템의 인덱스를 이벤트로 넘겨줍니다
        /// </summary>
        [field: SerializeField] public UnityEvent<int> OnChangedValue { get; private set; }

        /// <summary>
        /// .. 각 아이템은 어느정도의 간격으로 배치될건지의 비율
        /// </summary>
        public float SpacingRatio
        {
            get => _spacingRatio;
            set => _spacingRatio = Mathf.Clamp(
                value,
                SpacingRatioLimit.Min,
                SpacingRatioLimit.Max);
        }

        /// <summary>
        /// .. 아이템의 높이를 Content 높이의 기준의 비율로 정하는 값
        /// </summary>
        public float ItemHeightRatioFromContentHeight
        {
            get => _itemHeightRatioFromContentHeight;
            set => _itemHeightRatioFromContentHeight = Mathf.Clamp(
                value,
                ItemHeightRatioFromContentHeightLimit.Min,
                ItemHeightRatioFromContentHeightLimit.Max);
        }

        /// <summary>
        /// .. 아이템의 넓이를 높이 기준의 비율로 정하는 값
        /// </summary>
        public float ItemWidthRatioFromHeight
        {
            get => _itemWidthRatioFromHeight;
            set => _itemWidthRatioFromHeight = Mathf.Clamp(
                value,
                ItemWidthRatioFromHeightLimit.Min,
                ItemWidthRatioFromHeightLimit.Max);
        }
        /// <summary>
        /// .. 드래그 하다 놓을 시 중앙에 가까운 위치에 있는 아이템으로의 보정 속도
        /// </summary>
        public float OnPointerUpCorrection
        {
            get => _onPointerUpCorrection;
            set => _onPointerUpCorrection = Mathf.Clamp(
                value,
                OnPointerUpCorrectionLimit.Min,
                OnPointerUpCorrectionLimit.Max);
        }

        /// <summary>
        /// .. 뷰포트 내부의 콘텐츠가 뷰포트 범위에서 벗어났을때 원래대로 돌아가려는 힘
        /// </summary>
        public float Elasticity
        {
            get => _elasticity;
            set => _elasticity = Mathf.Clamp(value,
                ElasticityLimit.Min,
                ElasticityLimit.Max);
        }

        /// <summary>
        /// .. 현재 선택된 인덱스를 반환 값 변경시 OnChangedValue 이벤트 발생
        /// 이벤트를 발생시키지 않으려면 SetSelectedIndexWithOutNotify 메서드를 사용해주세요
        /// </summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex == value) return;

                _selectedIndex = Mathf.Clamp(value, 0, Content.childCount - 1);
                _targetPosition = GetContentLocalPositionFromSelectedIndex(_selectedIndex, Content.childCount, Content.sizeDelta.x);
                OnChangedValue.Invoke(_selectedIndex);
            }
        }

        [SerializeField]
        private float _spacingRatio = 1f;
        [SerializeField]
        private float _itemHeightRatioFromContentHeight = 1f;
        [SerializeField]
        private float _itemWidthRatioFromHeight = 1f;
        [SerializeField]
        private float _elasticity = 1f;
        [SerializeField]
        private float _onPointerUpCorrection = 1f;
        [SerializeField]
        private int _selectedIndex = 0;

        private bool _onDragged = false;
        private Vector2 _targetPosition = Vector2.zero;

        private void Start()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);

            Init(Viewport, Content);
        }

        private void FixedUpdate()
        {
            if (!_onDragged && Content.childCount > 0 && !float.IsNaN(_targetPosition.x))
            {
                Vector2 direction = (_targetPosition - (Vector2)Content.localPosition).normalized;
                float distance = Vector2.Distance(_targetPosition, Content.localPosition);

                Content.localPosition += new Vector3((direction * (distance * _onPointerUpCorrection)).x, 0f, 0f);
            }
        }

        private void OnDisable()
        {
            _onDragged = false;
        }

        private void OnRectTransformDimensionsChange()
        {
            Init(Viewport, Content);
            SelectedIndex = 0;
        }

        internal void Init(RectTransform viewport, RectTransform content)
        {
            RectTransform rectTransform = (transform as RectTransform)!;

            viewport.anchorMin = new(0.5f, 0.5f);
            viewport.anchorMax = new(0.5f, 0.5f);
            viewport.pivot = new(0.5f, 0.5f);
            viewport.sizeDelta = rectTransform.rect.size;
            viewport.localPosition = new(0f, 0f, 0f);

            content.anchorMin = new(0.5f, 0.5f);
            content.anchorMax = new(0.5f, 0.5f);
            content.pivot = new(0.5f, 0.5f);
            content.sizeDelta = new(content.sizeDelta.x, viewport.sizeDelta.y);

            float itemHeight = content.sizeDelta.y * _itemHeightRatioFromContentHeight;
            float itemWidth = itemHeight * _itemWidthRatioFromHeight;
            float itemHalfWidth = itemWidth * 0.5f;
            float itemSpacing = itemWidth * _spacingRatio;

            content.sizeDelta = new((itemWidth + itemSpacing) * content.childCount, content.sizeDelta.y);
            float contentHalfWidth = content.sizeDelta.x * 0.5f;

            for (int i = 0; i < content.childCount; i++)
            {
                RectTransform itemRectTransform = (RectTransform)content.GetChild(i);

                float normalizedX = (float)i / content.childCount;

                itemRectTransform.anchorMin = new(0.5f, 0.5f);
                itemRectTransform.anchorMax = new(0.5f, 0.5f);
                itemRectTransform.pivot = new(0.5f, 0.5f);
                itemRectTransform.sizeDelta = new(itemWidth, itemHeight);
                itemRectTransform.localPosition = new(
                    normalizedX * content.sizeDelta.x - contentHalfWidth + itemSpacing + (1 - _spacingRatio) * itemHalfWidth,
                    0f,
                    0f);
            }

            _targetPosition = GetContentLocalPositionFromSelectedIndex(_selectedIndex, Content.childCount, Content.sizeDelta.x);
        }

        internal static Vector3 GetContentLocalPositionFromSelectedIndex(int selectedIndex, int childCount, float contentSizeX)
        {
            float itemSize = contentSizeX / childCount;

            return new(-1f * ((float)selectedIndex / childCount * contentSizeX - contentSizeX * 0.5f) - itemSize * 0.5f, 0f, 0f);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _onDragged = true;
        }
        
        public void OnDrag(PointerEventData pointerEventData)
        {
            if (!_onDragged) return;

            Vector2 contentWorldLeft = Content.TransformPoint(Content.rect.min);
            Vector2 contentWorldRight = Content.TransformPoint(Content.rect.max);
            Vector2 viewportWorldLeft = Viewport.TransformPoint(Viewport.rect.min);
            Vector2 viewportWorldRight = Viewport.TransformPoint(Viewport.rect.max);

            if (contentWorldLeft.x > Viewport.position.x) // .. LEFT
            {
                float viewportHalfWidth = viewportWorldRight.x - Viewport.position.x;
                float distance = viewportWorldRight.x - contentWorldLeft.x;

                float normalizedX = Mathf.Clamp(distance / viewportHalfWidth, 0f, 1f);
                pointerEventData.delta *= normalizedX * _elasticity;
            }

            if (contentWorldRight.x < Viewport.position.x) // .. RIGHT
            {
                float viewportHalfWidth = Viewport.position.x - viewportWorldLeft.x;
                float distance = contentWorldRight.x - viewportWorldLeft.x;

                float normalizedX = Mathf.Clamp(distance / viewportHalfWidth, 0f, 1f);
                pointerEventData.delta *= normalizedX * _elasticity;
            }

            Content.localPosition += new Vector3(pointerEventData.delta.x, 0f, 0f);
        }

        public void OnEndDrag(PointerEventData pointerEventData)
        {
            if (!_onDragged) return;

            _onDragged = false;
            int selectedIndex = -(int)((Content.localPosition.x - Content.sizeDelta.x * 0.5f) / Content.sizeDelta.x * Content.childCount);
            SelectedIndex = selectedIndex;
        }

        public void SetSelectedIndexWithOutNotify(int selectedIndex)
        {
            _selectedIndex = selectedIndex;
            _targetPosition = GetContentLocalPositionFromSelectedIndex(_selectedIndex, Content.childCount, Content.sizeDelta.x);
        }

        public void AddItems(IEnumerable<GameObject> items)
        {
            foreach (GameObject item in items)
            {
                item.transform.SetParent(Content, false);
            }

            Init(Viewport, Content);
        }

        public void AddItem(GameObject item)
        {
            item.transform.SetParent(Content, false);

            Init(Viewport, Content);
        }

        public void ReBuild()
        {
            Init(Viewport, Content);
        }
    }
}
