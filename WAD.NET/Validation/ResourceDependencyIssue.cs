namespace WAD.NET.Validation
{
    /// <summary>
    /// Represents a single cross-resource dependency issue found during validation.
    /// </summary>
    public class ResourceDependencyIssue
    {
        /// <summary>
        /// The severity of the issue.
        /// </summary>
        public IssueSeverity Severity { get; init; }

        /// <summary>
        /// The category of resource involved: "Sprite", "Sound", or "Patch".
        /// </summary>
        public string Category { get; init; } = string.Empty;

        /// <summary>
        /// The source lump or script that references the missing resource.
        /// </summary>
        public string Source { get; init; } = string.Empty;

        /// <summary>
        /// The name of the missing referenced resource.
        /// </summary>
        public string ReferencedName { get; init; } = string.Empty;

        /// <summary>
        /// A human-readable description of the issue.
        /// </summary>
        public string Message { get; init; } = string.Empty;
    }
}
