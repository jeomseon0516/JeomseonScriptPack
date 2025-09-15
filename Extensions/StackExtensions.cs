using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Extensions
{
    public static class StackExtensions
    {
        public static void PopAll<T>(this Stack<T> stack, Action<T> action)
        {
            while (stack.Count > 0)
            {
                action.Invoke(stack.Pop());
            }
        }

        public static IEnumerable<T> Popable<T>(this Stack<T> stack)
        {
            while (stack.Count > 0)
            {
                yield return stack.Pop();
            }
        }
    }
}
