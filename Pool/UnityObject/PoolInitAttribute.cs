using System;

namespace Jeomseon.ObjectPool
{
    using Attribute = System.Attribute;

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class PoolInitAttribute : Attribute
    {
        public object DefaultValue { get; }
        public PoolInitAttribute(object defaultValue = null)
        {
            DefaultValue = defaultValue;
        }
    }
}
