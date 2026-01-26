using System;
using Xunit;

namespace WAD.NET.Tests.Infrastructure
{
    /// <summary>
    /// Base class for tests that require specific WAD files to be available.
    /// Uses Xunit.SkippableFact for runtime test skipping.
    /// </summary>
    public abstract class RealWorldWadTestBase
    {
        /// <summary>
        /// Skips the test if the WAD file is not available.
        /// Use with [SkippableFact] or [SkippableTheory] attribute.
        /// </summary>
        /// <param name="wadName">Display name for the WAD.</param>
        /// <param name="path">Path to the WAD file.</param>
        protected static void SkipIfNotAvailable(string wadName, string? path)
        {
            Skip.If(
                !WadTestConfiguration.IsAvailable(path),
                WadTestConfiguration.GetSkipReason(wadName, path));
        }

        /// <summary>
        /// Gets the path and throws skip if not available.
        /// </summary>
        protected static string RequireWad(string wadName, string? path)
        {
            SkipIfNotAvailable(wadName, path);
            return path!;
        }
    }
}
