using System.Collections.Generic;
using System.Linq;
using WAD.NET.ZScript;
using WAD.NET.ZScript.Emit;
using WAD.NET.ZScript.Semantics;
using WAD.NET.ZScript.Syntax;
using Xunit;

namespace WAD.NET.Tests.ZScript;

public class CSharpEmitterTests
{
    [Fact]
    public void Emit_SimpleClass_GeneratesClassDeclaration()
    {
        var source = @"
class SimpleMonster : Actor
{
    Default
    {
        Health 100;
        Speed 8;
        +ISMONSTER
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);
        var emitter = new CSharpEmitter(model);

        var csharp = emitter.Emit(unit);

        Assert.Contains("public class SimpleMonster : Actor", csharp);
        Assert.Contains("public int Health { get; set; } = 100", csharp);
        Assert.Contains("public int Speed { get; set; } = 8", csharp);
    }

    [Fact]
    public void Emit_ClassWithStates_GeneratesDefineStatesMethod()
    {
        var source = @"
class TestActor : Actor
{
    States
    {
    Spawn:
        POSS AB 4
        Loop
    Death:
        POSS H 5
        Stop
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);
        var emitter = new CSharpEmitter(model);

        var csharp = emitter.Emit(unit);

        Assert.Contains("protected override void DefineStates()", csharp);
        Assert.Contains("DefineLabel(\"Spawn\")", csharp);
        Assert.Contains("AddFrame(\"POSS\", \"AB\", 4)", csharp);
        Assert.Contains("Loop();", csharp);
        Assert.Contains("DefineLabel(\"Death\")", csharp);
        Assert.Contains("Stop();", csharp);
    }

    [Fact]
    public void Emit_ClassWithBaseType_GeneratesInheritance()
    {
        var source = @"class MyWeapon : Weapon { }";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);
        var emitter = new CSharpEmitter(model);

        var csharp = emitter.Emit(unit);

        Assert.Contains("public class MyWeapon : Weapon", csharp);
    }

    [Fact]
    public void Emit_EmptyClass_GeneratesMinimalShell()
    {
        var source = @"class EmptyActor : Actor { }";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);
        var emitter = new CSharpEmitter(model);

        var csharp = emitter.Emit(unit);

        Assert.Contains("public class EmptyActor : Actor", csharp);
        Assert.Contains("{", csharp);
        Assert.Contains("}", csharp);
    }

    [Fact]
    public void Emit_Header_IncludesUsingStatements()
    {
        var source = @"class Test : Actor { }";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);
        var emitter = new CSharpEmitter(model);

        var csharp = emitter.Emit(unit);

        Assert.Contains("// Auto-generated from ZScript", csharp);
        Assert.Contains("using System;", csharp);
        Assert.Contains("using ZDoom.Runtime;", csharp);
    }

    [Fact]
    public void Emit_StateWithAction_GeneratesActionCallback()
    {
        var source = @"
class TestActor : Actor
{
    States
    {
    Spawn:
        POSS A 4 A_Look
        Loop
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);
        var emitter = new CSharpEmitter(model);

        var csharp = emitter.Emit(unit);

        Assert.Contains("A_Look()", csharp);
    }

    [Fact]
    public void Emit_StateGoto_GeneratesGotoCall()
    {
        var source = @"
class TestActor : Actor
{
    States
    {
    Spawn:
        POSS A 4
        Goto See
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);
        var emitter = new CSharpEmitter(model);

        var csharp = emitter.Emit(unit);

        Assert.Contains("Goto(\"See\", 0)", csharp);
    }

    [Fact]
    public void Emit_ActorDeclaration_EmitsAsClass()
    {
        var source = @"
actor MyZombie : ZombieMan replaces ZombieMan 1001
{
    Health 50;
    +ISMONSTER;
    States
    {
    Spawn:
        POSS AB 4
        Loop
    }
}";
        var parser = new Parser(source, ScriptLanguage.Decorate);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);
        var emitter = new CSharpEmitter(model);

        var csharp = emitter.Emit(unit);

        Assert.Contains("public class MyZombie : ZombieMan", csharp);
    }

    [Fact]
    public void Emit_TypeMapping_MapsCommonTypes()
    {
        var source = @"
class TestActor : Actor
{
    Default
    {
        Health 100;
        Obituary ""was killed"";
        Scale 1.5;
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);
        var emitter = new CSharpEmitter(model);

        var csharp = emitter.Emit(unit);

        Assert.Contains("public int Health { get; set; } = 100", csharp);
        Assert.Contains("public string Obituary { get; set; }", csharp);
        Assert.Contains("public float Scale { get; set; }", csharp);
    }

    [Fact]
    public void Emit_BrightFrame_IncludesBrightFlag()
    {
        var source = @"
class TestActor : Actor
{
    States
    {
    Spawn:
        POSS A 4 Bright
        Loop
    }
}";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);
        var emitter = new CSharpEmitter(model);

        var csharp = emitter.Emit(unit);

        Assert.Contains("bright: true", csharp);
    }
}
