using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Jeomseon.UI.Components
{
    public class InitializePotentialDragTrigger : MonoBehaviour, IInitializePotentialDragHandler
    {
        public event UnityAction<PointerEventData> OnInitializePotentialDragEvent
        {
            add => _onInitializePotentialDragEvent.AddListener(value);
            remove => _onInitializePotentialDragEvent.RemoveListener(value);
        }
        
        [SerializeField] private UnityEvent<PointerEventData> _onInitializePotentialDragEvent = new();
        
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            _onInitializePotentialDragEvent.Invoke(eventData);
        }
    }
}
