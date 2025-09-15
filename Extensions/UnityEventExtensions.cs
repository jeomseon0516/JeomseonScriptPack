using System;
using UnityEngine;
using UnityEngine.Events;

namespace Jeomseon.Extensions
{
    public static class UnityEventExtensions
    {
        public static void SetPersistentListenerState(this UnityEventBase unityEvent, UnityEventCallState eventState)
        {
            for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
            {
                unityEvent.SetPersistentListenerState(i, eventState);
            }
        }
    }
}
