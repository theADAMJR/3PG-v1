using System;

namespace Bot3PG.DataStructs
{
    public class PremiumAttribute : Attribute
    {
        public bool IsPremium { get; private set; }

        public PremiumAttribute() => IsPremium = true;
    }
}