using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Jeomseon.UI.Components
{
    public class DragTrigger : MonoBehaviour, IDragHandler
    {
        public event UnityAction<PointerEventData> OnDragEvent
        {
            add => _onDragEvent.AddListener(value);
            remove => _onDragEvent.RemoveListener(value);
        }
        
        [SerializeField] private UnityEvent<PointerEventData> _onDragEvent = new();
        
        public void OnDrag(PointerEventData eventData)
        {
            _onDragEvent.Invoke(eventData);
        }
    }
}
