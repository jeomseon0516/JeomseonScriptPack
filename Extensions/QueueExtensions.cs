using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Extensions
{
    public static class QueueExtensions
    {
        public static void DequeueAll<T>(this Queue<T> queue, Action<T> action)
        {
            while (queue.Count > 0)
            {
                action.Invoke(queue.Dequeue());
            }
        }

        // .. foreach 문을 사용하기 위한 확장 메서드 입니다 도중에 break시 내부 요소가 남아있을 수 있습니다
        public static IEnumerable<T> Dequeueable<T>(this Queue<T> queue)
        {
            while (queue.Count > 0)
            {
                yield return queue.Dequeue();
            }
        }

        public static void EnqueueItems<T>(this Queue<T> queue, IEnumerable<T> items)
        {
            items.ForEach(queue.Enqueue);
        }
    }
}
