using WAD.NET.Archives;

namespace WAD.NET.Composite
{
    /// <summary>
    /// Represents a lump after multi-archive resolution, tracking its origin and resolution type.
    /// </summary>
    public class ResolvedLump
    {
        /// <summary>
        /// The resolved lump entry.
        /// </summary>
        public LumpEntry Lump { get; init; } = null!;

        /// <summary>
        /// How this lump was resolved (Base, Override, or Added).
        /// </summary>
        public LumpResolutionType Resolution { get; init; }

        /// <summary>
        /// Index of the source archive in the load order.
        /// </summary>
        public int SourceIndex { get; init; }

        /// <summary>
        /// File or folder path of the archive that provided this lump.
        /// </summary>
        public string SourcePath { get; init; } = string.Empty;

        /// <summary>
        /// The lump that this one replaced, if <see cref="Resolution"/> is <see cref="LumpResolutionType.Override"/>.
        /// Null for Base and Added lumps.
        /// </summary>
        public LumpEntry? OverriddenLump { get; init; }
    }
}
