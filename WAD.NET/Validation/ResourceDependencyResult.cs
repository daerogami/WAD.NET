using System.Collections.Generic;
using System.Linq;

namespace WAD.NET.Validation
{
    /// <summary>
    /// The result of cross-resource dependency validation.
    /// </summary>
    public class ResourceDependencyResult
    {
        /// <summary>
        /// All issues found during validation.
        /// </summary>
        public IReadOnlyList<ResourceDependencyIssue> Issues { get; init; } = System.Array.Empty<ResourceDependencyIssue>();

        /// <summary>
        /// True if no errors were found (warnings are acceptable).
        /// </summary>
        public bool IsValid => !Issues.Any(i => i.Severity == IssueSeverity.Error);
    }
}
