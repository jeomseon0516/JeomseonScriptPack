using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Jeomseon.Components.Movement
{
    /// <summary>
    /// .. 타겟을 Vector2로 삼는 스무스 무버입니다 값 타입이기 때문에 실시간으로 변하는 타겟의 움직임에 대응하기 힘듭니다
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Vector2SmoothMover : MonoBehaviour, IObjectSmoothMover
    {
        [field: Header("Target")]
        [field: SerializeField] public Vector2 TargetPosition { get; set; } = Vector2.positiveInfinity;

        [field: Header("Move Options")]
        [field: SerializeField] public float Ratio { get; set; } = 0.45f;
        [field: SerializeField] public bool IsLocal { get; set; } = false;

        private void FixedUpdate()
        {
            if (float.IsInfinity(TargetPosition.x) || float.IsInfinity(TargetPosition.y)) return;

            Vector3 target = (this as IObjectSmoothMover)
                .GetVector(
                IsLocal ? transform.localPosition : transform.position,
                (Vector3)TargetPosition);

            if (IsLocal)
            {
                transform.localPosition += new Vector3(target.x, target.y, 0f);
            }
            else
            {
                transform.position += new Vector3(target.x, target.y, 0f);
            }
        }
    }
}
