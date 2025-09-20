using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Prototype
{
    [DisallowMultipleComponent]
    internal class ReleaseAddressableInstance : MonoBehaviour
    {
        private string _primaryKey;
        internal virtual string PrimaryKey
        {
            get => _primaryKey;
            set
            {
                if (_primaryKey != null)
                {
#if DEBUG
                    Debug.LogWarning("ReleaseAddressableInstance : 키가 이미 할당되어 있습니다");
#endif
                    return;
                }
                _primaryKey = value;
            }
        }

        protected virtual void Release()
        {
            if (!string.IsNullOrEmpty(_primaryKey))
                PrototypeManager.ReleaseInstance(_primaryKey);
        }

        private void OnDestroy() => Release();
    }
}