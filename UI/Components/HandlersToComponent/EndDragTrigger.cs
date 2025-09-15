using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Jeomseon.UI.Components
{
    public class EndDragTrigger : MonoBehaviour, IEndDragHandler
    {
        public event UnityAction<PointerEventData> OnEndDragEvent
        {
            add => _onEndDragEvent.AddListener(value);
            remove => _onEndDragEvent.RemoveListener(value);
        }
        
        [SerializeField] private UnityEvent<PointerEventData> _onEndDragEvent = new();
        
        public void OnEndDrag(PointerEventData eventData)
        {
            _onEndDragEvent.Invoke(eventData);
        }
    }
}
