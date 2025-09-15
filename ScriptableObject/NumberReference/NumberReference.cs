using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.ScriptableObjects
{
    public sealed class NumberReference : UnityEngine.ScriptableObject
    {
        [field: SerializeField] public int No { get; }
    }
}