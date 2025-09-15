using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UpdateSelectedTrigger : MonoBehaviour, IUpdateSelectedHandler
{
    public event UnityAction<BaseEventData> OnUpdateSelectedEvent
    {
        add => _onUpdateSelectedEvent.AddListener(value);
        remove => _onUpdateSelectedEvent.RemoveListener(value);
    }
    
    [SerializeField] private UnityEvent<BaseEventData> _onUpdateSelectedEvent = new();
    
    public void OnUpdateSelected(BaseEventData eventData)
    {
        _onUpdateSelectedEvent.Invoke(eventData);
    }
}
