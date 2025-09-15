using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Components
{
    [DisallowMultipleComponent]
    public sealed class MovementTracker : MonoBehaviour
    {
        public delegate void DeltaCallback(Vector3 delta);
        
        private const float THRESHOLD_LIMIT_MIN = 0.01f;
        private const float THRESHOLD_LIMIT_MAX = 0.5f;

        /// <summary>
        /// .. 움직임을 감지하는 오차범위
        /// </summary>
        public float PositionThreshold
        {
            get => _positionThreshold;
            set => _positionThreshold = Mathf.Clamp(value, THRESHOLD_LIMIT_MIN, THRESHOLD_LIMIT_MAX);
        }

        [SerializeField, Range(THRESHOLD_LIMIT_MIN, THRESHOLD_LIMIT_MAX)]
        private float _positionThreshold = 0.01f;

        private Vector3 _previousPosition = Vector3.zero;
        private bool _isMoving = false;
        private DeltaCallback _onMoveBegan = null;
        private DeltaCallback _onMoveOnGoing = null;
        private DeltaCallback _onMoveEnded = null;

        private void Start()
        {
            _previousPosition = transform.position;
        }

        private void OnDisable()
        {
            _onMoveBegan = null;
            _onMoveOnGoing = null;
            _onMoveEnded = null;
        }

        private void FixedUpdate()
        {
            Vector3 currentPosition = transform.position;

            bool currentlyMoving = hasMoved(currentPosition, _previousPosition, _positionThreshold);

            switch (currentlyMoving)
            {
                case true when !_isMoving:
#if DEBUG
                    Debug.Log("Move Begin");
#endif
                    _onMoveBegan?.Invoke(currentPosition - _previousPosition);
                    _isMoving = true;
                    break;
                case false when _isMoving:
#if DEBUG
                    Debug.Log("Move Ended");
#endif
                    _onMoveEnded?.Invoke(currentPosition - _previousPosition);
                    _isMoving = false;
                    break;
                case true when _isMoving:
#if DEBUG
                    Debug.Log("Moving");
#endif
                    _onMoveOnGoing?.Invoke(currentPosition - _previousPosition);
                    break;
            }

            _previousPosition = currentPosition;
        }

        public void AddListenerOnMoveBegined(DeltaCallback action)
            => _onMoveBegan += action;

        public void AddListenerOnMoveOnGoing(DeltaCallback action)
            => _onMoveOnGoing += action;

        public void AddListenerOnMoveEnded(DeltaCallback action)
            => _onMoveEnded += action;

        public void RemoveListenerOnMoveBegined(DeltaCallback action)
            => _onMoveBegan -= action;

        public void RemoveListenerOnMoveOnGoing(DeltaCallback action)
            => _onMoveOnGoing -= action;

        public void RemoveListenerOnMoveEnded(DeltaCallback action)
            => _onMoveEnded -= action;

        public void RemoveAllListenerOnMoveBegan()
            => _onMoveBegan = null;

        public void RemoveAllListenerOnMoveOnGoing()
            => _onMoveOnGoing = null;

        public void RemoveAllListenerOnMoveEnded()
            => _onMoveEnded = null;

        private static bool hasMoved(Vector3 currentPosition, Vector3 previousPosition, float threshold)
            => Vector3.Distance(currentPosition, previousPosition) > threshold;
    }
}
