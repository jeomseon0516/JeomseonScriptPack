using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Jeomseon.UI.Components
{
    public class PointerUpTrigger : MonoBehaviour, IPointerUpHandler
    {
        public event UnityAction<PointerEventData> OnPointerUpEvent
        {
            add => _onPointerUpEvent.AddListener(value);
            remove => _onPointerUpEvent.RemoveListener(value);
        }
        
        [SerializeField] private UnityEvent<PointerEventData> _onPointerUpEvent = new();
        
        public void OnPointerUp(PointerEventData eventData)
        {
            _onPointerUpEvent.Invoke(eventData);
        }
    }
}