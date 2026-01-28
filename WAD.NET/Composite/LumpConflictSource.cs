using WAD.NET.Archives;

namespace WAD.NET.Composite
{
    /// <summary>
    /// Identifies one source of a conflicting lump.
    /// </summary>
    public class LumpConflictSource
    {
        /// <summary>
        /// Index of the source archive in the load order.
        /// </summary>
        public int SourceIndex { get; init; }

        /// <summary>
        /// File or folder path of the archive that provided this lump.
        /// </summary>
        public string SourcePath { get; init; } = string.Empty;

        /// <summary>
        /// The lump entry from this source.
        /// </summary>
        public LumpEntry Lump { get; init; } = null!;
    }
}
