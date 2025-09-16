#if SERIALIZEREFERENCEDROPDOWN_INSTALLED
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Jeomseon.Components
{
    [System.Serializable]
    public sealed class MouseRightInputEvent : IMouseInputEvent
    {
        public event UnityAction<Vector2> OnDownEvent
        {
            add => _onDownEvent.AddListener(value);
            remove => _onDownEvent.RemoveListener(value);
        }
        
        public event UnityAction<Vector2> OnUpEvent
        {
            add => _onUpEvent.AddListener(value);
            remove => _onUpEvent.RemoveListener(value);
        }

        [SerializeField] private UnityEvent<Vector2> _onDownEvent = new();
        [SerializeField] private UnityEvent<Vector2> _onUpEvent = new();
        
        private bool _isPrevState = false;
        
        public void Update(MouseInputEvent owner)
        {
            bool nowState = Mouse.current.rightButton.isPressed;
            
            if (_isPrevState != nowState)
            {
                if (nowState)
                {
                    _onDownEvent.Invoke(Mouse.current.position.ReadValue());
                }
                else
                {
                    _onUpEvent.Invoke(Mouse.current.position.ReadValue());
                }
            }

            _isPrevState = nowState;
        }
    }
}
#endif