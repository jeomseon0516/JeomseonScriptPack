using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Jeomseon.UI.Components
{
    public class DeselectTrigger : MonoBehaviour, IDeselectHandler
    {
        public event UnityAction<BaseEventData> OnDeselectEvent
        {
            add => _onDeselectEvent.AddListener(value);
            remove => _onDeselectEvent.RemoveListener(value);
        }
        
        [SerializeField] private UnityEvent<BaseEventData> _onDeselectEvent = new();
        
        public void OnDeselect(BaseEventData eventData)
        {
            _onDeselectEvent.Invoke(eventData);
        }
    }
}
