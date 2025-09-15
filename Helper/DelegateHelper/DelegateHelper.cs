using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Helper
{
    public static class DelegateHelper
    {
        public static void AddListener<T>(ref T @delegate, T action) where T : Delegate
        {
            if (@delegate == null)
            {
                @delegate = action;
                return;
            }

            @delegate = (T)Delegate.Combine(@delegate, action); ;
        }

        public static void RemoveListener<T>(ref T @delegate, T action) where T : Delegate
        {
            if (@delegate == null) return;

            @delegate = (T)Delegate.Remove(@delegate, action);
        }
    }
}
