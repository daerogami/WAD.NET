using System.Linq;
using WAD.NET.ZScript;
using WAD.NET.ZScript.Semantics;
using WAD.NET.ZScript.Syntax;
using Xunit;

namespace WAD.NET.Tests.ZScript;

public class SemanticModelTests
{
    // ------------------------------------------------------------------
    // Inheritance resolution
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldResolveInheritance()
    {
        var source = @"
class BaseActor { }
class Demon : BaseActor { }
class PinkyDemon : Demon { }
class Spectre : PinkyDemon { }
";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var spectre = model.LookupClass("Spectre");
        Assert.NotNull(spectre);
        var chain = model.GetInheritanceChain(spectre!).Select(c => c.Name).ToList();
        Assert.Equal(new[] { "Spectre", "PinkyDemon", "Demon", "BaseActor" }, chain);
    }

    [Fact]
    public void ShouldResolveCaseInsensitiveClassLookup()
    {
        var source = @"class MyActor : Actor { }";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        Assert.NotNull(model.LookupClass("myactor"));
        Assert.NotNull(model.LookupClass("MYACTOR"));
        Assert.NotNull(model.LookupClass("MyActor"));
    }

    // ------------------------------------------------------------------
    // Weapon detection
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldDetectWeapons()
    {
        var source = @"
class Weapon : Actor { }
class MyShotgun : Weapon
{
    Default
    {
        Weapon.SlotNumber 3;
    }
    States
    {
    Ready:
        SHTG A 1 A_WeaponReady
        Loop
    Fire:
        SHTG A 3 A_FireShotgun
        Goto Ready
    }
}
";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var weapon = model.LookupClass("MyShotgun");
        Assert.NotNull(weapon);
        Assert.True(model.IsWeapon(weapon!));
    }

    [Fact]
    public void ShouldNotDetectNonWeaponAsWeapon()
    {
        var source = @"class MyMonster : SomeBase { }";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var monster = model.LookupClass("MyMonster");
        Assert.NotNull(monster);
        Assert.False(model.IsWeapon(monster!));
    }

    // ------------------------------------------------------------------
    // Property extraction
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldExtractProperties()
    {
        var source = @"
class MyMonster : Actor
{
    Default
    {
        Health 100;
        Speed 8;
        +ISMONSTER;
        +SOLID;
    }
}
";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var monster = model.LookupClass("MyMonster");
        Assert.NotNull(monster);
        Assert.Equal(100, monster!.Properties["Health"]);
        Assert.Equal(8, monster.Properties["Speed"]);
        Assert.True(monster.HasFlag("ISMONSTER"));
        Assert.True(monster.HasFlag("SOLID"));
    }

    [Fact]
    public void ShouldExtractPrefixedProperties()
    {
        var source = @"
class Weapon : Actor { }
class MyGun : Weapon
{
    Default
    {
        Weapon.SlotNumber 2;
        Weapon.AmmoType ""Clip"";
    }
}
";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var gun = model.LookupClass("MyGun");
        Assert.NotNull(gun);
        Assert.True(gun!.Properties.ContainsKey("Weapon.SlotNumber"));
        Assert.Equal(2, gun.Properties["Weapon.SlotNumber"]);
    }

    [Fact]
    public void ShouldHandleCaseInsensitiveProperties()
    {
        var source = @"
class MyActor : Actor
{
    Default
    {
        Health 50;
    }
}
";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var actor = model.LookupClass("MyActor");
        Assert.NotNull(actor);
        Assert.Equal(50, actor!.Properties["health"]);
        Assert.Equal(50, actor.Properties["HEALTH"]);
    }

    [Fact]
    public void ShouldHandleCaseInsensitiveFlags()
    {
        var source = @"
class MyActor : Actor
{
    Default
    {
        +SOLID;
    }
}
";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var actor = model.LookupClass("MyActor");
        Assert.NotNull(actor);
        Assert.True(actor!.HasFlag("solid"));
        Assert.True(actor.HasFlag("SOLID"));
        Assert.True(actor.HasFlag("Solid"));
    }

    // ------------------------------------------------------------------
    // State extraction
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldExtractStateSprites()
    {
        var source = @"
class TestMonster : Actor
{
    States
    {
    Spawn:
        POSS AB 4
        Loop
    See:
        POSS AABBCCDD 4
        Loop
    }
}
";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var monster = model.LookupClass("TestMonster");
        Assert.NotNull(monster);
        Assert.Equal(2, monster!.States.Count);
        Assert.Equal("Spawn", monster.States[0].Label);
        Assert.Equal("POSS", monster.States[0].Frames[0].SpriteName);
        Assert.Equal("AB", monster.States[0].Frames[0].FrameLetters);
        Assert.Equal("See", monster.States[1].Label);
    }

    [Fact]
    public void ShouldExtractStateActionNames()
    {
        var source = @"
class Weapon : Actor { }
class TestWeapon : Weapon
{
    States
    {
    Fire:
        SHTG A 3 A_FireShotgun
        SHTG B 2 Bright
        Goto Ready
    }
}
";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var weapon = model.LookupClass("TestWeapon");
        Assert.NotNull(weapon);
        Assert.Single(weapon!.States);
        Assert.Equal("Fire", weapon.States[0].Label);
        Assert.Equal(2, weapon.States[0].Frames.Count);
        Assert.Equal("A_FireShotgun", weapon.States[0].Frames[0].ActionName);
        Assert.Null(weapon.States[0].Frames[1].ActionName);
        Assert.True(weapon.States[0].Frames[1].IsBright);
    }

    // ------------------------------------------------------------------
    // Replacers
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldFindReplacers()
    {
        var source = @"
class NewZombie : ZombieMan replaces ZombieMan { }
";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var replacers = model.GetReplacers("ZombieMan").ToList();
        Assert.Single(replacers);
        Assert.Equal("NewZombie", replacers[0].Name);
    }

    [Fact]
    public void ShouldFindReplacersCaseInsensitive()
    {
        var source = @"
class CustomZombie : ZombieMan replaces ZombieMan { }
";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var replacers = model.GetReplacers("zombieman").ToList();
        Assert.Single(replacers);
        Assert.Equal("CustomZombie", replacers[0].Name);
    }

    [Fact]
    public void ShouldReturnEmptyWhenNoReplacers()
    {
        var source = @"class MyActor : Actor { }";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var replacers = model.GetReplacers("ZombieMan").ToList();
        Assert.Empty(replacers);
    }

    // ------------------------------------------------------------------
    // DECORATE actor parsing
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldParseDecorateActorSemantics()
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
}
";
        var parser = new Parser(source, ScriptLanguage.Decorate);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var zombie = model.LookupClass("MyZombie");
        Assert.NotNull(zombie);
        Assert.Equal("ZombieMan", zombie!.BaseTypeName);
        Assert.Equal("ZombieMan", zombie.ReplacesName);
        Assert.Equal(1001, zombie.DoomEdNumber);
        Assert.Equal(50, zombie.Properties["Health"]);
        Assert.True(zombie.HasFlag("ISMONSTER"));
        Assert.Single(zombie.States);
        Assert.Equal("Spawn", zombie.States[0].Label);
    }

    [Fact]
    public void ShouldParseDecorateActorWithMultipleStates()
    {
        var source = @"
actor TestActor : Actor 5001
{
    Health 200;
    Radius 20;
    Height 56;
    +SOLID;
    +SHOOTABLE;
    -NOGRAVITY;
    States
    {
    Spawn:
        PLAY A 10
        Loop
    Death:
        PLAY H 5
        PLAY I 5
        PLAY J 5
        PLAY K -1
        Stop
    }
}
";
        var parser = new Parser(source, ScriptLanguage.Decorate);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var actor = model.LookupClass("TestActor");
        Assert.NotNull(actor);
        Assert.Equal(5001, actor!.DoomEdNumber);
        Assert.Equal(200, actor.Properties["Health"]);
        Assert.Equal(20, actor.Properties["Radius"]);
        Assert.Equal(56, actor.Properties["Height"]);
        Assert.True(actor.HasFlag("SOLID"));
        Assert.True(actor.HasFlag("SHOOTABLE"));
        Assert.False(actor.HasFlag("NOGRAVITY"));
        Assert.Equal(2, actor.States.Count);
        Assert.Equal("Spawn", actor.States[0].Label);
        Assert.Equal("Death", actor.States[1].Label);
        Assert.Equal(4, actor.States[1].Frames.Count);
    }

    // ------------------------------------------------------------------
    // Monster detection
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldDetectMonsterWithFlag()
    {
        var source = @"
class MyMonster : SomeBase
{
    Default
    {
        +ISMONSTER;
    }
}
";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var monster = model.LookupClass("MyMonster");
        Assert.NotNull(monster);
        Assert.True(model.IsMonster(monster!));
    }

    [Fact]
    public void ShouldNotDetectNonMonster()
    {
        var source = @"
class MyItem : SomeBase
{
    Default
    {
        +SOLID;
    }
}
";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var item = model.LookupClass("MyItem");
        Assert.NotNull(item);
        Assert.False(model.IsMonster(item!));
    }

    // ------------------------------------------------------------------
    // Methods and fields
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldExtractMethods()
    {
        var source = @"
class MyActor : Actor
{
    virtual void DoSomething() { }
    override void Tick() { }
    int GetHealth() { }
}
";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var actor = model.LookupClass("MyActor");
        Assert.NotNull(actor);
        Assert.Equal(3, actor!.Methods.Count);

        var doSomething = actor.Methods[0];
        Assert.Equal("DoSomething", doSomething.Name);
        Assert.True(doSomething.IsVirtual);
        Assert.False(doSomething.IsOverride);

        var tick = actor.Methods[1];
        Assert.Equal("Tick", tick.Name);
        Assert.True(tick.IsOverride);
    }

    [Fact]
    public void ShouldExtractFields()
    {
        var source = @"
class MyActor : Actor
{
    int health;
    double speed;
}
";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var actor = model.LookupClass("MyActor");
        Assert.NotNull(actor);
        Assert.Equal(2, actor!.Fields.Count);
        Assert.Equal("health", actor.Fields[0].Name);
        Assert.Equal("speed", actor.Fields[1].Name);
    }

    // ------------------------------------------------------------------
    // Multiple compilation units
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldResolveAcrossMultipleUnits()
    {
        var source1 = @"class BaseActor : Actor { }";
        var source2 = @"class DerivedActor : BaseActor { }";

        var parser1 = new Parser(source1);
        var unit1 = parser1.ParseCompilationUnit();
        var parser2 = new Parser(source2);
        var unit2 = parser2.ParseCompilationUnit();

        var model = SemanticModel.Create(new[] { unit1, unit2 });

        var derived = model.LookupClass("DerivedActor");
        Assert.NotNull(derived);
        Assert.NotNull(derived!.BaseType);
        Assert.Equal("BaseActor", derived.BaseType!.Name);
    }

    // ------------------------------------------------------------------
    // GetDeclaredSymbol
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldGetDeclaredSymbolForClass()
    {
        var source = @"class MyActor : Actor { }";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var classNode = unit.Classes.First();
        var symbol = model.GetDeclaredSymbol(classNode);
        Assert.NotNull(symbol);
        Assert.Equal("MyActor", symbol!.Name);
        Assert.Equal(SymbolKind.Class, symbol.Kind);
    }

    // ------------------------------------------------------------------
    // DoomEdNumber
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldExtractDoomEdNumber()
    {
        var source = @"class MyActor : Actor replaces ZombieMan 3004 { }";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var actor = model.LookupClass("MyActor");
        Assert.NotNull(actor);
        Assert.Equal(3004, actor!.DoomEdNumber);
        Assert.Equal("ZombieMan", actor.ReplacesName);
    }

    // ------------------------------------------------------------------
    // Empty / edge cases
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldHandleEmptyClass()
    {
        var source = @"class EmptyActor : Actor { }";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var actor = model.LookupClass("EmptyActor");
        Assert.NotNull(actor);
        Assert.Empty(actor!.Methods);
        Assert.Empty(actor.Fields);
        Assert.Empty(actor.States);
        Assert.Empty(actor.Properties);
        Assert.Empty(actor.Flags);
    }

    [Fact]
    public void ShouldListAllClasses()
    {
        var source = @"
class A : Actor { }
class B : Actor { }
class C : B { }
";
        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();
        var model = new SemanticModel(unit);

        var allClasses = model.AllClasses.Select(c => c.Name).OrderBy(n => n).ToList();
        Assert.Contains("A", allClasses);
        Assert.Contains("B", allClasses);
        Assert.Contains("C", allClasses);
        // Actor is referenced but not declared in this source, so it may or may not be in AllClasses
    }
}
