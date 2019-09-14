using System;

namespace Bot3PG.Modules
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ReleaseAttribute : Attribute
    {
        public Release Release { get; private set; }

        public ReleaseAttribute(Release release) => Release = release;
    }
}
