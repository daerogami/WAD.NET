using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WAD.NET.Archives;
using WAD.NET.Enums;
using WAD.NET.Tests.Infrastructure;
using WAD.NET.ZScript;
using WAD.NET.ZScript.Diagnostics;
using WAD.NET.ZScript.Semantics;
using WAD.NET.ZScript.Syntax;
using Xunit;
using Xunit.Abstractions;

namespace WAD.NET.Tests.RealWorld
{
    /// <summary>
    /// Integration tests that parse real ZScript/DECORATE from GZDoom and Zandronum PK3s.
    /// Exercises the full analysis pipeline: lexer -> parser -> semantic model.
    /// Skipped when PK3 paths are not configured.
    /// </summary>
    public class ZScriptIntegrationTests : RealWorldWadTestBase
    {
        private static WadPaths Paths => WadTestConfiguration.Paths;
        private readonly ITestOutputHelper _output;

        public ZScriptIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region Source Extraction Helpers

        /// <summary>
        /// Extracts all ZScript source text entries from a PK3.
        /// Finds the root zscript lump and all .zs/.zsc/.zc files under zscript/ folders.
        /// </summary>
        private static List<(string path, string source)> ExtractZScriptSources(Pk3Reader reader)
        {
            var sources = new List<(string path, string source)>();

            foreach (var entry in reader.GetEntries())
            {
                var fullPath = entry.FullPath;
                var upperPath = fullPath.ToUpperInvariant();
                var ext = Path.GetExtension(fullPath).ToLowerInvariant();

                bool isZScript =
                    // Root-level ZSCRIPT lump (any extension)
                    Path.GetFileNameWithoutExtension(fullPath)
                        .Equals("ZSCRIPT", StringComparison.OrdinalIgnoreCase) ||
                    // Files under zscript/ folder with script extensions
                    (upperPath.StartsWith("ZSCRIPT/") &&
                     ext is ".zs" or ".zsc" or ".zc" or ".txt" or "") ||
                    // Any .zs/.zsc file anywhere
                    ext is ".zs" or ".zsc";

                if (!isZScript || entry.Size == 0)
                    continue;

                var text = ReadEntryAsText(reader, entry);
                if (text != null)
                    sources.Add((fullPath, text));
            }

            return sources;
        }

        /// <summary>
        /// Extracts all DECORATE source text entries from a PK3.
        /// Finds root DECORATE lumps, .dec files, and files under actors/ and decorate/ folders.
        /// </summary>
        private static List<(string path, string source)> ExtractDecorateSources(Pk3Reader reader)
        {
            var sources = new List<(string path, string source)>();

            foreach (var entry in reader.GetEntries())
            {
                var fullPath = entry.FullPath;
                var upperPath = fullPath.ToUpperInvariant();
                var fileName = Path.GetFileNameWithoutExtension(fullPath);
                var ext = Path.GetExtension(fullPath).ToLowerInvariant();

                bool isDecorate =
                    // Root-level DECORATE lump
                    fileName.Equals("DECORATE", StringComparison.OrdinalIgnoreCase) ||
                    // .dec files anywhere
                    ext == ".dec" ||
                    // Files under decorate/ folder
                    upperPath.StartsWith("DECORATE/") ||
                    // Files under actors/ folder (Zandronum convention)
                    (upperPath.StartsWith("ACTORS/") && ext is ".txt" or ".dec" or "");

                if (!isDecorate || entry.Size == 0)
                    continue;

                var text = ReadEntryAsText(reader, entry);
                if (text != null)
                    sources.Add((fullPath, text));
            }

            return sources;
        }

        private static string? ReadEntryAsText(Pk3Reader reader, LumpEntry entry)
        {
            try
            {
                var data = reader.ReadLump(entry);
                var text = Encoding.UTF8.GetString(data);

                // Skip binary files that got picked up
                if (text.Contains('\0'))
                    return null;

                return text;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region GZDoom PK3 — ZScript

        [SkippableFact]
        public void GzdoomPk3_ShouldContainZScriptSources()
        {
            var path = RequireWad("GzdoomPk3", Paths.GzdoomPk3);

            using var reader = new Pk3Reader(path);
            var sources = ExtractZScriptSources(reader);

            _output.WriteLine($"Found {sources.Count} ZScript sources in gzdoom.pk3");
            foreach (var (filePath, source) in sources.Take(20))
                _output.WriteLine($"  {filePath} ({source.Length} chars)");

            Assert.NotEmpty(sources);
        }

        [SkippableFact]
        public void GzdoomPk3_ShouldLexAllZScriptWithoutCrash()
        {
            var path = RequireWad("GzdoomPk3", Paths.GzdoomPk3);

            using var reader = new Pk3Reader(path);
            var sources = ExtractZScriptSources(reader);
            Skip.If(sources.Count == 0, "No ZScript sources found in gzdoom.pk3");

            int totalTokens = 0;
            var failures = new List<string>();

            foreach (var (filePath, source) in sources)
            {
                try
                {
                    var lexer = new Lexer(source);
                    SyntaxToken token;
                    do
                    {
                        token = lexer.Lex();
                        totalTokens++;
                    } while (token.Kind != SyntaxTokenKind.EndOfFile);
                }
                catch (Exception ex)
                {
                    failures.Add($"{filePath}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            _output.WriteLine($"Lexed {totalTokens} tokens across {sources.Count} files");

            if (failures.Count > 0)
            {
                _output.WriteLine($"{failures.Count} lexer failures:");
                foreach (var f in failures)
                    _output.WriteLine($"  {f}");
            }

            Assert.Empty(failures);
        }

        [SkippableFact]
        public void GzdoomPk3_ShouldParseAllZScriptFiles()
        {
            var path = RequireWad("GzdoomPk3", Paths.GzdoomPk3);

            using var reader = new Pk3Reader(path);
            var sources = ExtractZScriptSources(reader);
            Skip.If(sources.Count == 0, "No ZScript sources found in gzdoom.pk3");

            int totalMembers = 0;
            int totalErrors = 0;
            var crashedFiles = new List<string>();
            var filesWithErrors = new List<(string path, int errorCount, string firstError)>();

            foreach (var (filePath, source) in sources)
            {
                try
                {
                    var parser = new Parser(source, ScriptLanguage.ZScript);
                    var unit = parser.ParseCompilationUnit();
                    totalMembers += unit.Members.Length;

                    var errors = parser.Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
                    if (errors.Count > 0)
                    {
                        totalErrors += errors.Count;
                        filesWithErrors.Add((filePath, errors.Count, errors[0].Message));
                    }
                }
                catch (Exception ex)
                {
                    crashedFiles.Add($"{filePath}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            _output.WriteLine($"Parsed {sources.Count} files, {totalMembers} top-level members");
            _output.WriteLine($"  Errors: {totalErrors} across {filesWithErrors.Count} files");
            _output.WriteLine($"  Crashes: {crashedFiles.Count}");

            foreach (var (fp, count, first) in filesWithErrors.Take(10))
                _output.WriteLine($"  {fp}: {count} errors (first: {first})");

            foreach (var c in crashedFiles.Take(10))
                _output.WriteLine($"  CRASH: {c}");

            // The parser must not crash on any real-world input
            Assert.Empty(crashedFiles);
        }

        [SkippableFact]
        public void GzdoomPk3_ShouldBuildSemanticModel()
        {
            var path = RequireWad("GzdoomPk3", Paths.GzdoomPk3);

            using var reader = new Pk3Reader(path);
            var sources = ExtractZScriptSources(reader);
            Skip.If(sources.Count == 0, "No ZScript sources found in gzdoom.pk3");

            var units = new List<CompilationUnitSyntax>();

            foreach (var (_, source) in sources)
            {
                try
                {
                    var parser = new Parser(source, ScriptLanguage.ZScript);
                    units.Add(parser.ParseCompilationUnit());
                }
                catch
                {
                    // Skip files that crash the parser for semantic model testing
                }
            }

            Skip.If(units.Count == 0, "No files parsed successfully");

            var model = SemanticModel.Create(units);
            var allClasses = model.AllClasses.ToList();

            _output.WriteLine($"Semantic model: {allClasses.Count} classes from {units.Count} compilation units");

            // GZDoom's zscript defines hundreds of actors
            Assert.NotEmpty(allClasses);

            // Spot-check well-known GZDoom base classes
            var actor = model.LookupClass("Actor");
            if (actor != null)
                _output.WriteLine($"  Actor: {actor.Methods.Count()} methods, {actor.Fields.Count()} fields");

            var weapon = model.LookupClass("Weapon");
            if (weapon != null)
                _output.WriteLine($"  Weapon: base={weapon.BaseType?.Name ?? "null"}");

            var withStates = allClasses.Count(c => c.States.Any());
            var withFlags = allClasses.Count(c => c.Flags.Any());
            _output.WriteLine($"  Classes with states: {withStates}");
            _output.WriteLine($"  Classes with flags: {withFlags}");
        }

        [SkippableFact]
        public void GzdoomPk3_RoundTripFidelity()
        {
            var path = RequireWad("GzdoomPk3", Paths.GzdoomPk3);

            using var reader = new Pk3Reader(path);
            var sources = ExtractZScriptSources(reader);
            Skip.If(sources.Count == 0, "No ZScript sources found in gzdoom.pk3");

            int passed = 0;
            int failed = 0;
            var sampleFailures = new List<string>();

            foreach (var (filePath, source) in sources)
            {
                try
                {
                    var parser = new Parser(source, ScriptLanguage.ZScript);
                    var unit = parser.ParseCompilationUnit();
                    var emitted = unit.ToFullString();

                    if (emitted == source)
                        passed++;
                    else
                    {
                        failed++;
                        if (sampleFailures.Count < 10)
                            sampleFailures.Add(filePath);
                    }
                }
                catch
                {
                    // Parser crashes tracked by separate test
                }
            }

            _output.WriteLine($"Round-trip: {passed}/{passed + failed} files preserved exactly");

            foreach (var f in sampleFailures)
                _output.WriteLine($"  MISMATCH: {f}");

            // Report round-trip fidelity percentage; don't hard-fail since
            // full trivia preservation is tracked as a known improvement area.
            // Fail only if literally nothing round-trips (regression detection).
            Assert.True(passed > 0, "No files round-tripped successfully — possible parser regression");
        }

        #endregion

        #region Zandronum PK3 — DECORATE

        [SkippableFact]
        public void ZandronumPk3_ShouldContainDecorateSources()
        {
            var path = RequireWad("ZandronumPk3", Paths.ZandronumPk3);

            using var reader = new Pk3Reader(path);
            var sources = ExtractDecorateSources(reader);

            _output.WriteLine($"Found {sources.Count} DECORATE sources in zandronum.pk3");
            foreach (var (filePath, source) in sources.Take(20))
                _output.WriteLine($"  {filePath} ({source.Length} chars)");

            Assert.NotEmpty(sources);
        }

        [SkippableFact]
        public void ZandronumPk3_ShouldLexAllDecorateWithoutCrash()
        {
            var path = RequireWad("ZandronumPk3", Paths.ZandronumPk3);

            using var reader = new Pk3Reader(path);
            var sources = ExtractDecorateSources(reader);
            Skip.If(sources.Count == 0, "No DECORATE sources found in zandronum.pk3");

            int totalTokens = 0;
            var failures = new List<string>();

            foreach (var (filePath, source) in sources)
            {
                try
                {
                    var lexer = new Lexer(source);
                    SyntaxToken token;
                    do
                    {
                        token = lexer.Lex();
                        totalTokens++;
                    } while (token.Kind != SyntaxTokenKind.EndOfFile);
                }
                catch (Exception ex)
                {
                    failures.Add($"{filePath}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            _output.WriteLine($"Lexed {totalTokens} tokens across {sources.Count} files");

            if (failures.Count > 0)
            {
                _output.WriteLine($"{failures.Count} lexer failures:");
                foreach (var f in failures)
                    _output.WriteLine($"  {f}");
            }

            Assert.Empty(failures);
        }

        [SkippableFact]
        public void ZandronumPk3_ShouldParseAllDecorateFiles()
        {
            var path = RequireWad("ZandronumPk3", Paths.ZandronumPk3);

            using var reader = new Pk3Reader(path);
            var sources = ExtractDecorateSources(reader);
            Skip.If(sources.Count == 0, "No DECORATE sources found in zandronum.pk3");

            int totalMembers = 0;
            int totalErrors = 0;
            var crashedFiles = new List<string>();
            var filesWithErrors = new List<(string path, int errorCount, string firstError)>();

            foreach (var (filePath, source) in sources)
            {
                try
                {
                    var parser = new Parser(source, ScriptLanguage.Decorate);
                    var unit = parser.ParseCompilationUnit();
                    totalMembers += unit.Members.Length;

                    var errors = parser.Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
                    if (errors.Count > 0)
                    {
                        totalErrors += errors.Count;
                        filesWithErrors.Add((filePath, errors.Count, errors[0].Message));
                    }
                }
                catch (Exception ex)
                {
                    crashedFiles.Add($"{filePath}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            _output.WriteLine($"Parsed {sources.Count} files, {totalMembers} top-level members");
            _output.WriteLine($"  Errors: {totalErrors} across {filesWithErrors.Count} files");
            _output.WriteLine($"  Crashes: {crashedFiles.Count}");

            foreach (var (fp, count, first) in filesWithErrors.Take(10))
                _output.WriteLine($"  {fp}: {count} errors (first: {first})");

            foreach (var c in crashedFiles.Take(10))
                _output.WriteLine($"  CRASH: {c}");

            Assert.Empty(crashedFiles);
        }

        [SkippableFact]
        public void ZandronumPk3_ShouldBuildSemanticModel()
        {
            var path = RequireWad("ZandronumPk3", Paths.ZandronumPk3);

            using var reader = new Pk3Reader(path);
            var sources = ExtractDecorateSources(reader);
            Skip.If(sources.Count == 0, "No DECORATE sources found in zandronum.pk3");

            var units = new List<CompilationUnitSyntax>();

            foreach (var (_, source) in sources)
            {
                try
                {
                    var parser = new Parser(source, ScriptLanguage.Decorate);
                    units.Add(parser.ParseCompilationUnit());
                }
                catch
                {
                    // Skip files that crash the parser
                }
            }

            Skip.If(units.Count == 0, "No files parsed successfully");

            var model = SemanticModel.Create(units);
            var allClasses = model.AllClasses.ToList();

            _output.WriteLine($"Semantic model: {allClasses.Count} classes from {units.Count} compilation units");

            Assert.NotEmpty(allClasses);

            var actor = model.LookupClass("Actor");
            if (actor != null)
                _output.WriteLine($"  Actor: {actor.Methods.Count()} methods, {actor.Fields.Count()} fields");

            var withStates = allClasses.Count(c => c.States.Any());
            var withFlags = allClasses.Count(c => c.Flags.Any());
            _output.WriteLine($"  Classes with states: {withStates}");
            _output.WriteLine($"  Classes with flags: {withFlags}");
        }

        [SkippableFact]
        public void ZandronumPk3_RoundTripFidelity()
        {
            var path = RequireWad("ZandronumPk3", Paths.ZandronumPk3);

            using var reader = new Pk3Reader(path);
            var sources = ExtractDecorateSources(reader);
            Skip.If(sources.Count == 0, "No DECORATE sources found in zandronum.pk3");

            int passed = 0;
            int failed = 0;
            var sampleFailures = new List<string>();

            foreach (var (filePath, source) in sources)
            {
                try
                {
                    var parser = new Parser(source, ScriptLanguage.Decorate);
                    var unit = parser.ParseCompilationUnit();
                    var emitted = unit.ToFullString();

                    if (emitted == source)
                        passed++;
                    else
                    {
                        failed++;
                        if (sampleFailures.Count < 10)
                            sampleFailures.Add(filePath);
                    }
                }
                catch
                {
                    // Parser crashes tracked by separate test
                }
            }

            _output.WriteLine($"Round-trip: {passed}/{passed + failed} files preserved exactly");

            foreach (var f in sampleFailures)
                _output.WriteLine($"  MISMATCH: {f}");

            Assert.True(passed > 0, "No files round-tripped successfully — possible parser regression");
        }

        #endregion

        #region DECORATE from all PK3s

        [SkippableFact]
        public void AllPk3s_ShouldParseDecorateWithoutCrash()
        {
            var pk3s = Paths.GetPk3s();
            int testedCount = 0;
            int totalActors = 0;
            var allCrashes = new List<string>();

            foreach (var (name, pk3Path) in pk3s)
            {
                if (!WadTestConfiguration.IsAvailable(pk3Path))
                    continue;

                testedCount++;

                using var reader = new Pk3Reader(pk3Path);
                var sources = ExtractDecorateSources(reader);

                foreach (var (filePath, source) in sources)
                {
                    try
                    {
                        var parser = new Parser(source, ScriptLanguage.Decorate);
                        var unit = parser.ParseCompilationUnit();
                        totalActors += unit.Members.Length;
                    }
                    catch (Exception ex)
                    {
                        allCrashes.Add($"{name}/{filePath}: {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }

            Skip.If(testedCount == 0, "No PK3s configured for testing");

            _output.WriteLine($"Parsed DECORATE from {testedCount} PK3s, {totalActors} actors");

            foreach (var c in allCrashes.Take(10))
                _output.WriteLine($"  CRASH: {c}");

            Assert.Empty(allCrashes);
        }

        #endregion
    }
}
