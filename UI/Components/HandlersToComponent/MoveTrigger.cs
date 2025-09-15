using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Jeomseon.UI.Components
{
    public class MoveTrigger : MonoBehaviour, IMoveHandler
    {
        public event UnityAction<AxisEventData> OnMoveEvent
        {
            add => _onMoveEvent.AddListener(value);
            remove => _onMoveEvent.RemoveListener(value);
        }
        
        [SerializeField] private UnityEvent<AxisEventData> _onMoveEvent = new();

        public void OnMove(AxisEventData eventData)
        {
            _onMoveEvent.Invoke(eventData);
        }
    }
}