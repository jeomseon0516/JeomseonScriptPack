using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Jeomseon.UI.Components
{
    public class CancelTrigger : MonoBehaviour, ICancelHandler
    {
        public event UnityAction<BaseEventData> OnCancelEvent
        {
            add => _onCancelEvent.AddListener(value);
            remove => _onCancelEvent.RemoveListener(value);
        }
        
        [SerializeField] private UnityEvent<BaseEventData> _onCancelEvent = new();
        
        public void OnCancel(BaseEventData eventData)
        {
            _onCancelEvent.Invoke(eventData);
        }
    }
}
