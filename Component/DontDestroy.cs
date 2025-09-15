using System.Collections;
using UnityEngine;

namespace Jeomseon
{
    [DisallowMultipleComponent]
    public class DontDestroy : MonoBehaviour
    {
        private void Start() => DontDestroyOnLoad(gameObject);
        private DontDestroy() { }
    }
}