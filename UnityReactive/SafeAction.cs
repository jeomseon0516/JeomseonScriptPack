using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.UnityReactive
{
    /// <summary>
    /// .. 딜리게이트의 추가/제거에 안전한 메서드를 제공하는 클래스 입니다
    /// 딜리게이트의 참조를 직접 관리하지 않는 경우 메모리 누수를 유발할 수 있습니다
    /// </summary>
    public sealed class SafeAction
    {
        private Action _action = null;

        public void AddListener(Action action)
        {
            _action += action;
        }

        public void RemoveListener(Action action)
        {
            if (_action is null) return;

            _action -= action;
        }

        public void Invoke()
        {
            _action?.Invoke();
        }
    }
    
    public sealed class SafeAction<T>
    {
        private Action<T> _action = null;

        public void AddListener(Action<T> action)
        {
            _action += action;
        }

        public void RemoveListener(Action<T> action)
        {
            if (_action is null) return;

            _action -= action;
        }

        public void Invoke(T item)
        {
            _action.Invoke(item);
        }
    }
}
