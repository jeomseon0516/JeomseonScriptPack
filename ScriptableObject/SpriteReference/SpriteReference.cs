using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.ScriptableObjects
{
    [CreateAssetMenu(fileName = "SpriteReference", menuName = "Scriptable Object/Sprite Reference")]
    public sealed class SpriteReference : UnityEngine.ScriptableObject
    {
        [field: SerializeField] public Sprite Sprite { get; private set; }
    }
}
