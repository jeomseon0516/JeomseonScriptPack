using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Jeomseon.UI.Components
{
    public class PointerClickTrigger : MonoBehaviour, IPointerClickHandler
    {
        public event UnityAction<PointerEventData> OnPointerClickEvent
        {
            add => _onPointerClickEvent.AddListener(value);
            remove => _onPointerClickEvent.RemoveListener(value);
        }
        
        [SerializeField] private UnityEvent<PointerEventData> _onPointerClickEvent = new();
        
        public void OnPointerClick(PointerEventData eventData)
        {
            _onPointerClickEvent.Invoke(eventData);
        }
    }
}
