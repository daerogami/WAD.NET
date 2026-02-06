using WAD.NET.ZScript.Syntax;

namespace WAD.NET.ZScript.Diagnostics;

/// <summary>
/// The severity level of a diagnostic message.
/// </summary>
public enum DiagnosticSeverity
{
    Hidden,
    Info,
    Warning,
    Error
}

/// <summary>
/// Represents a source location (file path, line, and column).
/// </summary>
public class Location
{
    /// <summary>
    /// The file path where the diagnostic occurred.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// The one-based line number.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// The one-based column number.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Creates a new <see cref="Location"/> instance.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="line">The one-based line number.</param>
    /// <param name="column">The one-based column number.</param>
    public Location(string filePath, int line, int column)
    {
        FilePath = filePath;
        Line = line;
        Column = column;
    }
}

/// <summary>
/// Represents a compiler diagnostic (error, warning, or informational message).
/// </summary>
public class Diagnostic
{
    /// <summary>
    /// The severity of this diagnostic.
    /// </summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>
    /// The diagnostic identifier (e.g., "ZS0001").
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The human-readable diagnostic message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The span in the source text where the diagnostic applies.
    /// </summary>
    public TextSpan Span { get; }

    /// <summary>
    /// The optional source location (file, line, column).
    /// </summary>
    public Location? Location { get; }

    /// <summary>
    /// Creates a new <see cref="Diagnostic"/> instance.
    /// </summary>
    /// <param name="severity">The severity.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="span">The source span.</param>
    /// <param name="location">The optional source location.</param>
    /// <param name="id">The diagnostic identifier (defaults to "ZS0000").</param>
    public Diagnostic(
        DiagnosticSeverity severity,
        string message,
        TextSpan span,
        Location? location = null,
        string id = "ZS0000")
    {
        Severity = severity;
        Id = id;
        Message = message;
        Span = span;
        Location = location;
    }
}
