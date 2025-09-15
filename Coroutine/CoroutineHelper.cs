using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Coroutine
{
    public static class CoroutineHelper
    {
        public static WaitForEndOfFrame WaitForEndOfFrame { get; } = new WaitForEndOfFrame();
        public static WaitForFixedUpdate WaitForFixedUpdate { get; } = new WaitForFixedUpdate();

        private static readonly Dictionary<float, WaitForSeconds> _waitForSecondsDic = new Dictionary<float, WaitForSeconds>();

        public static WaitForSeconds WaitForSeconds(float delayTime)
        {
            if (!_waitForSecondsDic.TryGetValue(delayTime, out WaitForSeconds value))
            {
                value = new WaitForSeconds(delayTime);
                _waitForSecondsDic.Add(delayTime, value);
            }

            return value;
        }
    }
}
