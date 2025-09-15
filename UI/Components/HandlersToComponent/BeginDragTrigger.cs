using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Jeomseon.UI.Components
{
    public class BeginDragTrigger : MonoBehaviour, IBeginDragHandler
    {
        public event UnityAction<PointerEventData> OnBeginDragEvent
        {
            add => _onBeginDragEvent.AddListener(value);
            remove => _onBeginDragEvent.RemoveListener(value);
        }
        
        [SerializeField] private UnityEvent<PointerEventData> _onBeginDragEvent = new();        
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            _onBeginDragEvent.Invoke(eventData);
        }
    }
}