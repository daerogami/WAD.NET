using System;

namespace WAD.NET.ZScript.Diagnostics;

/// <summary>
/// Describes a category of diagnostic that can be reported.
/// </summary>
public class DiagnosticDescriptor
{
    /// <summary>
    /// The unique identifier for this diagnostic (e.g., "ZS1001").
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// A short title for the diagnostic.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// A composite format string for the diagnostic message (e.g., "Weapon '{0}' is missing ...").
    /// </summary>
    public string MessageFormat { get; }

    /// <summary>
    /// The default severity of this diagnostic.
    /// </summary>
    public DiagnosticSeverity DefaultSeverity { get; }

    /// <summary>
    /// The category of this diagnostic (e.g., "Completeness", "Design").
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Creates a new <see cref="DiagnosticDescriptor"/> instance.
    /// </summary>
    public DiagnosticDescriptor(
        string id,
        string title,
        string messageFormat,
        string category,
        DiagnosticSeverity defaultSeverity)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        MessageFormat = messageFormat ?? throw new ArgumentNullException(nameof(messageFormat));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        DefaultSeverity = defaultSeverity;
    }
}
