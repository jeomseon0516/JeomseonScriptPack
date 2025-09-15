using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Jeomseon.UI.Components
{
    public class SubmitTrigger : MonoBehaviour, ISubmitHandler
    {
        public event UnityAction<BaseEventData> OnSubmitEvent
        {
            add => _onSubmitEvent.AddListener(value);
            remove => _onSubmitEvent.RemoveListener(value);
        }
        
        [SerializeField] private UnityEvent<BaseEventData> _onSubmitEvent = new();
        
        public void OnSubmit(BaseEventData eventData)
        {
            _onSubmitEvent.Invoke(eventData);   
        }
    }
}
