using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Jeomseon.Extensions;

namespace Jeomseon.Dispatcher
{
    /// <summary>
    /// .. 비동기적 처리를 하는 경우 특정 결과를 메인스레드에서 처리하도록 도와주는 디스패처 클래스입니다
    /// Editor와 Runtime 모두 처리 가능합니다
    /// </summary>
    public static class UnitySyncContextDispatcher
    {
        private static readonly Queue<Action> _executionQueue = new();
        private static SynchronizationContext _unityContext;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // .. InitializedOnLoadMethod를 호출하는 Context는 유니티의 메인 스레드이므로 현재 선택된 스레드는 메인 스레드이다 현재 스레드를 캐쉬해둔다
            _unityContext = SynchronizationContext.Current;
            Application.quitting += () =>
            {
                _unityContext = null;
                lock (_executionQueue)
                {
                    _executionQueue.Clear();
                }
            };
        }

        private static void ExecuteActions()
        {
#if DEBUG
            Debug.Log("Execute Actions");
#endif

            lock (_executionQueue)
            {
                foreach (Action action in _executionQueue.Dequeueable())
                {
                    try // .. 예외 발생시 큐에 담긴 나머지 콜백들이 처리되도록 보장
                    {
                        action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Exception in UnitySyncContextDispatcher.ExecuteActions: {ex}");
                    }
                }
            }
        }

        public static void Enqueue(Action action)
        {
            if (_unityContext == null)
            {
                throw new InvalidOperationException("UnitySyncContextDispatcher 가 초기화 되어있지 않습니다");
            }

            lock (_executionQueue)
            {
                if (action is not null)
                {
                    _executionQueue.Enqueue(action);
                    _unityContext.Post(_ => ExecuteActions(), null); // .. 유니티 스레드의 메세지큐에 동기화 시켜서 호출할 메서드를 보낸다
                }
            }
        }
    }
}
