using System;

namespace Bot3PG.DataStructs.Attributes
{
    public class ExtraInfoAttribute : Attribute
    {
        public string ExtraInfo { get; private set; }

        public ExtraInfoAttribute(string info) => ExtraInfo = info;
    }
}