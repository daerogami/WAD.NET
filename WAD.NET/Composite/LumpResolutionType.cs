namespace WAD.NET.Composite
{
    /// <summary>
    /// Describes how a lump was resolved in a multi-archive load order.
    /// </summary>
    public enum LumpResolutionType
    {
        /// <summary>Original lump from the first archive (typically the IWAD).</summary>
        Base,

        /// <summary>Lump replaced by a later archive in the load order.</summary>
        Override,

        /// <summary>New lump not present in any earlier archive.</summary>
        Added
    }
}
