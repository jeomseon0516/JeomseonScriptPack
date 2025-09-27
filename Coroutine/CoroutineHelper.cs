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
        private static readonly Dictionary<float, WaitForSecondsRealtime> _waitForSecondsRealtimeDic = new Dictionary<float, WaitForSecondsRealtime>();

        public static WaitForSeconds WaitForSeconds(float delayTime)
        {
            if (!_waitForSecondsDic.TryGetValue(delayTime, out WaitForSeconds value))
            {
                value = new WaitForSeconds(delayTime);
                _waitForSecondsDic.Add(delayTime, value);
            }

            return value;
        }

        public static WaitForSecondsRealtime WaitForSecondsRealtime(float delayTime)
        {
            if (_waitForSecondsRealtimeDic.TryGetValue(delayTime, out WaitForSecondsRealtime value))
            {
                value = new WaitForSecondsRealtime(delayTime);
                _waitForSecondsRealtimeDic.Add(delayTime, value);
            }

            return value;
        }
    }
}
