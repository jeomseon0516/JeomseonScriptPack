#if SERIALIZEREFERENCEDROPDOWN_INSTALLED
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Jeomseon.Extensions;

namespace Jeomseon.UnityReactive
{
    /// <summary>
    /// .. 읽기 전용 인터페이스 입니다 가장 기본적인 메서드만 제공합니다
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IReadOnlyReactiveField<out T>
    {
        T Value { get; }
        event Action<T> ChangedEvent;

        /// <summary>
        /// .. 리스너를 추가시 이벤트를 발생시키지 않습니다
        /// </summary>
        /// <param name="onChangedValue"> .. 이벤트 메서드 </param>
        void AddListenerWithoutNotify(Action<T> onChangedValue);
    }

    public interface IReactiveField<T> : IReadOnlyReactiveField<T>
    {
        /// <summary>
        /// .. 값을 읽거나 쓸 수 있습니다. 값을 변경시 이전의 값과 다른 값일 경우에만 이벤트를 트리거합니다
        /// </summary>
        new T Value { get; set; }

        /// <summary>
        /// .. 설정할 값이 이전의 값과 같은 값이어도 이벤트를 강제로 트리거시킵니다 
        /// </summary>
        /// <param name="value"> value </param>
        void SetValueAndForceInvoke(T value);
    }

    [System.Serializable]
    public class ReactiveField<T> : ReactiveFieldBase<T>
    {
        private static readonly EqualityComparer<T> _defaultEqualityComparer = EqualityComparer<T>.Default;
     
        [field: SerializeField] protected T _value { get; set; }

        protected virtual EqualityComparer<T> EqualityComparer => _defaultEqualityComparer;

        /// <summary>
        /// .. SetValueAndForceInvoke 메서드와 Value의 Setter 호출 시 인자로 들어온 값을
        /// 필터링하는 프로세서 인터페이스입니다 사용자 정의 프로세서를 추가시킬시 값을
        /// 프로세서에 한번 필터 시킨후 값을 초기화 시킵니다.
        /// </summary>
        [field: SerializeReference, SerializeReferenceDropdown] public List<IValueProcessor> ValueProcessors { get; set; } = new();

        public override T Value
        {
            get => _value;
            set
            {
                T processedValue = GetProcessedValue(value);

                if (!EqualityComparer.Equals(_value, processedValue))
                {
                    _value = processedValue;
                    _changedEvent.Invoke(_value);
                }
            }
        }

        public override void SetValueAndForceInvoke(T value)
        {
            _value = GetProcessedValue(value);
            _changedEvent.Invoke(_value);
        }

        protected T GetProcessedValue(T value)
        {
            T processedValue = value;
            ValueProcessors?.ForEach(processor => processedValue = processor.Process(processedValue));

            return processedValue;
        }
    }


    [System.Serializable]
    public abstract class ReactiveFieldBase<T> : IReactiveField<T>
    {
        [SerializeField] protected UnityEvent<T> _changedEvent = new();

        /// <summary>
        /// .. 리스너를 추가 제거 시킵니다. 리스너 등록과 동시에 이벤트가 한번 호출됩니다
        /// </summary>
        public event Action<T> ChangedEvent
        {
            add { if (value is null) return; _changedEvent.AddListener((UnityAction<T>)Delegate.CreateDelegate(typeof(UnityAction<T>), value.Target, value.Method)); value.Invoke(Value); }
            remove { if (value is null) return; _changedEvent.RemoveListener((UnityAction<T>)Delegate.CreateDelegate(typeof(UnityAction<T>), value.Target, value.Method)); }
        }

        public abstract T Value { get; set; }

        public virtual void SetValueAndForceInvoke(T value)
        {
            Value = value;
            _changedEvent.Invoke(Value);
        }


        public void AddListenerWithoutNotify(Action<T> onChangedValue)
        {
            if (onChangedValue is null) return;

            _changedEvent.AddListener((UnityAction<T>)Delegate.CreateDelegate(typeof(UnityAction<T>), onChangedValue.Target, onChangedValue.Method));
        }

        public int GetPersistentEventCount() => _changedEvent.GetPersistentEventCount();
        public UnityEngine.Object GetPersistentTarget(int index) => _changedEvent.GetPersistentTarget(index);
        public void SetPersistentListenerState(UnityEventCallState callState) => _changedEvent.SetPersistentListenerState(callState);
        public void SetPersistentListenerState(int index, UnityEventCallState callState) => _changedEvent.SetPersistentListenerState(index, callState);
        public void RemoveAllListener() => _changedEvent.RemoveAllListeners();
        public string GetPersistentMethodName(int index) => _changedEvent.GetPersistentMethodName(index);
        public override string ToString() => Value.ToString();
    }
}
#endif