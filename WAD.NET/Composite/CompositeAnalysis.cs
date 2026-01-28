using System.Collections.Generic;
using WAD.NET.Detection;

namespace WAD.NET.Composite
{
    /// <summary>
    /// Result of analyzing a <see cref="CompositeArchive"/>, including compatibility
    /// requirements and lump conflicts between non-base archives.
    /// </summary>
    public class CompositeAnalysis
    {
        /// <summary>
        /// Compatibility analysis of the effective (resolved) lump set.
        /// </summary>
        public ModCompatibility Compatibility { get; init; } = null!;

        /// <summary>
        /// Lump conflicts where two or more non-base archives provide the same lump.
        /// </summary>
        public IReadOnlyList<LumpConflict> Conflicts { get; init; } = System.Array.Empty<LumpConflict>();
    }
}
