using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Jeomseon.UI.Components
{
    public class SelectTrigger : MonoBehaviour, ISelectHandler
    {
        public event UnityAction<BaseEventData> OnSelectEvent
        {
            add => _onSelectEvent.AddListener(value);
            remove => _onSelectEvent.RemoveListener(value);
        }

        [SerializeField] private UnityEvent<BaseEventData> _onSelectEvent = new();

        public void OnSelect(BaseEventData eventData)
        {
            _onSelectEvent.Invoke(eventData);
        }
    }
}
