using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Jeomseon.UI.Components
{
    public class PointerExitTrigger : MonoBehaviour, IPointerExitHandler
    {
        public event UnityAction<PointerEventData> OnPointerExitEvent
        {
            add => _onPointerExitEvent.AddListener(value);
            remove => _onPointerExitEvent.RemoveListener(value);
        }

        [SerializeField] private UnityEvent<PointerEventData> _onPointerExitEvent = new();

        public void OnPointerExit(PointerEventData eventData)
        {
            _onPointerExitEvent.Invoke(eventData);
        }
    }
}
