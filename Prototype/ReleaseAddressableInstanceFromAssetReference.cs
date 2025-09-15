using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Prototype
{
    internal sealed class ReleaseAddressableInstanceFromAssetReference : ReleaseAddressableInstance
    {
        protected override void Release()
        {
            PrototypeManager.Instance.ReleaseInstanceFromRuntimeKey(_primaryKey);
        }
    }
}