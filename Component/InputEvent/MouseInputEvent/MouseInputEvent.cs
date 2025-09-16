#if SERIALIZEREFERENCEDROPDOWN_INSTALLED
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Jeomseon.Components
{
    public sealed class MouseInputEvent : MonoBehaviour
    {
        [SerializeReference, SerializeReferenceDropdown]
        private List<IMouseInputEvent> _events = new();

        public void AddListenerDownEvent<TEvent>(UnityAction<Vector2> onDownEvent) where TEvent : IMouseInputEvent, new()
        {
            int index = _events.FindIndex(e => e is TEvent);
            bool isContains = index > 0;
            
            TEvent current = isContains ? (TEvent)_events[index] : new();
            
            if (!isContains)
            {
                _events.Add(current);
            }
            
            current.OnDownEvent += onDownEvent;
        }

        public void RemoveListenerDownEvent<TEvent>(UnityAction<Vector2> onDownEvent) where TEvent : IMouseInputEvent
        {
            int index = _events.FindIndex(e => e is TEvent);

            if (index > -1)
            {
                _events[index].OnDownEvent -= onDownEvent;
            }
        }

        public void AddListenerUpEvent<TEvent>(UnityAction<Vector2> onUpEvent) where TEvent : IMouseInputEvent, new()
        {
            int index = _events.FindIndex(e => e is TEvent);
            bool isContains = index > 0;

            TEvent current = isContains ? (TEvent)_events[index] : new();

            if (!isContains)
            {
                _events.Add(current);
            }

            current.OnUpEvent += onUpEvent;
        }

        public void RemoveListenerUpEvent<TEvent>(UnityAction<Vector2> onUpEvent) where TEvent : IMouseInputEvent
        {
            int index = _events.FindIndex(e => e is TEvent);

            if (index > -1)
            {
                _events[index].OnUpEvent -= onUpEvent;
            }
        }
        
        private void Update()
        {
            _events.ForEach(inputEvent => inputEvent.Update(this));
        }
    }
}
#endif