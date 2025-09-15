using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Scope
{
    public interface ICollectionScope<out TCollection, TItem> : IDisposable where TCollection : ICollection<TItem>
    {
        /// <summary>
        /// 한번 생성된 컬렉션은 메모리 해제전까지는 항상 같은 컬렉션의 참조를 반환합니다
        /// </summary>
        /// <returns></returns>
        TCollection Get();
    }
}
