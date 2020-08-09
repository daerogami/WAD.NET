using System;

namespace WAD.NET.UDMF.Fields
{
    public static class Specification {
        public const string UDMF = "udmf";
        public const string UDMF_ZDOOM = "udmf_zdoom";
    }

    internal class FieldSpecificationAttribute : Attribute
    {
        string SpecificationId { get; }
        Version SpecificationVersion { get; }

        public FieldSpecificationAttribute(string specId, int majorVersion, int minorVersion)
        {
            SpecificationId = specId;
            SpecificationVersion = new Version(majorVersion, minorVersion);
        }
    }
}