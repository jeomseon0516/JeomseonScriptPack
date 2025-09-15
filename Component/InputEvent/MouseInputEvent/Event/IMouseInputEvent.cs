using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Jeomseon.Components
{
    public interface IMouseInputEvent
    {
        event UnityAction<Vector2> OnDownEvent;
        event UnityAction<Vector2> OnUpEvent;
        
        void Update(MouseInputEvent owner);
    }
}
