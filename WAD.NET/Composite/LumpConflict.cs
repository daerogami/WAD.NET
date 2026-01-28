using System.Collections.Generic;

namespace WAD.NET.Composite
{
    /// <summary>
    /// Represents a conflict where two or more non-base archives provide the same lump.
    /// </summary>
    public class LumpConflict
    {
        /// <summary>
        /// The lump name that is provided by multiple non-base archives.
        /// </summary>
        public string LumpName { get; init; } = string.Empty;

        /// <summary>
        /// The non-base sources that each provide this lump.
        /// </summary>
        public IReadOnlyList<LumpConflictSource> Sources { get; init; } = System.Array.Empty<LumpConflictSource>();
    }
}
