using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Jeomseon.UI.Components
{
    public class DropTrigger : MonoBehaviour, IDropHandler
    {
        public event UnityAction<PointerEventData> OnDropEvent
        {
            add => _onDropEvent.AddListener(value);
            remove => _onDropEvent.RemoveListener(value);
        }
        
        [SerializeField] private UnityEvent<PointerEventData> _onDropEvent = new();
        
        public void OnDrop(PointerEventData eventData)
        {
            _onDropEvent.Invoke(eventData);
        }
    }
}