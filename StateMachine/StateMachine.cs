using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.State
{
    /// <summary>
    /// .. StatePattern을 사용하려면 해당 인터페이스를 상속받아야 합니다.
    /// </summary>
    public interface IStateObject<T> where T : class, IStateObject<T>
    {
        StateMachine<T> StateMachine { get; }
    }

    public sealed class StateMachine<T> where T : class, IStateObject<T>
    {
        private readonly Dictionary<string, IState<T>> _stateList = new(); // .. 상태들을 저장. 상태들을 매번 new로 생성하면 가비지가 생성되고 비용이 크기 때문
        private IState<T> _state = null;                                   // .. 현재 상태

        public void Update(T controller) => _state?.Update(controller);
        public void FixedUpdate(T controller) => (_state as IFixedUpdateState<T>)?.FixedUpdate(controller);

        /// <summary>
        /// .. 호출 순서 생성자 => Awake(State의 첫 생성시에만 호출) => Enter(State가 변경되어 Update되기전 한번만 호출) => Update => Exit(State가 변경될 시에만 호출 OnDestroy나 OnDisable과 같음)
        /// </summary>
        public void ChangeState<ToState>(T controller) where ToState : IState<T>, new() // .. 딕셔너리에서 저장 된 상태를 가져옴 
        {
            if (!_stateList.TryGetValue(typeof(ToState).Name, out IState<T> state))
            {
                ToState newState = new();

                newState.Awake(controller);
                _stateList.Add(typeof(ToState).Name, newState);

                state = newState;
            }

            _state?.Exit(controller);
            _state = state;
            _state.Enter(controller);
        }

        public bool CheckNowState<AnyState>() where AnyState : IState<T> => _state is AnyState;

        /// <summary>
        /// .. AnyState로 들어온 클래스가 현재 상태가 맞는지 확인하고 맞다면 해당 상태를 반환하고 그렇지 않으면 null을 반환합니다.
        /// IState를 상속받은 클래스에 있는 필드 또는 프로퍼티의 값이 필요할 경우 사용할 수 있는 메서드 입니다.
        /// </summary>
        /// <typeparam name="AnyState"></typeparam>
        /// <returns></returns>
        public AnyState GetState<AnyState>() where AnyState : class, IState<T> => _state as AnyState;
    }
}