using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Jeomseon.Extensions;

#if UNITY_EDITOR
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("com.jeomseon.ui.rangeadjustmentslider.editor")]
#endif

namespace Jeomseon.UI.Components
{
    using static UIHelper;

    [DisallowMultipleComponent]
    public sealed class RangeAdjustmentSlider : MonoBehaviour
    {
        [field: SerializeField] public Image LeftHandle { get; set; } = null;
        [field: SerializeField] public Image RightHandle { get; set; } = null;

        [field: SerializeField] public Image BackgroundBar { get; set; } = null;
        [field: SerializeField] public Image FrontBar { get; set; } = null;

        [field: SerializeField] public int DivideValue { get; set; } = 100;
        [field: SerializeField] public bool IsDivide { get; set; } = false;

        [field: SerializeField] public float HandleSizeRatio { get; set; } = 2.5f;
        [field: SerializeField] public Camera TargetCamera { get; set; } = null;

        [field: SerializeField] public UnityEvent<float> OnChangedLeftValue { get; private set; }
        [field: SerializeField] public UnityEvent<float> OnChangedRightValue { get; private set; }

        [field: SerializeField] public UnityEvent<int> OnChangedLeftIntValue { get; private set; }
        [field: SerializeField] public UnityEvent<int> OnChangedRightIntValue { get; private set; }

        public float LeftValue
        {
            get => _leftValue;
            set => _leftValue = Mathf.Clamp(value, 0f, _rightValue);
        }

        public float RightValue
        {
            get => _rightValue;
            set => _rightValue = Mathf.Clamp(value, _leftValue, 1f);
        }

        public int LeftIntValue
        {
            get => _leftIntValue;
            set => _leftIntValue = Mathf.Clamp(value, 0, _rightIntValue - 1);
        }

        public int RightIntValue
        {
            get => _rightIntValue;
            set => _rightIntValue = Mathf.Clamp(value, _leftIntValue + 1, DivideValue);
        }

        public float HandleSize => BackgroundBar ? BackgroundBar.rectTransform.rect.height * HandleSizeRatio : 0.0f;

        [SerializeField]
        private float _leftValue = 0f;
        [SerializeField]
        private float _rightValue = 1f;

        [SerializeField]
        private int _leftIntValue = 0;
        [SerializeField]
        private int _rightIntValue = 1;

        private Image _selectedHandle = null;
        private bool _isLeft = false;

        private void Awake()
        {
            if (!TargetCamera)
            {
                TargetCamera = GetComponentInParent<Camera>();
            }
        }

        private IEnumerator Start()
        {
            yield return null;
            Init();
        }

        private void Update()
        {
            if (Mathf.Abs(LeftHandle.rectTransform.localPosition.x) > Mathf.Abs(RightHandle.rectTransform.localPosition.x))
            {
                checkIntersectHandle(LeftHandle, true);
                checkIntersectHandle(RightHandle, false);
                RightHandle.rectTransform.SetAsLastSibling();
            }
            else
            {
                checkIntersectHandle(RightHandle, false);
                checkIntersectHandle(LeftHandle, true);
                LeftHandle.rectTransform.SetAsLastSibling();
            }

            if (_selectedHandle)
            {
                Vector2 mouseLocalPosition = BackgroundBar.rectTransform.GetScreenToLocalPoint(TargetCamera, Input.mousePosition);
                float normalizedX = (mouseLocalPosition.x + BackgroundBar.rectTransform.rect.width * 0.5f) / BackgroundBar.rectTransform.rect.width;

                if (IsDivide)
                {
                    if (_isLeft)
                    {
                        LeftIntValue = (int)(normalizedX * DivideValue);
                    }
                    else
                    {
                        RightIntValue = (int)(normalizedX * DivideValue);
                    }
                }
                else
                {
                    if (_isLeft)
                    {
                        LeftValue = normalizedX;
                    }
                    else
                    {
                        RightValue = normalizedX;
                    }
                }

                if (Input.GetMouseButtonUp(0))
                {
                    _selectedHandle = null;
                }
            }

            if (IsDivide)
            {
                setHandleLocalPositionAndNotify(LeftHandle, GetIntValueToLocalPosition(_leftIntValue), () => OnChangedLeftIntValue.Invoke(_leftIntValue));
                setHandleLocalPositionAndNotify(RightHandle, GetIntValueToLocalPosition(_rightIntValue), () => OnChangedRightIntValue.Invoke(_rightIntValue));
            }
            else
            {
                setHandleLocalPositionAndNotify(LeftHandle, GetValueToLocalPosition(_leftValue), () => OnChangedLeftValue.Invoke(_leftValue));
                setHandleLocalPositionAndNotify(RightHandle, GetValueToLocalPosition(_rightValue), () => OnChangedRightValue.Invoke(_rightValue));
            }

            SetFrontBarSizeAndLocalPosition();
        }

        private void setHandleLocalPositionAndNotify(Image handle, Vector2 newHandlePosition, System.Action onChangedValue)
        {
            if (handle.rectTransform.localPosition.x == newHandlePosition.x) return;

            handle.rectTransform.localPosition = newHandlePosition;
            onChangedValue.Invoke();
        }

        private void checkIntersectHandle(Image handle, bool isLeft)
        {
            if (!Input.GetMouseButtonDown(0) || !handle.rectTransform.CheckInClickPointer(TargetCamera, Input.mousePosition)) return;

            _isLeft = isLeft;
            _selectedHandle = handle;
        }

        internal void Init()
        {
            BackgroundBar.rectTransform.sizeDelta = (transform as RectTransform).rect.size;

            Vector2 handleSize = new Vector2(BackgroundBar.rectTransform.rect.height, BackgroundBar.rectTransform.rect.height) * HandleSizeRatio;
            LeftHandle.rectTransform.sizeDelta = handleSize;
            RightHandle.rectTransform.sizeDelta = handleSize;

            SetFrontBarSizeAndLocalPosition();
        }

        internal void SetFrontBarSizeAndLocalPosition()
        {
            float frontBarWidth = RightHandle.rectTransform.localPosition.x - LeftHandle.rectTransform.localPosition.x;
            FrontBar.rectTransform.sizeDelta = new(frontBarWidth, BackgroundBar.rectTransform.sizeDelta.y);
            FrontBar.rectTransform.localPosition = new(LeftHandle.rectTransform.localPosition.x + frontBarWidth * 0.5f, 0.0f);
        }

        internal Vector2 GetValueToLocalPosition(float value)
            => new(value * BackgroundBar.rectTransform.rect.width - BackgroundBar.rectTransform.rect.width * 0.5f, 0.0f);

        internal Vector2 GetIntValueToLocalPosition(int intValue)
            => new(intValue / (float)DivideValue * BackgroundBar.rectTransform.rect.width
                - BackgroundBar.rectTransform.rect.width * 0.5f, 0.5f);
    }
}
