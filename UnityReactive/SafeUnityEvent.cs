using System;
using System.Collections;
using System.Collections.Generic;
using Jeomseon.Extensions;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Jeomseon.UnityReactive
{
    public interface IUnityEventListenerModifier
    {
        void AddListener(UnityAction call);
        void RemoveListener(UnityAction call);
    }

    public interface IUnityEventListenerModifier<T>
    {
        void AddListener(UnityAction<T> call);
        void RemoveListener(UnityAction<T> call);
    }

    public interface IUnityEventListenerModifier<T0, T1>
    {
        void AddListener(UnityAction<T0, T1> call);
        void RemoveListener(UnityAction<T0, T1> call);
    }

    public interface IUnityEventListenerModifier<T0, T1, T2>
    {
        void AddListener(UnityAction<T0, T1, T2> call);
        void RemoveListener(UnityAction<T0, T1, T2> call);
    }

    public interface IUnityEventListenerModifier<T0, T1, T2, T3>
    {
        void AddListener(UnityAction<T0, T1, T2, T3> call);
        void RemoveListener(UnityAction<T0, T1, T2, T3> call);
    }

    public interface ISafeUnityEventBase
    {
        int GetPersistentEventCount();
        Object GetPersistentTarget(int index);
        void SetPersistentListenerState(UnityEventCallState callState);
        void SetPersistentListenerState(int index, UnityEventCallState callState);
        void RemoveAllListener();
        string GetPersistentMethodName(int index);
    }

    [Serializable]
    public abstract class BaseSafeUnityEvent<TEvent> : ISafeUnityEventBase where TEvent : UnityEventBase, new()
    {
        [SerializeField] protected TEvent _unityEvent = new();

        public int GetPersistentEventCount() => _unityEvent.GetPersistentEventCount();
        public Object GetPersistentTarget(int index) => _unityEvent.GetPersistentTarget(index);
        public void SetPersistentListenerState(UnityEventCallState callState) => _unityEvent.SetPersistentListenerState(callState);
        public void SetPersistentListenerState(int index, UnityEventCallState callState) => _unityEvent.SetPersistentListenerState(index, callState);
        public void RemoveAllListener() => _unityEvent.RemoveAllListeners();
        public string GetPersistentMethodName(int index) =>_unityEvent.GetPersistentMethodName(index);
        public override string ToString() => _unityEvent.ToString();
    }
    
    /// <summary>
    /// .. RemoveAllListener메서드를 제공하지 않는 유니티 이벤트입니다
    /// 딜리게이트에 대한 참조를 관리하지 않는 경우 메모리 누수와 예기치 못한 버그가 발생할 수 있습니다
    /// </summary>
    [Serializable]
    public sealed class SafeUnityEvent : BaseSafeUnityEvent<UnityEvent>, IUnityEventListenerModifier
    {
        public void AddListener(UnityAction call) => _unityEvent.AddListener(call);
        public void RemoveListener(UnityAction call) => _unityEvent.RemoveListener(call);
        public void Invoke() => _unityEvent.Invoke();
    }

    /// <summary>
    /// .. RemoveAllListener메서드를 제공하지 않는 유니티 이벤트입니다
    /// 딜리게이트에 대한 참조를 관리하지 않는 경우 메모리 누수와 예기치 못한 버그가 발생할 수 있습니다
    /// </summary>
    [Serializable]
    public sealed class SafeUnityEvent<T0> : BaseSafeUnityEvent<UnityEvent<T0>>, IUnityEventListenerModifier<T0>
    {
        public void AddListener(UnityAction<T0> call) => _unityEvent.AddListener(call);
        public void RemoveListener(UnityAction<T0> call) => _unityEvent.RemoveListener(call);
        public void Invoke(T0 item) => _unityEvent.Invoke(item);
    }

    /// <summary>
    /// .. RemoveAllListener메서드를 제공하지 않는 유니티 이벤트입니다
    /// 딜리게이트에 대한 참조를 관리하지 않는 경우 메모리 누수와 예기치 못한 버그가 발생할 수 있습니다
    /// </summary>
    [Serializable]
    public sealed class SafeUnityEvent<T0, T1> : BaseSafeUnityEvent<UnityEvent<T0, T1>>, IUnityEventListenerModifier<T0, T1>
    {
        public void AddListener(UnityAction<T0, T1> call) => _unityEvent.AddListener(call);
        public void RemoveListener(UnityAction<T0, T1> call) => _unityEvent.RemoveListener(call);
        public void Invoke(T0 item1, T1 item2) => _unityEvent.Invoke(item1, item2);
    }
    
    /// <summary>
    /// .. RemoveAllListener메서드를 제공하지 않는 유니티 이벤트입니다
    /// 딜리게이트에 대한 참조를 관리하지 않는 경우 메모리 누수와 예기치 못한 버그가 발생할 수 있습니다
    /// </summary>
    [Serializable]
    public sealed class SafeUnityEvent<T0, T1, T2> : BaseSafeUnityEvent<UnityEvent<T0, T1, T2>>, IUnityEventListenerModifier<T0, T1, T2>
    {
        public void AddListener(UnityAction<T0, T1, T2> call) => _unityEvent.AddListener(call);
        public void RemoveListener(UnityAction<T0, T1, T2> call) => _unityEvent.RemoveListener(call);
        public void Invoke(T0 item1, T1 item2, T2 item3) => _unityEvent.Invoke(item1, item2, item3);
    }

    /// <summary>
    /// .. RemoveAllListener메서드를 제공하지 않는 유니티 이벤트입니다
    /// 딜리게이트에 대한 참조를 관리하지 않는 경우 메모리 누수와 예기치 못한 버그가 발생할 수 있습니다
    /// </summary>
    [Serializable]
    public sealed class SafeUnityEvent<T0, T1, T2, T3> : BaseSafeUnityEvent<UnityEvent<T0, T1, T2, T3>>, IUnityEventListenerModifier<T0, T1, T2, T3>
    {
        public void AddListener(UnityAction<T0, T1, T2, T3> call) => _unityEvent.AddListener(call);
        public void RemoveListener(UnityAction<T0, T1, T2, T3> call) => _unityEvent.RemoveListener(call);
        public void Invoke(T0 item1, T1 item2, T2 item3, T3 item4) => _unityEvent.Invoke(item1, item2, item3, item4);
    }
}