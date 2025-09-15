using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Attribute
{
    /// <summary>
    /// .. 해당 어트리뷰트를 참조중인 컴포넌트가 모노비하이비어나 컴포넌트 클래스에 의해 참조중인 경우 해당 모노비하이비어(or 컴포넌트)가 붙어있는 오브젝트에 해당하는 컴포넌트를 부착합니다
    /// </summary>
    [AttributeUsage(AttributeTargets.Field), Conditional("UNITY_EDITOR")]
    public sealed class InitializeRequireComponentAttribute : PropertyAttribute {}
}
