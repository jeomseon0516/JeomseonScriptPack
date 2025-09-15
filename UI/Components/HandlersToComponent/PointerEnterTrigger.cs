using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Jeomseon.UI.Components
{
    // .. 유니티는 EventTrigger라는 컴포넌트로 여러종류의 핸들러 이벤트를 트리거 시켜주는 컴포넌트가 이미 존재하지만 하나의 컴포넌트에 모든 핸들러 인터페이스를 구현하여 모든 종류의 입력 이벤트를 받아오기 때문에 성능상의 이유로 커스텀 스크립트로 개별 컴포넌트로 기능을 나누어 구현합니다
    // .. 비슷한 기능으로 UniRx의 ObservablePointerEnterTrigger라는 컴포넌트를 제공하지만 인스펙터에 이벤트가 노출되지 않고 스크립트에서만 내용을 수정할 수 있기 때문에 인스펙터 편집이 가능하게끔 구현합니다
    public class PointerEnterTrigger : MonoBehaviour, IPointerEnterHandler
    {
        public event UnityAction<PointerEventData> OnPointerEnterEvent
        {
            add => _onPointerEnterEvent.AddListener(value);
            remove => _onPointerEnterEvent.RemoveListener(value);
        }
        
        [SerializeField] private UnityEvent<PointerEventData> _onPointerEnterEvent = new();

        public void OnPointerEnter(PointerEventData eventData)
        {
            _onPointerEnterEvent.Invoke(eventData);
        }
    }
}