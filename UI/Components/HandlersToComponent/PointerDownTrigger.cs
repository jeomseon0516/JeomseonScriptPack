using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Jeomseon.UI.Components
{
    public class PointerDownTrigger : MonoBehaviour, IPointerDownHandler
    {
        public event UnityAction<PointerEventData> OnPointerDownEvent
        {
            add => _onPointerDownEvent.AddListener(value);
            remove => _onPointerDownEvent.RemoveListener(value);
        }
        
        [SerializeField] private UnityEvent<PointerEventData> _onPointerDownEvent = new();

        public void OnPointerDown(PointerEventData eventData)
        {
            _onPointerDownEvent.Invoke(eventData);
        }
    }
}
