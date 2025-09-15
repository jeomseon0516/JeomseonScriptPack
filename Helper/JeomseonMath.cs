using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Helper
{
    public static class JeomseonMath
    {
        public static int GetArithmeticSeriesSum(int target)
        {
            return target * (target + 1) / 2;
        }
    }
}