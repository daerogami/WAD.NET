namespace WAD.NET.Validation
{
    /// <summary>
    /// Severity level for resource dependency validation issues.
    /// </summary>
    public enum IssueSeverity
    {
        /// <summary>
        /// A potential problem that may not cause failures (e.g., missing sprites that could come from the engine).
        /// </summary>
        Warning,

        /// <summary>
        /// A definite problem that will cause visual or functional errors (e.g., missing patches).
        /// </summary>
        Error
    }
}
