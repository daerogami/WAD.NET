# Phase 10: Text Definition Parser Bug Fixes

## Overview

This phase fixes real bugs in the MAPINFO, SNDINFO, and DEHACKED parsers. These are structured data formats (key-value pairs, directives, sections), not programming languages. They do not need ASTs, syntax nodes, semantic models, or visitor patterns. The fixes are targeted at actual broken behavior.

## Priority: MEDIUM

These parsers are important for mod compatibility detection but less frequently used than DECORATE/ZScript.

---

## Bug Inventory

### MapInfoParser — 3 bugs

| # | Bug | Impact |
|---|-----|--------|
| 1 | Comment stripping via regex before tokenization destroys strings containing `//` or `/* */` (e.g., URLs) | Corrupt data on valid input |
| 2 | `mustconfirm` greedily consumes the next keyword as its message | Silent property loss |
| 3 | Episode parsing without braces can eat the next top-level keyword as the episode name | Silent data corruption |

### SndInfoParser — 2 bugs

| # | Bug | Impact |
|---|-----|--------|
| 4 | `$random` only works when braces are on the same line; multi-line `$random` blocks (common format) silently fail | Silent data loss |
| 5 | `/* */` block comment handling has multiple failure modes (wrong line skipping, interaction with `//` strip) | Parsing errors on valid input |

### DehackedParser — 4 bugs

| # | Bug | Impact |
|---|-----|--------|
| 6 | `ParseText` uses `AppendLine()` which appends `Environment.NewLine` (CRLF on Windows), but counts characters assuming LF. Text blocks split at wrong position on Windows. | Cross-platform data corruption |
| 7 | `ParseIntFromValue` uses `Convert.ToInt32` for hex values, which throws `OverflowException` on common flag values >= `0x80000000` | Crash on valid input |
| 8 | `ParseStrings`/`ParsePars`/`ParseCheats` section terminators don't check for all section types (`Ammo`, `Sound`, `Sprite`, `Text`, `Misc`), causing garbage entries when followed by those sections | Data corruption |
| 9 | `ParseCheatLine` returns `null!` and caller at line 118 adds it to `Cheats` list without null check | `NullReferenceException` downstream |

---

## Task 10.1: Shared Comment-Aware Tokenizer

**File**: `WAD.NET/Parsers/DefinitionTokenizer.cs`

MapInfoParser and SndInfoParser both need comment-aware tokenization that respects quoted strings. Extract a shared utility.

The tokenizer produces a `Queue<string>` (same interface the existing MapInfoParser uses), but handles comments correctly during tokenization instead of stripping them beforehand.

```csharp
namespace WAD.NET.Parsers;

/// <summary>
/// Tokenizes text definition formats (MAPINFO, SNDINFO) with proper comment
/// handling that respects quoted strings. Comments inside quotes are preserved.
/// </summary>
internal static class DefinitionTokenizer
{
    /// <summary>
    /// Tokenizes input into a queue of string tokens.
    /// Handles: double-quoted strings, single-quoted strings,
    /// // line comments, /* */ block comments, ; line comments (optional),
    /// punctuation ({, }, =, ,), and unquoted words.
    /// </summary>
    /// <param name="content">The text to tokenize.</param>
    /// <param name="semicolonComments">If true, ; starts a line comment.</param>
    /// <returns>Queue of tokens (quoted strings have quotes stripped).</returns>
    public static Queue<string> Tokenize(string content, bool semicolonComments = false)
    {
        // Single-pass character-level tokenization:
        // - When inside a quoted string: collect until closing quote, handle \" escapes
        // - When hitting //: skip to end of line
        // - When hitting /*: skip to */
        // - When hitting ; (if enabled): skip to end of line
        // - Otherwise: collect punctuation as single-char tokens, or words until whitespace/punctuation
    }
}
```

This is ~80 lines of straightforward character scanning. No trivia, no spans, no token kinds — just `Queue<string>`.

---

## Task 10.2: Fix MapInfoParser (Bugs 1-3)

**File**: `WAD.NET/Parsers/MapInfo/MapInfoParser.cs`

### Bug 1 fix: Replace `Tokenize()` method

Replace the existing `Tokenize()` method (which strips comments via regex then tokenizes) with a call to the shared `DefinitionTokenizer.Tokenize()`.

**Before** (lines 107-136):
```csharp
private Queue<string> Tokenize(string content)
{
    // Remove comments (BUG: destroys strings containing // or /* */)
    content = Regex.Replace(content, @"//[^\n]*", "");
    content = Regex.Replace(content, @"/\*[\s\S]*?\*/", "");
    // ... regex tokenization
}
```

**After**:
```csharp
private Queue<string> Tokenize(string content)
{
    return DefinitionTokenizer.Tokenize(content);
}
```

### Bug 2 fix: `mustconfirm` greedy consume

**Before** (line 470):
```csharp
case "mustconfirm":
    def.MustConfirm = true;
    if (_tokens.Count > 0 && PeekToken() != "}")
        def.MustConfirmMessage = ParseString();
    break;
```

**After**: Only consume the next token as a message if it looks like a string value (quoted), not a keyword:
```csharp
case "mustconfirm":
    def.MustConfirm = true;
    if (_tokens.Count > 0 && PeekToken() != "}" && !IsPropertyKeyword(PeekToken()))
        def.MustConfirmMessage = ParseString();
    break;
```

Add helper method `IsPropertyKeyword()` that checks if a token matches any known MAPINFO keyword (the existing switch keys).

### Bug 3 fix: Episode name eating

**Before** (lines 400-418): The loop before the `{` can consume any non-specific token as the episode name.

**After**: Only consume a name token if it's a quoted string, not an unquoted word that might be the next top-level keyword:
```csharp
// After consuming StartMap:
if (_tokens.Count > 0)
{
    var next = PeekToken();
    if (next == "{")
    {
        // Block follows, parse it
    }
    else if (next?.StartsWith("\"") == true || IsQuotedValue(next))
    {
        // Explicit name before block
        def.Name = ParseString();
    }
    // Otherwise: no name, let the next iteration handle whatever comes next
}
```

Actually, the simpler fix: the tokenizer already strips quotes from string literals, so we can't check for quotes. Instead, check if the next token is a known top-level keyword before consuming it as a name.

---

## Task 10.3: Fix SndInfoParser (Bugs 4-5)

**File**: `WAD.NET/Parsers/SndInfo/SndInfoParser.cs`

Replace the line-by-line approach with token-based parsing using `DefinitionTokenizer`. This is a more significant change than the MapInfo fix because the current architecture (line splitting) is fundamentally unable to handle multi-line `$random` blocks.

### Approach

Replace the `Parse(string content)` method internals. Keep the same public signature: `public SndInfo Parse(string content)`.

Instead of splitting on newlines and processing line-by-line:
1. Tokenize using `DefinitionTokenizer.Tokenize(content, semicolonComments: true)`
2. Process tokens from the queue (same pattern as MapInfoParser)
3. For `$random`: consume tokens until `}` — works regardless of line boundaries
4. For other directives: consume the expected number of argument tokens

This fixes both bugs:
- **Bug 4**: Multi-line `$random` works because braces are proper tokens, not regex-matched within a line
- **Bug 5**: Block comments handled correctly during tokenization

### Changes

The main parsing loop changes from:
```csharp
for (int i = 0; i < lines.Length; i++)
{
    var line = lines[i].Trim();
    // strip comments, split, dispatch...
}
```

To:
```csharp
var tokens = DefinitionTokenizer.Tokenize(content, semicolonComments: true);
while (tokens.Count > 0)
{
    var token = tokens.Dequeue();
    if (token.StartsWith("$"))
        ParseDirective(token, tokens, info);
    else if (tokens.Count > 0)
        info.Sounds[token.ToLowerInvariant()] = tokens.Dequeue().ToUpperInvariant();
}
```

The `ParseDirective` method takes the token queue instead of a pre-split line, and `ParseRandom` reads tokens until `}`:
```csharp
private void ParseRandom(Queue<string> tokens, SndInfo info)
{
    var name = tokens.Dequeue().ToLowerInvariant();
    if (tokens.Count > 0 && tokens.Peek() == "{")
        tokens.Dequeue(); // consume {

    var sounds = new List<string>();
    while (tokens.Count > 0 && tokens.Peek() != "}")
        sounds.Add(tokens.Dequeue().ToLowerInvariant());

    if (tokens.Count > 0)
        tokens.Dequeue(); // consume }

    info.RandomSounds[name] = sounds.ToArray();
}
```

---

## Task 10.4: Fix DehackedParser (Bugs 6-9)

**File**: `WAD.NET/Parsers/Dehacked/DehackedParser.cs`

These are all targeted single-method fixes. No architectural change needed — the line-oriented approach is appropriate for DEH.

### Bug 6 fix: Cross-platform `ParseText`

**Before** (lines 416-444): Uses `StringBuilder.AppendLine()` which appends platform-specific line endings.

**After**: Use `Append()` with explicit `\n` to match the character counting:
```csharp
while (_lineIndex < _lines.Length && charsRead < totalChars)
{
    var line = _lines[_lineIndex];
    textBuilder.Append(line);
    textBuilder.Append('\n');
    charsRead += line.Length + 1;
    _lineIndex++;
}
```

### Bug 7 fix: Hex overflow

**Before** (lines 666-669):
```csharp
if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
{
    return Convert.ToInt32(value, 16);
}
```

**After**: Parse as unsigned first, then cast to int (preserving the bit pattern):
```csharp
if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
{
    return unchecked((int)Convert.ToUInt32(value, 16));
}
```

### Bug 8 fix: Complete section terminators

Extract a shared method to check for section-starting lines:
```csharp
private static bool IsSectionStart(string line)
{
    return line.StartsWith("[") ||
           line.StartsWith("Thing ", StringComparison.OrdinalIgnoreCase) ||
           line.StartsWith("Frame ", StringComparison.OrdinalIgnoreCase) ||
           line.StartsWith("Weapon ", StringComparison.OrdinalIgnoreCase) ||
           line.StartsWith("Ammo ", StringComparison.OrdinalIgnoreCase) ||
           line.StartsWith("Sound ", StringComparison.OrdinalIgnoreCase) ||
           line.StartsWith("Sprite ", StringComparison.OrdinalIgnoreCase) ||
           line.StartsWith("Text ", StringComparison.OrdinalIgnoreCase) ||
           line.StartsWith("Pointer ", StringComparison.OrdinalIgnoreCase) ||
           (line.StartsWith("Misc", StringComparison.OrdinalIgnoreCase) &&
            (line.Length == 4 || line[4] == ' ')) ||
           line.StartsWith("Cheat ", StringComparison.OrdinalIgnoreCase) ||
           line.StartsWith("Patch File", StringComparison.OrdinalIgnoreCase);
}
```

Replace all four copy-pasted section terminator checks with calls to `IsSectionStart()`.

For `ParseCodePointers`, also handle the special case where `Frame X = Y` is valid content (not a section start):
```csharp
if (IsSectionStart(line) && !(line.StartsWith("Frame ", StringComparison.OrdinalIgnoreCase) && line.Contains("=")))
    break;
```

### Bug 9 fix: Null cheat

**Before** (line 118):
```csharp
patch.Cheats.Add(ParseCheatLine(line));
```

**After**:
```csharp
var cheat = ParseCheatLine(line);
if (cheat != null)
    patch.Cheats.Add(cheat);
```

Also fix `ParseCheatLine` return type annotation: change `return null!;` to `return null;` and make the return type `DehCheat?`.

---

## Task 10.5: Tests

**File**: `Wad.NET.Tests/SourcePortExtensionsTests.cs` (add to existing test class)

Add targeted regression tests for the fixed bugs:

```csharp
#region Parser Bug Fix Tests

// Bug 1: Comments in strings
[Fact]
public void MapInfoParser_ShouldPreserveUrlsInStrings()
{
    var mapinfo = @"
map MAP01 ""http://example.com/mymap""
{
    par = 30
}";
    var parser = new MapInfoParser(mapinfo);
    var info = parser.Parse();
    Assert.Equal("http://example.com/mymap", info.Maps["MAP01"].NiceName);
}

// Bug 4: Multi-line $random
[Fact]
public void SndInfoParser_ShouldParseMultiLineRandom()
{
    var sndinfo = @"
$random weapons/shotgun {
    weapons/shotgun1
    weapons/shotgun2
    weapons/shotgun3
}
";
    var parser = new SndInfoParser();
    var info = parser.Parse(sndinfo);
    Assert.True(info.RandomSounds.ContainsKey("weapons/shotgun"));
    Assert.Equal(3, info.RandomSounds["weapons/shotgun"].Length);
}

// Bug 5: Block comments
[Fact]
public void SndInfoParser_ShouldHandleBlockComments()
{
    var sndinfo = @"
pistol DSPISTOL
/* this is
   a multi-line
   comment */
shotgn DSSHOTGN
";
    var parser = new SndInfoParser();
    var info = parser.Parse(sndinfo);
    Assert.Equal(2, info.Sounds.Count);
}

// Bug 7: Hex overflow
[Fact]
public void DehackedParser_ShouldHandleLargeHexValues()
{
    var deh = @"
Thing 1 (Test)
Bits = 0x80000000
";
    var parser = new DehackedParser(deh);
    var patch = parser.Parse();
    Assert.Equal(unchecked((uint)0x80000000), patch.Things[0].Flags);
}

// Bug 8: Section terminators
[Fact]
public void DehackedParser_ShouldStopStringsSectionAtAmmo()
{
    var deh = @"
[STRINGS]
HUSTR_E1M1 = Hangar

Ammo 0
Max ammo = 200
";
    var parser = new DehackedParser(deh);
    var patch = parser.Parse();
    Assert.Single(patch.Strings);
    Assert.Equal("HUSTR_E1M1", patch.Strings[0].Mnemonic);
    Assert.Single(patch.Ammo);
}

#endregion
```

---

## Implementation Order

1. **DefinitionTokenizer** — shared utility (~80 lines)
2. **MapInfoParser fixes** — replace Tokenize(), fix mustconfirm, fix episode name
3. **SndInfoParser fixes** — replace internals with token-based parsing
4. **DehackedParser fixes** — four targeted single-method fixes
5. **Tests** — regression tests for each bug
6. **Build & verify** — all existing tests still pass

## Files Changed

| # | File | Change |
|---|------|--------|
| 1 | `WAD.NET/Parsers/DefinitionTokenizer.cs` | **New**: shared tokenizer (~80 lines) |
| 2 | `WAD.NET/Parsers/MapInfo/MapInfoParser.cs` | **Modified**: replace Tokenize(), fix mustconfirm, fix episode |
| 3 | `WAD.NET/Parsers/SndInfo/SndInfoParser.cs` | **Modified**: replace internals with token-based parsing |
| 4 | `WAD.NET/Parsers/Dehacked/DehackedParser.cs` | **Modified**: 4 targeted bug fixes |
| 5 | `Wad.NET.Tests/SourcePortExtensionsTests.cs` | **Modified**: add regression tests |

**Total**: 1 new file, 4 modified files. No new directories, no new namespaces.

## Verification

1. `dotnet build WAD.NET.sln` — no errors
2. `dotnet test Wad.NET.Tests/Wad.NET.Tests.csproj` — all existing tests pass
3. New regression tests pass
