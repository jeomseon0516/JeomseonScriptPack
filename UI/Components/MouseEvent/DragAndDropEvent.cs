using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Jeomseon.Extensions;

namespace Jeomseon.UI.Components
{
    using static UIHelper;

    [DisallowMultipleComponent]
    public sealed class DragAndDropEvent : MonoBehaviour, IPointerDownHandler
    {
        [field: SerializeField] public Canvas Canvas { get; set; }
        [field: SerializeField, Range(0f, 1f)] public float HoldTime { get; set; } = 0.5f;
        [field: SerializeField, Range(0.1f, 1f)] public float DraggedAlpha { get; set; } = 0.3f;

        [field: SerializeField] public UnityEvent<RectTransform, Camera, Vector2> OnDropEvent { get; set; }

        [Header("Dragging Copy Object"), Tooltip("Images에 들어간 이미지들은 순서에 맞게 렌더됩니다")]
        [SerializeField]
        private List<Image> _images;
        [SerializeField]
        private Image _rootImage;

        private RectTransform _rectTransform = null;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
        }

        private void Start()
        {
            if (!Canvas)
            {
                Canvas = GetComponentInParent<Canvas>();
#if DEBUG
                Debug.LogWarning($"{typeof(DragAndDropEvent).Name} : Canvas가 설정되어 있지 않기 때문에 자동으로 Canvas를 찾아 설정합니다.");
#endif
            }
        }

        private IEnumerator iEMoveKeepBox(PointerEventData eventData)
        {
            float time = 0.0f;
            while (Input.GetMouseButton(0) && time < HoldTime)
            {
                yield return null;
                time += Time.deltaTime;

                if (!_rectTransform.CheckInClickPointer(eventData.pressEventCamera, Input.mousePosition))
                {
                    yield break;
                }
            }

            Camera camera = eventData.pressEventCamera;
            eventData.pointerDrag = null;

            GameObject dragObject = new("DraggedObject");
            dragObject.transform.SetParent(Canvas.transform);
            dragObject.transform.localScale = new Vector3(1f, 1f, 1f);

            RectTransform dragRect = dragObject.AddComponent<RectTransform>();
            dragRect.sizeDelta = _rectTransform.rect.size;

            Image dragImage = dragObject.AddComponent<Image>();
            dragImage.sprite = _rootImage.sprite;
            dragImage.enabled = _rootImage.enabled;
            dragImage.preserveAspect = _rootImage.preserveAspect;
            dragImage.color = new(255f, 255f, 255f, DraggedAlpha);

            checkMaskComponent(_rootImage, dragObject);

            for (int i = 0; i < _images.Count; i++)
            {
                GameObject childObject = new($"child_{i + 1}");
                childObject.transform.SetParent(dragObject.transform);
                childObject.transform.localScale = new Vector3(1f, 1f, 1f);

                RectTransform childRectTransform = childObject.AddComponent<RectTransform>();
                childRectTransform.sizeDelta = _images[i].rectTransform.rect.size;
                childRectTransform.transform.localPosition = _rootImage.rectTransform.InverseTransformPoint(_images[i].rectTransform.position);

                Image childImage = childObject.AddComponent<Image>();
                childImage.sprite = _images[i].sprite;
                childImage.enabled = _images[i].enabled;
                childImage.preserveAspect = _images[i].preserveAspect;
                childImage.color = new(255f, 255f, 255f, DraggedAlpha);

                checkMaskComponent(_images[i], childObject);
            }

            dragRect.SetAsLastSibling();

            while (!Input.GetMouseButtonUp(0))
            {
                Vector2 worldPosition = camera.ScreenToWorldPoint(Input.mousePosition);
                dragRect.transform.position = worldPosition;
                yield return null;
            }

            OnDropEvent?.Invoke(dragRect, camera, Input.mousePosition);
            Destroy(dragRect.gameObject);
        }

        private void checkMaskComponent(Image image, GameObject childObject)
        {
            if (!image.TryGetComponent(out Mask mask)) return;

            Mask childMask = childObject.AddComponent<Mask>();
            childMask.showMaskGraphic = mask.showMaskGraphic;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            _ = StartCoroutine(iEMoveKeepBox(eventData));
        }
    }
}