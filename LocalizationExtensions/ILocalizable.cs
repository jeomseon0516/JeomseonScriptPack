using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.LocalizationExtensions
{
    public interface ILocalizable
    {
        LocalizedStringOption StringOption { get; }
    }
}
