using System;
using System.Collections.Generic;
using System.Linq;

namespace WAD.NET.Validation
{
    /// <summary>
    /// Represents a single validation message (error or warning).
    /// </summary>
    public class ValidationMessage
    {
        /// <summary>
        /// A unique code identifying the type of validation issue.
        /// </summary>
        /// <remarks>
        /// Codes follow the format: CATEGORY_ISSUE
        /// Examples: MAP_MISSING_LUMPS, LINEDEF_INVALID_VERTEX, MARKER_UNPAIRED
        /// </remarks>
        public string Code { get; }

        /// <summary>
        /// A human-readable description of the validation issue.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The name of the lump associated with this message, if applicable.
        /// </summary>
        public string? LumpName { get; }

        /// <summary>
        /// The name of the map associated with this message, if applicable.
        /// </summary>
        public string? MapName { get; }

        /// <summary>
        /// Additional context for the validation message.
        /// </summary>
        public string? Context { get; }

        /// <summary>
        /// Creates a new validation message.
        /// </summary>
        /// <param name="code">The validation code.</param>
        /// <param name="message">The human-readable message.</param>
        /// <param name="lumpName">Optional lump name.</param>
        /// <param name="mapName">Optional map name.</param>
        /// <param name="context">Optional additional context.</param>
        public ValidationMessage(string code, string message, string? lumpName = null, string? mapName = null, string? context = null)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            LumpName = lumpName;
            MapName = mapName;
            Context = context;
        }

        /// <summary>
        /// Returns a string representation of the validation message.
        /// </summary>
        public override string ToString()
        {
            var parts = new List<string> { $"[{Code}]" };

            if (!string.IsNullOrEmpty(MapName))
                parts.Add($"Map '{MapName}':");

            if (!string.IsNullOrEmpty(LumpName))
                parts.Add($"Lump '{LumpName}':");

            parts.Add(Message);

            if (!string.IsNullOrEmpty(Context))
                parts.Add($"({Context})");

            return string.Join(" ", parts);
        }
    }

    /// <summary>
    /// Represents the result of validating a WAD file.
    /// </summary>
    public class ValidationResult
    {
        private readonly List<ValidationMessage> _errors;
        private readonly List<ValidationMessage> _warnings;

        /// <summary>
        /// Gets whether the WAD is valid (has no errors).
        /// </summary>
        /// <remarks>
        /// A WAD is considered valid if it has no errors.
        /// Warnings do not affect validity.
        /// </remarks>
        public bool IsValid => _errors.Count == 0;

        /// <summary>
        /// Gets the validation errors.
        /// </summary>
        public IReadOnlyList<ValidationMessage> Errors => _errors;

        /// <summary>
        /// Gets the validation warnings.
        /// </summary>
        public IReadOnlyList<ValidationMessage> Warnings => _warnings;

        /// <summary>
        /// Gets the total number of issues (errors + warnings).
        /// </summary>
        public int TotalIssueCount => _errors.Count + _warnings.Count;

        /// <summary>
        /// Gets the number of errors.
        /// </summary>
        public int ErrorCount => _errors.Count;

        /// <summary>
        /// Gets the number of warnings.
        /// </summary>
        public int WarningCount => _warnings.Count;

        /// <summary>
        /// Creates a new empty validation result.
        /// </summary>
        internal ValidationResult()
        {
            _errors = new List<ValidationMessage>();
            _warnings = new List<ValidationMessage>();
        }

        /// <summary>
        /// Creates a validation result with the specified errors and warnings.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        /// <param name="warnings">The validation warnings.</param>
        public ValidationResult(IEnumerable<ValidationMessage>? errors, IEnumerable<ValidationMessage>? warnings)
        {
            _errors = errors?.ToList() ?? new List<ValidationMessage>();
            _warnings = warnings?.ToList() ?? new List<ValidationMessage>();
        }

        /// <summary>
        /// Adds an error to the validation result.
        /// </summary>
        internal void AddError(ValidationMessage error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));
            _errors.Add(error);
        }

        /// <summary>
        /// Adds an error to the validation result.
        /// </summary>
        internal void AddError(string code, string message, string? lumpName = null, string? mapName = null, string? context = null)
        {
            _errors.Add(new ValidationMessage(code, message, lumpName, mapName, context));
        }

        /// <summary>
        /// Adds a warning to the validation result.
        /// </summary>
        internal void AddWarning(ValidationMessage warning)
        {
            if (warning == null) throw new ArgumentNullException(nameof(warning));
            _warnings.Add(warning);
        }

        /// <summary>
        /// Adds a warning to the validation result.
        /// </summary>
        internal void AddWarning(string code, string message, string? lumpName = null, string? mapName = null, string? context = null)
        {
            _warnings.Add(new ValidationMessage(code, message, lumpName, mapName, context));
        }

        /// <summary>
        /// Merges another validation result into this one.
        /// </summary>
        /// <param name="other">The other validation result to merge.</param>
        internal void Merge(ValidationResult other)
        {
            if (other == null) return;
            _errors.AddRange(other.Errors);
            _warnings.AddRange(other.Warnings);
        }

        /// <summary>
        /// Creates a successful validation result with no errors or warnings.
        /// </summary>
        public static ValidationResult Success() => new ValidationResult();

        /// <summary>
        /// Creates a validation result with a single error.
        /// </summary>
        public static ValidationResult WithError(string code, string message, string? lumpName = null, string? mapName = null)
        {
            var result = new ValidationResult();
            result.AddError(code, message, lumpName, mapName);
            return result;
        }

        /// <summary>
        /// Returns a summary string of the validation result.
        /// </summary>
        public override string ToString()
        {
            if (IsValid && _warnings.Count == 0)
                return "Validation passed: No errors or warnings.";

            return $"Validation {(IsValid ? "passed with warnings" : "failed")}: {_errors.Count} error(s), {_warnings.Count} warning(s).";
        }
    }
}
