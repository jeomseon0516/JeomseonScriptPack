using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

#if UNITY_EDITOR
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("com.jeomseon.ui.horizontal.selector.editor")]
#endif

namespace Jeomseon.UI.Components
{
    [DisallowMultipleComponent]
    public sealed class HorizontalSelector : MonoBehaviour
    {
        [field: SerializeField] public Button LeftButton { get; private set; }
        [field: SerializeField] public Button RightButton { get; private set; }

        // .. 크기는 뷰포트 기준
        [field: SerializeField] public RectTransform Viewport { get; private set; }
        [field: SerializeField] public RectTransform Content { get; private set; }

        [field: SerializeField] public UnityEvent<int> OnChangedValue { get; private set; }

        private int _contentChildCount = 0;
        /// <summary>
        /// .. 값 변경시 OnChangedValue 이벤트가 발생합니다.
        /// OnChangedValue 이벤트 발생을 시키지 않으려면 SetSelectedIndexWithOutNotify 메서드를 사용해주세요.
        /// </summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex == value) return;

                _selectedIndex = Mathf.Clamp(value, 0, Content.childCount - 1);

                _targetPosition = GetTargetPosition(
                    _selectedIndex,
                    Content.childCount,
                    Content.sizeDelta.x,
                    Viewport.sizeDelta.x);

                OnChangedValue.Invoke(_selectedIndex);
            }
        }

        [SerializeField]
        private int _selectedIndex = 0;

        private Vector2 _targetPosition = Vector2.zero;

        private void Start()
        {
            LeftButton.onClick.AddListener(() => SelectedIndex -= 1);
            RightButton.onClick.AddListener(() => SelectedIndex += 1);

            _contentChildCount = Content.childCount;
            InitRectTransforms();
        }

        private void Update()
        {
            if (_contentChildCount != Content.childCount)
            {
                InitRectTransforms();
            }

            _contentChildCount = Content.childCount;
        }

        private void FixedUpdate()
        {
            float distance = Vector2.Distance(Content.localPosition, _targetPosition);
            Vector2 direction = (_targetPosition - (Vector2)Content.localPosition).normalized;

            Content.localPosition += (Vector3)(direction * (distance * 0.5f));
        }

        private void OnRectTransformDimensionsChange()
        {
            InitRectTransforms();
        }

        public void SetSelectedIndexWithOutNotify(int selectedIndex)
        {
            _selectedIndex = Mathf.Clamp(selectedIndex, 0, Content.childCount - 1);

            _targetPosition = GetTargetPosition(
                _selectedIndex,
                Content.childCount,
                Content.sizeDelta.x,
                Viewport.sizeDelta.x);
        }

        internal void InitRectTransforms()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);

            Viewport.pivot = new(0.5f, 0.5f);
            Content.pivot = new(0.5f, 0.5f);

            Viewport.anchorMin = new(0.5f, 0.5f);
            Viewport.anchorMax = new(0.5f, 0.5f);

            Content.anchorMin = new(0.5f, 0.5f);
            Content.anchorMax = new(0.5f, 0.5f);

            Viewport.sizeDelta = ((RectTransform)transform).rect.size;
            Content.sizeDelta = new(Viewport.sizeDelta.x * Content.childCount, Viewport.sizeDelta.y);

            for (int i = 0; i < Content.childCount; i++)
            {
                RectTransform elementTransform = Content.GetChild(i) as RectTransform;
                elementTransform.anchorMin = new(0.5f, 0.5f);
                elementTransform.anchorMax = new(0.5f, 0.5f);

                elementTransform.pivot = new(0, 0.5f);
                elementTransform.sizeDelta = Viewport.sizeDelta;

                elementTransform.localPosition = new(
                    i * Viewport.sizeDelta.x - Content.sizeDelta.x * 0.5f,
                    0f,
                    0f);
            }

            _targetPosition = GetTargetPosition(
                _selectedIndex,
                Content.childCount,
                Content.sizeDelta.x,
                Viewport.sizeDelta.x);

            Content.localPosition = _targetPosition;
        }

        internal Vector3 GetTargetPosition(int selectedIndex, int childCount, float contentSizeX, float viewportSizeX)
            => new(
                -1 * ((float)selectedIndex / childCount * contentSizeX - contentSizeX * 0.5f + viewportSizeX * 0.5f),
                0f,
                0f);
    }
}