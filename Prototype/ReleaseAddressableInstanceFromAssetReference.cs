using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Prototype
{
    internal sealed class ReleaseAddressableInstanceFromAssetReference : ReleaseAddressableInstance
    {
        protected override void Release()
        {
            if (!string.IsNullOrEmpty(PrimaryKey))
                PrototypeManager.ReleaseInstanceFromRuntimeKey(PrimaryKey);
        }
    }
}