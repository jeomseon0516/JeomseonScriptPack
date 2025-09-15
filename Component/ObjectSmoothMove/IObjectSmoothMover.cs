using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Components.Movement
{
    public interface IObjectSmoothMover
    {
        float Ratio { get; set; }

        Vector3 GetVector(Vector3 objectPosition, Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - objectPosition).normalized;
            return Ratio * Vector3.Distance(objectPosition, targetPosition) * direction;
        }
    }
}
