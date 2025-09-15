using System;
using System.Collections.Generic;
using UnityEngine;
using Jeomseon.Singleton;

namespace Jeomseon.Coroutine
{
    using Coroutine = UnityEngine.Coroutine;

    public sealed class CoroutineManager : Singleton<CoroutineManager>
    {
        protected override void Init() {}

        public Coroutine DoCallWaitForOneFrame(Action action)
            => CoroutineExtensions.DoCallWaitForOneFrame(this, action);

        public Coroutine DoCallWaitForSeconds(float delayTime, Action action)
            => CoroutineExtensions.DoCallWaitForSeconds(this, delayTime, action);

        public Coroutine DoCallRoofCoroutine<T>(Func<bool> action) where T : YieldInstruction
            => CoroutineExtensions.DoCallRoofCoroutine<T>(this, action);

        public Coroutine DoCallRoofCoroutine<T>(Func<bool> action, Action init) where T : YieldInstruction
            => CoroutineExtensions.DoCallRoofCoroutine<T>(this, action, init);

        public Coroutine DoCallRoofCoroutine<T>(Func<bool> action, Action init, Action finish) where T : YieldInstruction
            => CoroutineExtensions.DoCallRoofCoroutine<T>(this, action, init, finish);

        public Coroutine DoCallRoofCoroutineFinish<T>(Func<bool> action, Action finish) where T : YieldInstruction
            => CoroutineExtensions.DoCallRoofCoroutineFinish<T>(this, action, finish);

        public Coroutine WaitCompletedConditions(Func<bool> match, Action callback)
            => CoroutineExtensions.WaitCompletedConditions(this, match, callback);

        public Coroutine WaitCompletedAsync(Action asyncAction, Action callback)
            => CoroutineExtensions.WaitCompletedAsync(this, asyncAction, callback);

        public Coroutine WaitCompletedAsync<T>(Func<T> asycnAction, Action<T> callback)
            => CoroutineExtensions.WaitCompletedAsync(this, asycnAction, callback);

        public Coroutine ProgressFromEnumerable<T>(IEnumerable<T> objects, Action<T> callback)
            => CoroutineExtensions.ProgressFromEnumerable(this, objects, callback);

        public Coroutine GetWaitComponent<T>(MonoBehaviour monoBehaviour, Action<T> callback) where T : Component
            => monoBehaviour.GetWaitComponent(callback);

        public Coroutine GetWaitComponent<T>(MonoBehaviour monoBehaviour, float delayTime, Action<T> callback) where T : Component
            => monoBehaviour.GetWaitComponent(delayTime, callback);
    }
}
