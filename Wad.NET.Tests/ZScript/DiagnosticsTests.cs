using System.Collections.Generic;
using WAD.NET.ZScript.Diagnostics;
using WAD.NET.ZScript.Semantics;
using WAD.NET.ZScript.Syntax;
using Xunit;

namespace WAD.NET.Tests.ZScript;

public class DiagnosticsTests
{
    [Fact]
    public void DiagnosticDescriptor_StoresProperties()
    {
        var descriptor = new DiagnosticDescriptor(
            "ZS9999",
            "Test Title",
            "Test message {0}",
            "Testing",
            DiagnosticSeverity.Warning);

        Assert.Equal("ZS9999", descriptor.Id);
        Assert.Equal("Test Title", descriptor.Title);
        Assert.Equal("Test message {0}", descriptor.MessageFormat);
        Assert.Equal("Testing", descriptor.Category);
        Assert.Equal(DiagnosticSeverity.Warning, descriptor.DefaultSeverity);
    }

    [Fact]
    public void WeaponStateAnalyzer_DetectsMissingFireState()
    {
        var source = @"
class Weapon : Actor { }
class BrokenWeapon : Weapon
{
    States
    {
    Ready:
        PISG A 1 A_WeaponReady
        Loop
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var analyzer = new WeaponStateAnalyzer();
        var diagnostics = new List<Diagnostic>();
        analyzer.Analyze(model, diagnostics.Add);

        Assert.Single(diagnostics);
        Assert.Equal("ZS1002", diagnostics[0].Id);
        Assert.Contains("Fire", diagnostics[0].Message);
    }

    [Fact]
    public void WeaponStateAnalyzer_DetectsMissingReadyState()
    {
        var source = @"
class Weapon : Actor { }
class BrokenWeapon : Weapon
{
    States
    {
    Fire:
        PISG A 3 A_FirePistol
        Goto Ready
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var analyzer = new WeaponStateAnalyzer();
        var diagnostics = new List<Diagnostic>();
        analyzer.Analyze(model, diagnostics.Add);

        Assert.Single(diagnostics);
        Assert.Equal("ZS1001", diagnostics[0].Id);
        Assert.Contains("Ready", diagnostics[0].Message);
    }

    [Fact]
    public void WeaponStateAnalyzer_DetectsBothMissingStates()
    {
        var source = @"
class Weapon : Actor { }
class BrokenWeapon : Weapon
{
    States
    {
    Spawn:
        TNT1 A 0
        Stop
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var analyzer = new WeaponStateAnalyzer();
        var diagnostics = new List<Diagnostic>();
        analyzer.Analyze(model, diagnostics.Add);

        Assert.Equal(2, diagnostics.Count);
        Assert.Contains(diagnostics, d => d.Id == "ZS1001");
        Assert.Contains(diagnostics, d => d.Id == "ZS1002");
    }

    [Fact]
    public void WeaponStateAnalyzer_NoWarningsForCompleteWeapon()
    {
        var source = @"
class Weapon : Actor { }
class GoodWeapon : Weapon
{
    States
    {
    Ready:
        PISG A 1 A_WeaponReady
        Loop
    Fire:
        PISG A 3 A_FirePistol
        Goto Ready
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var analyzer = new WeaponStateAnalyzer();
        var diagnostics = new List<Diagnostic>();
        analyzer.Analyze(model, diagnostics.Add);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void WeaponStateAnalyzer_IgnoresNonWeaponClasses()
    {
        var source = @"
class MyMonster : Actor
{
    States
    {
    Spawn:
        POSS AB 4
        Loop
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var analyzer = new WeaponStateAnalyzer();
        var diagnostics = new List<Diagnostic>();
        analyzer.Analyze(model, diagnostics.Add);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void WeaponStateAnalyzer_IgnoresBaseWeaponClass()
    {
        var source = @"
class Weapon : Actor { }";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var analyzer = new WeaponStateAnalyzer();
        var diagnostics = new List<Diagnostic>();
        analyzer.Analyze(model, diagnostics.Add);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void WeaponStateAnalyzer_SupportedDiagnostics_ReturnsBothDescriptors()
    {
        var analyzer = new WeaponStateAnalyzer();
        var descriptors = new List<DiagnosticDescriptor>(analyzer.SupportedDiagnostics);

        Assert.Equal(2, descriptors.Count);
        Assert.Contains(descriptors, d => d.Id == "ZS1001");
        Assert.Contains(descriptors, d => d.Id == "ZS1002");
    }
}
