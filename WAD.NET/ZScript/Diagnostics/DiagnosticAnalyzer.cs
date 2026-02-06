using System;
using System.Collections.Generic;
using WAD.NET.ZScript.Semantics;

namespace WAD.NET.ZScript.Diagnostics;

/// <summary>
/// Abstract base class for implementing custom diagnostic analyzers.
/// </summary>
public abstract class DiagnosticAnalyzer
{
    /// <summary>
    /// The set of diagnostic descriptors that this analyzer can produce.
    /// </summary>
    public abstract IEnumerable<DiagnosticDescriptor> SupportedDiagnostics { get; }

    /// <summary>
    /// Analyzes the given semantic model and reports any diagnostics found.
    /// </summary>
    /// <param name="model">The semantic model to analyze.</param>
    /// <param name="reportDiagnostic">Callback to report each diagnostic found.</param>
    public abstract void Analyze(SemanticModel model, Action<Diagnostic> reportDiagnostic);
}
