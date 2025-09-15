using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.UI
{
    using Singleton;

    public sealed class UIManager : Singleton<UIManager>
    {
        [Header("UI Canvas")]
        [SerializeField]
        private Canvas _canvas;

        [SerializeField]
        private List<BaseUI> _baseUIList = new();
        private readonly Dictionary<string, BaseUI> _uiPool = new();
        private readonly List<BaseUI> _activeUIStack = new();

        protected override void Init()
        {
            _uiPool.EnsureCapacity(_baseUIList.Count);

            foreach (BaseUI baseUI in _baseUIList)
            {
                baseUI.SetActive(false);
                _uiPool.Add(baseUI.GetType().Name, baseUI);
            }

            _baseUIList.Clear();
            _baseUIList = null;
        }

        public T OpenUI<T>() where T : BaseUI
        {
            T openedUI = GetUI<T>();
            openUI(openedUI);

            return openedUI;
        }

        public T GetUI<T>() where T : BaseUI
            => _uiPool.TryGetValue(typeof(T).Name, out BaseUI ui) ? ui as T : null;

        private void openUI(BaseUI obj)
        {
            if (!obj) return; // .. 비동기식으로 불러오기 때문에 씬 로드후 null인 상태가 존재할 수 있음

            if (_activeUIStack.Count > 0) // .. 이미 켜져있는 UI가 있다면 레이캐스트 비활성화
            {
                _activeUIStack[^1].GraphicRaycaster.enabled = false;
            }

            if (obj.ActiveSelf) // .. 해당 UI가 이미 켜져있다면? 
            {
                int index = _activeUIStack.IndexOf(obj); // .. 인덱스 구하기
                obj.transform.SetAsLastSibling(); // .. 가장 먼저 출력하기

                // .. 원래 마지막에 출력되고 있던 UI랑 자리바꾸기
                (_activeUIStack[^1], _activeUIStack[index]) = (_activeUIStack[index], _activeUIStack[^1]);
                obj.GraphicRaycaster.enabled = true; // .. 레이캐스트 활성화
            }
            else
            {
                _activeUIStack.Add(obj); // .. 스택에 없는 상태일때
                obj.SetActive(true); // .. 활성화
            }
        }

        public void CloseUI(BaseUI closedUI)
        {
            while (_activeUIStack.Count > 0)
            {
                BaseUI currentUI = _activeUIStack[^1]; // .. 기본적으로 구조가 명시되어있지 않지만 UI가 켜지고 꺼지는건 서로 연관관계가 있는 UI만 스택에 쌓이게 됌 

                _activeUIStack[^1].SetActive(false); // .. 스택 끝자리 비활성화
                _activeUIStack.RemoveAt(_activeUIStack.Count - 1); // .. 스택에서 제거

                if (_activeUIStack.Count > 0) // .. 스택에서 제거후 존재하는 UI가 있다면 끝자리 UI 활성화
                {
                    _activeUIStack[^1].GraphicRaycaster.enabled = true;
                }

                if (currentUI == closedUI) // .. 선택된 UI까지 off되었다면 return 
                {
                    return;
                }
            }
        }

        public void CloseAllUI() // .. 모든 UI off
        {
            _activeUIStack.ForEach(ui => ui.SetActive(false));
            _activeUIStack.Clear();
        }

        public void InsertNewUI(BaseUI ui) // .. 만약 새로운 UI가 추가 된다면?
        {
            string uiName = ui.GetType().Name;

            if (!_uiPool.ContainsKey(uiName))
            {
                _uiPool.Add(uiName, ui);
            }
        }

        public void HideUI(bool isHide) => _canvas.gameObject.SetActive(!isHide);
    }
}