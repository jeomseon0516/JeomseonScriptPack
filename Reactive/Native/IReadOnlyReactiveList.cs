using System;
using System.Collections.Generic;

namespace Jeomseon.Reactive
{
    public delegate void ElementChangedHandler<in T>(int index, T previous, T current);
    public delegate void AddOrRemoveHandler<in T>(int[] indices, T[] items);

    /// <summary>
    /// .. 내부 값을 추가/제거/변경이 불가능하고 리스너 추가만 가능한 읽기전용 인터페이스입니다
    /// </summary>
    public interface IReadOnlyReactiveList<out T> : IReadOnlyList<T>
    {
        /// <summary>
        /// .. 값이 추가될때 발행되는 이벤트를 구독자에게 알림없이 리스너를 추가합니다
        /// </summary>
        /// <param name="onAddAction"> .. 리스너 메서드 </param>
        void AddListenerToAddedEventWithoutNotify(AddOrRemoveHandler<T> onAddAction);
        /// <summary>
        /// .. 값이 추가 될때 발행되는 이벤트입니다 리스너 추가시 한번 이벤트를 발행합니다
        /// </summary>
        event AddOrRemoveHandler<T> AddedEvent;
        /// <summary>
        /// .. 값이 제거 될때 발행되는 이벤트입니다
        /// </summary>
        event AddOrRemoveHandler<T> RemovedEvent;
        /// <summary>
        /// .. 내부
        /// </summary>
        event ElementChangedHandler<T> ChangedEvent;
        /// <summary>
        /// .. 내부 값들의 순서가 재배치 될때 발행되는 이벤트입니다
        /// </summary>
        event Action<IReadOnlyList<T>> ReorderedEvent;
    }
}
