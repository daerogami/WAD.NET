# ZScript & DECORATE Parser

WAD.NET includes a full Roslyn-style parser for [ZScript](https://zdoom.org/wiki/ZScript) and [DECORATE](https://zdoom.org/wiki/DECORATE), the scripting languages used by [ZDoom](https://zdoom.org/wiki/ZDoom)-family source ports for defining actors, weapons, items, and game logic.

> **Background:** DECORATE was ZDoom's original actor definition language, introduced in the early 2000s. ZScript superseded it in GZDoom 2.4.0 (2017) as a full programming language with classes, types, and expressions. DECORATE is still widely used in existing mods but is considered legacy. New mods should use ZScript. See the [ZDoom Wiki - ZScript](https://zdoom.org/wiki/ZScript) for the language reference.

## Architecture

The parser follows the same design principles as [Roslyn](https://github.com/dotnet/roslyn) (the C# compiler):

```
Source text
    │
    ▼
  Lexer         Tokenizes source into SyntaxTokens (with trivia)
    │
    ▼
  Parser        Recursive descent, builds immutable syntax tree
    │
    ▼
  SyntaxNode    Full-fidelity AST (preserves whitespace and comments)
    │
    ├── SemanticModel    Symbol resolution, type checking, inheritance
    ├── DiagnosticAnalyzer    Extensible linting framework
    ├── SyntaxFormatter    Code formatting
    └── CSharpEmitter    Transpile to C#
```

Key properties:
- **Full fidelity** - Every character of the source is preserved in the tree (trivia = comments + whitespace)
- **Immutable** - AST nodes are immutable; modifications produce new trees via `SyntaxRewriter`
- **Round-trippable** - `node.ToFullString()` reproduces the exact original source

## Parsing

### Basic Parsing

```csharp
using WAD.NET.ZScript;
using WAD.NET.ZScript.Syntax;

string source = @"
class MyMonster : Actor
{
    Default
    {
        Health 200;
        Speed 8;
        Monster;
        +FLOORCLIP
    }

    States
    {
    Spawn:
        TROO AB 10 A_Look;
        Loop;
    See:
        TROO AABBCCDD 3 A_Chase;
        Loop;
    }
}";

var parser = new Parser(source, ScriptLanguage.ZScript);
CompilationUnitSyntax tree = parser.Parse();
```

### Parsing DECORATE

[DECORATE](https://zdoom.org/wiki/DECORATE) uses a different syntax style. Specify the language when creating the parser:

```csharp
string decorateSource = @"
ACTOR MyItem : CustomInventory
{
    Inventory.MaxAmount 1
    Inventory.PickupMessage ""You got the item!""
    States
    {
    Spawn:
        ITEM A -1
        Stop
    Pickup:
        TNT1 A 0 A_GiveInventory(""Shotgun"")
        Stop
    }
}";

var parser = new Parser(decorateSource, ScriptLanguage.DECORATE);
CompilationUnitSyntax tree = parser.Parse();
```

> **Note:** DECORATE is a legacy format. For new mods, the ZDoom community recommends [ZScript](https://zdoom.org/wiki/ZScript). DECORATE is still fully supported for parsing existing mods.

### Parser Diagnostics

The parser reports syntax errors without throwing exceptions:

```csharp
var parser = new Parser(source);
var tree = parser.Parse();

foreach (var diagnostic in parser.Diagnostics)
{
    Console.WriteLine($"{diagnostic.Severity} at {diagnostic.Span}: {diagnostic.Message}");
}
```

## AST Nodes

The syntax tree is composed of `SyntaxNode` types:

| Node | Description |
|------|-------------|
| `CompilationUnitSyntax` | Root node containing all declarations |
| `ClassDeclarationSyntax` | `class MyActor : Actor { ... }` |
| `VersionDirectiveSyntax` | `version "4.10.0"` |
| `IncludeDirectiveSyntax` | `#include "zscript/myfile.zs"` |
| `EnumDeclarationSyntax` | `enum MyEnum { ... }` |
| `StructDeclarationSyntax` | `struct MyStruct { ... }` |
| `ConstDefinitionSyntax` | `const MY_CONST = 42;` |
| `DefaultBlockSyntax` | The `Default { ... }` block with properties and flags |
| `StatesBlockSyntax` | The `States { ... }` block |
| `StateDeclarationSyntax` | A state label with its frames |
| `StateFrameSyntax` | `TROO AB 10 A_Look;` |
| `PropertySyntax` | `Health 200;` |
| `FlagSyntax` | `+FLOORCLIP` or `-SOLID` |
| `MethodDeclarationSyntax` | Method/function definitions |
| `FieldDeclarationSyntax` | Field/variable declarations |
| Various expression nodes | Literals, binary ops, calls, member access, etc. |
| Various statement nodes | If, for, while, return, expression statements, etc. |

### Traversing the Tree

Use `SyntaxVisitor` for read-only traversal:

```csharp
using WAD.NET.ZScript.Syntax;
using WAD.NET.ZScript.Syntax.Visitors;

// Collect all state labels
var collector = new StateLabelCollector();
collector.Visit(tree);
foreach (var label in collector.Labels)
{
    Console.WriteLine($"State: {label}");
}

// Find unused states
var unusedFinder = new UnusedStateFinder();
unusedFinder.Visit(tree);
```

### Rewriting the Tree

Use `SyntaxRewriter` for immutable tree transformations:

```csharp
// Rename an actor class
var renamer = new ActorRenamer("OldName", "NewName");
var newTree = renamer.Visit(tree);
string newSource = newTree.ToFullString();
```

Write custom visitors by subclassing `SyntaxVisitor` or `SyntaxVisitor<TResult>`:

```csharp
class ClassCounter : SyntaxVisitor
{
    public int Count { get; private set; }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        Count++;
        base.VisitClassDeclaration(node);
    }
}
```

## Semantic Analysis

The `SemanticModel` resolves symbols, inheritance, and types:

```csharp
using WAD.NET.ZScript.Semantics;

var model = new SemanticModel(tree);

// Look up a class by name
ClassSymbol? cls = model.GetClass("MyMonster");
if (cls != null)
{
    Console.WriteLine($"Class: {cls.Name}");
    Console.WriteLine($"Base: {cls.BaseClassName}");
    Console.WriteLine($"Methods: {cls.Methods.Count}");
    Console.WriteLine($"Fields: {cls.Fields.Count}");
    Console.WriteLine($"States: {cls.States.Count}");
}

// Get all classes
foreach (var classSymbol in model.AllClasses)
{
    Console.WriteLine($"{classSymbol.Name} : {classSymbol.BaseClassName}");
}
```

### Multi-File Analysis

For mods that span multiple files (common in large ZScript projects):

```csharp
var units = new List<CompilationUnitSyntax>();

foreach (var file in zscriptFiles)
{
    var parser = new Parser(File.ReadAllText(file));
    units.Add(parser.Parse());
}

// Combined symbol table resolves cross-file inheritance
var model = SemanticModel.Create(units);
```

## Diagnostics (Linting)

The diagnostic framework lets you write custom analyzers:

```csharp
using WAD.NET.ZScript.Diagnostics;

// Built-in analyzer: checks weapon state definitions
var analyzer = new WeaponStateAnalyzer();

var diagnostics = new List<Diagnostic>();
analyzer.Analyze(model, d => diagnostics.Add(d));

foreach (var d in diagnostics)
{
    Console.WriteLine($"[{d.Descriptor.Id}] {d.Severity}: {d.Message}");
}
```

### Writing Custom Analyzers

Subclass `DiagnosticAnalyzer`:

```csharp
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        "MY001", "My Rule", "Description of what this checks",
        DiagnosticSeverity.Warning);

    public override IEnumerable<DiagnosticDescriptor> SupportedDiagnostics
        => new[] { Rule };

    public override void Analyze(SemanticModel model, Action<Diagnostic> reportDiagnostic)
    {
        foreach (var cls in model.AllClasses)
        {
            // Your analysis logic here
            if (/* condition */)
            {
                reportDiagnostic(new Diagnostic(Rule, TextSpan.Empty, "Issue found"));
            }
        }
    }
}
```

## Code Formatting

Format ZScript source code consistently:

```csharp
using WAD.NET.ZScript.Formatting;

var formatter = new SyntaxFormatter(new FormattingOptions
{
    IndentSize = 4,
    UseTabs = true
});

string formatted = formatter.Format(tree);
```

## C# Transpilation

Convert ZScript to C# source code:

```csharp
using WAD.NET.ZScript.Emit;

var emitter = new CSharpEmitter(model);
string csharpCode = emitter.Emit(tree);

// Output is valid C# with ZDoom.Runtime using directives
Console.WriteLine(csharpCode);
```

This is experimental and useful for migration tooling or code analysis.

## Language References

- [ZDoom Wiki - ZScript](https://zdoom.org/wiki/ZScript) - Complete ZScript language reference
- [ZDoom Wiki - ZScript virtual functions](https://zdoom.org/wiki/ZScript_virtual_functions) - Overridable methods
- [ZDoom Wiki - ZScript classes](https://zdoom.org/wiki/ZScript_classes) - Class system and inheritance
- [ZDoom Wiki - ZScript types](https://zdoom.org/wiki/ZScript_types) - Type system
- [ZDoom Wiki - DECORATE](https://zdoom.org/wiki/DECORATE) - Legacy actor definition language
- [ZDoom Wiki - DECORATE expressions](https://zdoom.org/wiki/DECORATE_expressions) - DECORATE expression syntax
- [ZDoom Wiki - Actor](https://zdoom.org/wiki/Actor) - Base actor class reference
- [ZDoom Wiki - Actor states](https://zdoom.org/wiki/Actor_states) - State machine definitions
- [ZDoom Wiki - Action functions](https://zdoom.org/wiki/Action_functions) - Built-in action function reference
- [ZDoom Wiki - A_SpawnProjectile](https://zdoom.org/wiki/A_SpawnProjectile) - Example action function (spawning projectiles)
- [Doom Wiki - DeHackEd](https://doomwiki.org/wiki/DeHackEd) - The predecessor to DECORATE (binary patching)
