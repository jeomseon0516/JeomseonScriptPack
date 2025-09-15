using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Jeomseon.Coroutine
{
    using Coroutine = UnityEngine.Coroutine;

    public static class CoroutineExtensions
    {
        #region 공개 인터페이스
        public static void StopCoroutineIfNotNull(this MonoBehaviour monoBehaviour, Coroutine coroutine)
        {
            if (!monoBehaviour || coroutine is null) return;

            monoBehaviour.StopCoroutine(coroutine);
        }

        public static void StopCoroutineIfNotNull(this MonoBehaviour monoBehaviour, IEnumerator enumerator)
        {
            if (!monoBehaviour || enumerator is null) return;
            
            monoBehaviour.StopCoroutine(enumerator);
        }

        public static Coroutine DoCallWaitForOneFrame(this MonoBehaviour monoBehaviour, Action action)
         => monoBehaviour.StartCoroutine(iEDoCallWaitForOneFrame(action));

        public static Coroutine DoCallWaitForSeconds(this MonoBehaviour monoBehaviour, float delayTime, Action action)
            => monoBehaviour.StartCoroutine(iEDoCallWaitForSeconds(delayTime, action));

        public static Coroutine DoCallRoofCoroutine<T>(this MonoBehaviour monoBehaviour, Func<bool> action) where T : YieldInstruction
            => monoBehaviour.StartCoroutine(iEDoCallRoofCoroutine<T>(action));

        public static Coroutine DoCallRoofCoroutine<T>(this MonoBehaviour monoBehaviour, Func<bool> action, Action init) where T : YieldInstruction
            => monoBehaviour.StartCoroutine(iEDoCallRoofCoroutine<T>(action, init));

        public static Coroutine DoCallRoofCoroutine<T>(this MonoBehaviour monoBehaviour, Func<bool> action, Action init, Action finish) where T : YieldInstruction
            => monoBehaviour.StartCoroutine(iEDoCallRoofCoroutine<T>(action, init, finish));

        public static Coroutine DoCallRoofCoroutineFinish<T>(this MonoBehaviour monoBehaviour, Func<bool> action, Action finish) where T : YieldInstruction
            => monoBehaviour.StartCoroutine(iEDoCallRoofCoroutineFinish<T>(action, finish));

        public static Coroutine WaitCompletedConditions(this MonoBehaviour monoBehaviour, Func<bool> match, Action callback)
            => monoBehaviour.StartCoroutine(iECallWaitCompletedConditions(match, callback));

        public static Coroutine WaitCompletedAsync(this MonoBehaviour monoBehaviour, Action asyncAction, Action callback)
            => monoBehaviour.StartCoroutine(iEWaitCompletedAsyncTask(Task.Run(asyncAction), callback));

        public static Coroutine WaitCompletedAsync<T>(this MonoBehaviour monoBehaviour, Func<T> asycnAction, Action<T> callback)
            => monoBehaviour.StartCoroutine(iEWaitCompletedAsyncTask(asycnAction, callback));

        public static Coroutine ProgressFromEnumerable<T>(this MonoBehaviour monoBehaviour, IEnumerable<T> objects, Action<T> callback)
            => monoBehaviour.StartCoroutine(iEProgressFromEnumerable(objects, callback));

        public static Coroutine GetWaitComponent<T>(this MonoBehaviour monoBehaviour, Action<T> foundAction) where T : Component
            => monoBehaviour.StartCoroutine(iEGetComponent(monoBehaviour, foundAction));

        public static Coroutine GetWaitComponent<T>(this MonoBehaviour monoBehaviour, float delayTime, Action<T> foundAction) where T : Component
            => monoBehaviour.StartCoroutine(iEGetComponent(monoBehaviour, delayTime, foundAction));

        public static IEnumerator AddCallback(this Coroutine coroutine, Action callback)
        {
            yield return coroutine;
            callback.Invoke();
        }

        public static IEnumerator AddCallback(this IEnumerator enumerator, Action callback)
        {
            yield return enumerator;
            callback.Invoke();
        }
#endregion

        private static IEnumerator iECallWaitCompletedConditions(Func<bool> match, Action callback)
        {
            yield return new WaitUntil(match);
            callback.Invoke();
        }

        private static IEnumerator iEWaitCompletedAsyncTask(Task task, Action callback)
        {
            yield return task;
            callback.Invoke();
        }

        private static IEnumerator iEWaitCompletedAsyncTask<T>(Func<T> asyncAction, Action<T> callback)
        {
            T someObject = default(T);

            yield return Task.Run(() => someObject = asyncAction.Invoke());
            callback.Invoke(someObject);
        }

        private static IEnumerator iEProgressFromEnumerable<T>(IEnumerable<T> objects, Action<T> callback)
        {
            foreach (T item in objects)
            {
                callback.Invoke(item);
                yield return null;
            }
        }

        private static IEnumerator iEDoCallWaitForOneFrame(Action action)
        {
            yield return null;
            action.Invoke();
        }

        private static IEnumerator iEDoCallWaitForSeconds(float delayTime, Action action)
        {
            yield return CoroutineHelper.WaitForSeconds(delayTime);
            action.Invoke();
        }

        private static IEnumerator iEDoCallRoofCoroutine<T>(Func<bool> action) where T : YieldInstruction
        {
            T yieldInstruction = getYieldInstruction<T>();

            do
            {
                yield return yieldInstruction;
            } while (action.Invoke());
        }

        private static IEnumerator iEDoCallRoofCoroutine<T>(Func<bool> action, Action init) where T : YieldInstruction
        {
            init.Invoke();
            yield return iEDoCallRoofCoroutine<T>(action);
        }

        private static IEnumerator iEDoCallRoofCoroutine<T>(Func<bool> action, Action init, Action finish) where T : YieldInstruction
        {
            init.Invoke();
            yield return iEDoCallRoofCoroutine<T>(action);
            finish.Invoke();
        }

        private static IEnumerator iEDoCallRoofCoroutineFinish<T>(Func<bool> action, Action finish) where T : YieldInstruction
        {
            yield return iEDoCallRoofCoroutine<T>(action);
            finish.Invoke();
        }

        private static IEnumerator iEGetComponent<T>(MonoBehaviour monoBehaviour, Action<T> foundAction) where T : Component
        {
            T component;
            float time = 2.0f;

            while (!monoBehaviour.TryGetComponent(out component) && time > 0.0f)
            {
                yield return null;
                time -= Time.deltaTime;
            }

            foundAction.Invoke(component);
        }

        private static IEnumerator iEGetComponent<T>(MonoBehaviour monoBehaviour, float delayTime, Action<T> foundAction) where T : Component
        {
            T component;

            while (!monoBehaviour.TryGetComponent(out component) && delayTime > 0.0f)
            {
                yield return null;
                delayTime -= Time.deltaTime;
            }

            foundAction.Invoke(component);
        }

        private static T getYieldInstruction<T>() where T : YieldInstruction => typeof(T) switch
        {
            { } type when type == typeof(WaitForEndOfFrame) => CoroutineHelper.WaitForEndOfFrame as T,
            { } type when type == typeof(WaitForFixedUpdate) => CoroutineHelper.WaitForFixedUpdate as T,
            _ => null
        };
    }
}
