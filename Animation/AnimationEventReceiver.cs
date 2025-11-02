using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;

namespace Jeomseon.Animation
{
    [DisallowMultipleComponent]
    public class AnimationEventReceiver : MonoBehaviour
    {
        [SerializeField] private UnityEvent<AnimationEvent> _animationEventReceived = new();

        private bool _isInitialized = false;
        private readonly Dictionary<string, UnityAction<AnimationEvent>> _routes = new();

        public event UnityAction<AnimationEvent> AnimationEventReceived
        {
            add => _animationEventReceived.AddListener(value);
            remove => _animationEventReceived.RemoveListener(value);
        }

        public void ReceiveAnimationEvent(AnimationEvent evt)
        {
            _animationEventReceived.Invoke(evt);
        }

        public void Register(string key, UnityAction<AnimationEvent> handler)
        {
            initialize();
            _routes[key] = handler;
        }
        public void Unregister(string key) => _routes.Remove(key);

        private void Awake()
        {
            initialize();
        }

        private void initialize()
        {
            if (_isInitialized) return;

            AnimationEventReceived += evt =>
            {
                if (!string.IsNullOrEmpty(evt.stringParameter) && _routes.TryGetValue(evt.stringParameter, out var h))
                    h?.Invoke(evt);
            };

            _isInitialized = true;
        }
    }
}
