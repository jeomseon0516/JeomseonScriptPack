using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.State
{
    /// <summary>
    /// .. 구현할 상태를 해당 인터페이스를 상속받아 구현합니다.
    /// IStateObject를 상속받는 클래스의 innerClass로 구현하는 것이 좋습니다.
    /// </summary>
    public interface IState<T> where T : class, IStateObject<T>
    {
        /// <summary>
        /// .. 초기에 상태가 생성될 시에만 해당 함수 호출
        /// </summary>
        void Awake(T t);
        /// <summary>
        /// .. 상태가 변경됐을때 Update 전에 한번만 호출
        /// </summary>
        void Enter(T t);
        /// <summary>
        /// .. 상태가 변경되기 전까지 계속해서 Update 호출
        /// </summary>
        void Update(T t);
        /// <summary>
        /// .. 상태가 변경됐을때 호출 StateMachine은 항상 IState를 캐싱해두기 때문에 Exit에서 재사용할 필드들을 원래 값으로 초기화 시켜주어야 합니다.
        /// </summary>
        void Exit(T t);
    }

    /// <summary>
    /// .. IState 확장 기능
    /// </summary>
    public interface IFixedUpdateState<T> where T : class, IStateObject<T>
    {
        /// <summary>
        /// .. 상태가 변경되기 전까지 계속해서 FixedUpdate 호출 ..
        /// </summary>
        void FixedUpdate(T t);
    }
}