using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Jeomseon.Singleton;

namespace Jeomseon.UI
{
    public sealed class ResolutionObserver : Singleton<ResolutionObserver>
    {
        [Header("Resolution Changed Event")]
        [SerializeField]
        private UnityEvent<Vector2> _onChangedResolution;

        private Vector2Int _lastResolution = Vector2Int.zero;

        protected override void Init()
        {
            _onChangedResolution ??= new();
        }

        private void Update()
        {
            if (_lastResolution.x != Screen.width || _lastResolution.y != Screen.height)
            {
                _onChangedResolution.Invoke(new(Screen.width, Screen.height));
            }

            _lastResolution = new(Screen.width, Screen.height);
        }

        public void AddListenerOnChangedResolution(UnityAction<Vector2> callback)
            => _onChangedResolution.AddListener(callback);

        public void RemoveListenerOnResolutionChanged(UnityAction<Vector2> callback)
            => _onChangedResolution.RemoveListener(callback);
    }
}
