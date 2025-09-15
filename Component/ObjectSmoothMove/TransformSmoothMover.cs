using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Components.Movement
{
    [DisallowMultipleComponent]
    public sealed class TransformSmoothMover : MonoBehaviour, IObjectSmoothMover
    {
        [field: Header("Target")]
        [field: SerializeField] public Transform TargetTransform { get; set; } = null;

        [field: Header("Move Option")]
        [field: SerializeField, Range(0.15f, 1f)] public float Ratio { get; set; } = 0.45f;

        // Update is called once per frame
        private void FixedUpdate()
        {
            if (!TargetTransform) return;

            transform.position += (this as IObjectSmoothMover).GetVector(transform.position, TargetTransform.position);
        }
    }
}