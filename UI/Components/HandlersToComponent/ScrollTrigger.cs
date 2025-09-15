using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ScrollTrigger : MonoBehaviour, IScrollHandler
{
    public event UnityAction<PointerEventData> OnScrollEvent
    {
        add => _onScrollEvent.AddListener(value);
        remove => _onScrollEvent.RemoveListener(value);
    }
    
    [SerializeField] private UnityEvent<PointerEventData> _onScrollEvent = new();
    
    public void OnScroll(PointerEventData eventData)
    {
        _onScrollEvent.Invoke(eventData);
    }
}
