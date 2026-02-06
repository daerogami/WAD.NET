using System;
using System.Collections.Generic;
using WAD.NET.ZScript.Semantics;
using WAD.NET.ZScript.Syntax;

namespace WAD.NET.ZScript.Diagnostics;

/// <summary>
/// Analyzes weapon classes for missing required states (Ready and Fire).
/// </summary>
public class WeaponStateAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor MissingReadyState = new DiagnosticDescriptor(
        "ZS1001",
        "Missing Ready state",
        "Weapon '{0}' is missing required 'Ready' state",
        "Completeness",
        DiagnosticSeverity.Error);

    private static readonly DiagnosticDescriptor MissingFireState = new DiagnosticDescriptor(
        "ZS1002",
        "Missing Fire state",
        "Weapon '{0}' is missing required 'Fire' state",
        "Completeness",
        DiagnosticSeverity.Error);

    /// <inheritdoc/>
    public override IEnumerable<DiagnosticDescriptor> SupportedDiagnostics =>
        new DiagnosticDescriptor[] { MissingReadyState, MissingFireState };

    /// <inheritdoc/>
    public override void Analyze(SemanticModel model, Action<Diagnostic> reportDiagnostic)
    {
        foreach (var cls in model.AllClasses)
        {
            if (!model.IsWeapon(cls))
                continue;

            // Don't flag the base Weapon class itself
            if (cls.Name.Equals("Weapon", StringComparison.OrdinalIgnoreCase))
                continue;

            bool hasReady = false;
            bool hasFire = false;

            foreach (var state in cls.States)
            {
                if (state.Label.Equals("Ready", StringComparison.OrdinalIgnoreCase))
                    hasReady = true;
                if (state.Label.Equals("Fire", StringComparison.OrdinalIgnoreCase))
                    hasFire = true;
            }

            var span = cls.DeclaringSyntax?.Span ?? default;

            if (!hasReady)
            {
                reportDiagnostic(new Diagnostic(
                    MissingReadyState.DefaultSeverity,
                    string.Format(MissingReadyState.MessageFormat, cls.Name),
                    span,
                    id: MissingReadyState.Id));
            }

            if (!hasFire)
            {
                reportDiagnostic(new Diagnostic(
                    MissingFireState.DefaultSeverity,
                    string.Format(MissingFireState.MessageFormat, cls.Name),
                    span,
                    id: MissingFireState.Id));
            }
        }
    }
}
