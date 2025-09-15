using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Jeomseon.Singleton;

namespace Jeomseon.Components
{
    public sealed class MouseMovementTracker : Singleton<MouseMovementTracker>
    {
        private const float MOVEMENT_THRESHOLD_MIN = 0f;
        private const float MOVEMENT_THRESHOLD_MAX = 0.25f;

        [field: SerializeField, Range(MOVEMENT_THRESHOLD_MIN, MOVEMENT_THRESHOLD_MAX)] public float MovementThreshold = 0f;

        public event UnityAction<Vector2> OnMoveBeginMouseForNowScene
        {
            add => _onMoveBeginMouseForNowScene.AddListener(value); 
            remove => _onMoveBeginMouseForNowScene.AddListener(value);
        } 
        
        public event UnityAction<Vector2> OnMoveEndMouseForNowScene
        {
            add => _onMoveEndMouseForNowScene.AddListener(value); 
            remove => _onMoveEndMouseForNowScene.AddListener(value);
        } 
        
        public event UnityAction<Vector2> OnHoverMouseForNowScene
        {
            add => _onHoverMouseForNowScene.AddListener(value); 
            remove => _onHoverMouseForNowScene.AddListener(value);
        } 
        
        public event UnityAction<Vector2> OnUpdateMouseForNowScene
        {
            add => _onUpdateMouseForNowScene.AddListener(value);
            remove => _onUpdateMouseForNowScene.RemoveListener(value);
        }
        
        public event UnityAction<Vector2> OnMoveBeginMouseForRuntime
        {
            add => _onMoveBeginMouseForRuntime.AddListener(value);
            remove => _onMoveBeginMouseForRuntime.RemoveListener(value);
        }

        public event UnityAction<Vector2> OnMoveEndMouseRuntime
        {
            add => _onMoveEndMouseRuntime.AddListener(value);
            remove => _onMoveEndMouseRuntime.RemoveListener(value);
        }

        public event UnityAction<Vector2> OnHoverMouseRuntime
        {
            add => _onHoverMouseRuntime.AddListener(value);
            remove => _onHoverMouseRuntime.RemoveListener(value);
        }
        
        public event UnityAction<Vector2> OnUpdateMouseRuntime
        {
            add => _onUpdateMouseRuntime.AddListener(value);
            remove => _onUpdateMouseRuntime.RemoveListener(value);
        }

        [field: SerializeField] private UnityEvent<Vector2> _onMoveBeginMouseForNowScene = new();
        [field: SerializeField] private UnityEvent<Vector2> _onMoveEndMouseForNowScene = new();
        [field: SerializeField] private UnityEvent<Vector2> _onHoverMouseForNowScene = new();
        [field: SerializeField] private UnityEvent<Vector2> _onUpdateMouseForNowScene = new();
        
        [field: SerializeField] private UnityEvent<Vector2> _onMoveBeginMouseForRuntime = new();
        [field: SerializeField] private UnityEvent<Vector2> _onMoveEndMouseRuntime = new();
        [field: SerializeField] private UnityEvent<Vector2> _onHoverMouseRuntime = new();
        [field: SerializeField] private UnityEvent<Vector2> _onUpdateMouseRuntime = new();

        private bool _isMove = false;
        
        protected override void Init()
        {
            SceneManager.sceneLoaded += onSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= onSceneLoaded;
        }

        private void onSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _onMoveBeginMouseForNowScene.RemoveAllListeners();
            _onMoveEndMouseForNowScene.RemoveAllListeners();
            _onHoverMouseForNowScene.RemoveAllListeners();
        }

        private void Update()
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector2 delta = Mouse.current.delta.ReadValue();
            bool isMoving = delta.sqrMagnitude > MovementThreshold * MovementThreshold;
            
            _onUpdateMouseForNowScene.Invoke(mousePosition);
            _onUpdateMouseRuntime.Invoke(mousePosition);
            
            if (isMoving)
            {
                _onHoverMouseForNowScene.Invoke(mousePosition);
                _onHoverMouseRuntime.Invoke(mousePosition);

                if (!_isMove)
                {
                    _isMove = true;
                    _onMoveBeginMouseForNowScene.Invoke(mousePosition);
                    _onMoveBeginMouseForRuntime.Invoke(mousePosition);
                }
            }
            else
            {
                if (_isMove)
                {
                    _isMove = false;
                    _onMoveEndMouseForNowScene.Invoke(mousePosition);
                    _onMoveEndMouseRuntime.Invoke(mousePosition);
                }
            }
        }
    }
}