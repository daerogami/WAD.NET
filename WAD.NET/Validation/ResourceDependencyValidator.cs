using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WAD.NET.Archives;
using WAD.NET.Composite;
using WAD.NET.Detection;
using WAD.NET.Parsers.SndInfo;

namespace WAD.NET.Validation
{
    /// <summary>
    /// Validates that cross-resource references (sprites, sounds, patches) resolve
    /// within an archive or composite archive's effective lump set.
    /// </summary>
    public class ResourceDependencyValidator
    {
        /// <summary>
        /// Validates that all cross-resource references resolve within the archive.
        /// </summary>
        /// <param name="archive">The archive reader to validate.</param>
        /// <returns>A result containing any issues found.</returns>
        public ResourceDependencyResult Validate(IArchiveReader archive)
        {
            if (archive == null)
                throw new ArgumentNullException(nameof(archive));

            var entries = archive.GetEntries().ToList();
            var lumpNames = new HashSet<string>(entries.Select(e => e.Name), StringComparer.OrdinalIgnoreCase);
            var issues = new List<ResourceDependencyIssue>();

            // Helper to read lump content as string
            string? ReadLumpText(string name)
            {
                var entry = archive.GetEntry(name);
                if (entry == null) return null;
                var data = archive.ReadLump(entry);
                if (data == null || data.Length == 0) return null;
                return Encoding.UTF8.GetString(data);
            }

            // Helper to read lump content as bytes
            byte[]? ReadLumpBytes(string name)
            {
                var entry = archive.GetEntry(name);
                if (entry == null) return null;
                return archive.ReadLump(entry);
            }

            // a) DECORATE sprites
            CheckDecorateSprites(ReadLumpText("DECORATE"), lumpNames, issues);

            // b) ZScript sprites
            CheckZScriptSprites(ReadLumpText("ZSCRIPT"), lumpNames, issues);

            // c) SNDINFO sounds
            CheckSndInfoSounds(ReadLumpText("SNDINFO"), lumpNames, issues);

            // d) PNAMES patches
            var pnamesData = ReadLumpBytes("PNAMES");
            var patchNames = CheckPnamesPatches(pnamesData, lumpNames, issues);

            // e) TEXTURE1/TEXTURE2 patch indices
            CheckTexturePatches(ReadLumpBytes("TEXTURE1"), "TEXTURE1", patchNames?.Count ?? 0, issues);
            CheckTexturePatches(ReadLumpBytes("TEXTURE2"), "TEXTURE2", patchNames?.Count ?? 0, issues);

            return new ResourceDependencyResult { Issues = issues };
        }

        /// <summary>
        /// Validates against a composite archive's effective lump set.
        /// </summary>
        /// <param name="composite">The composite archive to validate.</param>
        /// <returns>A result containing any issues found.</returns>
        public ResourceDependencyResult Validate(CompositeArchive composite)
        {
            if (composite == null)
                throw new ArgumentNullException(nameof(composite));

            var effectiveLumps = composite.EffectiveLumps;
            var lumpNames = new HashSet<string>(effectiveLumps.Keys, StringComparer.OrdinalIgnoreCase);
            var issues = new List<ResourceDependencyIssue>();

            // Helper to read lump content as string
            string? ReadLumpText(string name)
            {
                if (!effectiveLumps.ContainsKey(name)) return null;
                try
                {
                    var data = composite.ReadLump(name);
                    if (data == null || data.Length == 0) return null;
                    return Encoding.UTF8.GetString(data);
                }
                catch
                {
                    return null;
                }
            }

            // Helper to read lump content as bytes
            byte[]? ReadLumpBytes(string name)
            {
                if (!effectiveLumps.ContainsKey(name)) return null;
                try
                {
                    return composite.ReadLump(name);
                }
                catch
                {
                    return null;
                }
            }

            // a) DECORATE sprites
            CheckDecorateSprites(ReadLumpText("DECORATE"), lumpNames, issues);

            // b) ZScript sprites
            CheckZScriptSprites(ReadLumpText("ZSCRIPT"), lumpNames, issues);

            // c) SNDINFO sounds
            CheckSndInfoSounds(ReadLumpText("SNDINFO"), lumpNames, issues);

            // d) PNAMES patches
            var pnamesData = ReadLumpBytes("PNAMES");
            var patchNames = CheckPnamesPatches(pnamesData, lumpNames, issues);

            // e) TEXTURE1/TEXTURE2 patch indices
            CheckTexturePatches(ReadLumpBytes("TEXTURE1"), "TEXTURE1", patchNames?.Count ?? 0, issues);
            CheckTexturePatches(ReadLumpBytes("TEXTURE2"), "TEXTURE2", patchNames?.Count ?? 0, issues);

            return new ResourceDependencyResult { Issues = issues };
        }

        /// <summary>
        /// Checks DECORATE actor sprite references against available lumps.
        /// </summary>
        private void CheckDecorateSprites(string? decorateContent, HashSet<string> lumpNames, List<ResourceDependencyIssue> issues)
        {
            if (decorateContent == null) return;

            var info = DecorateScanner.Scan(decorateContent);
            foreach (var actor in info.Actors)
            {
                foreach (var sprite in actor.ReferencedSprites)
                {
                    if (!HasSpriteWithPrefix(sprite, lumpNames))
                    {
                        issues.Add(new ResourceDependencyIssue
                        {
                            Severity = IssueSeverity.Warning,
                            Category = "Sprite",
                            Source = $"DECORATE actor {actor.Name}",
                            ReferencedName = sprite,
                            Message = $"Actor '{actor.Name}' references sprite prefix '{sprite}' but no matching sprite lump was found."
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Checks ZScript class sprite references against available lumps.
        /// </summary>
        private void CheckZScriptSprites(string? zscriptContent, HashSet<string> lumpNames, List<ResourceDependencyIssue> issues)
        {
            if (zscriptContent == null) return;

            var info = ZScriptScanner.Scan(zscriptContent);
            foreach (var cls in info.Classes)
            {
                foreach (var sprite in cls.ReferencedSprites)
                {
                    if (!HasSpriteWithPrefix(sprite, lumpNames))
                    {
                        issues.Add(new ResourceDependencyIssue
                        {
                            Severity = IssueSeverity.Warning,
                            Category = "Sprite",
                            Source = $"ZSCRIPT class {cls.Name}",
                            ReferencedName = sprite,
                            Message = $"Class '{cls.Name}' references sprite prefix '{sprite}' but no matching sprite lump was found."
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Checks SNDINFO sound lump references against available lumps.
        /// </summary>
        private void CheckSndInfoSounds(string? sndInfoContent, HashSet<string> lumpNames, List<ResourceDependencyIssue> issues)
        {
            if (sndInfoContent == null) return;

            var parser = new SndInfoParser();
            var sndInfo = parser.Parse(sndInfoContent);

            foreach (var kvp in sndInfo.Sounds)
            {
                var logicalName = kvp.Key;
                var lumpName = kvp.Value;

                if (!lumpNames.Contains(lumpName))
                {
                    issues.Add(new ResourceDependencyIssue
                    {
                        Severity = IssueSeverity.Warning,
                        Category = "Sound",
                        Source = $"SNDINFO ({logicalName})",
                        ReferencedName = lumpName,
                        Message = $"Sound '{logicalName}' references lump '{lumpName}' which was not found."
                    });
                }
            }
        }

        /// <summary>
        /// Parses PNAMES and checks that each patch name exists as a lump.
        /// Returns the list of parsed patch names for use by TEXTURE1/2 validation.
        /// </summary>
        private List<string>? CheckPnamesPatches(byte[]? pnamesData, HashSet<string> lumpNames, List<ResourceDependencyIssue> issues)
        {
            if (pnamesData == null || pnamesData.Length < 4)
                return null;

            int count = BitConverter.ToInt32(pnamesData, 0);
            if (pnamesData.Length < 4 + count * 8)
                return null;

            var patchNames = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                int offset = 4 + i * 8;
                string name = Encoding.ASCII.GetString(pnamesData, offset, 8).TrimEnd('\0').ToUpperInvariant();
                patchNames.Add(name);

                if (!string.IsNullOrEmpty(name) && !lumpNames.Contains(name))
                {
                    issues.Add(new ResourceDependencyIssue
                    {
                        Severity = IssueSeverity.Error,
                        Category = "Patch",
                        Source = "PNAMES",
                        ReferencedName = name,
                        Message = $"PNAMES entry {i} references patch '{name}' which was not found."
                    });
                }
            }

            return patchNames;
        }

        /// <summary>
        /// Parses a TEXTURE1 or TEXTURE2 lump and checks that patch indices are within PNAMES bounds.
        /// </summary>
        private void CheckTexturePatches(byte[]? textureData, string textureLumpName, int pnamesCount, List<ResourceDependencyIssue> issues)
        {
            if (textureData == null || textureData.Length < 4)
                return;

            int textureCount = BitConverter.ToInt32(textureData, 0);
            if (textureData.Length < 4 + textureCount * 4)
                return;

            // Read texture offsets
            for (int t = 0; t < textureCount; t++)
            {
                int textureOffset = BitConverter.ToInt32(textureData, 4 + t * 4);
                if (textureOffset < 0 || textureOffset + 22 > textureData.Length)
                    continue;

                string texName = Encoding.ASCII.GetString(textureData, textureOffset, 8).TrimEnd('\0').ToUpperInvariant();
                // Skip: masked (4), width (2), height (2), columnDirectory (4) = 12 bytes after name
                int patchCountOffset = textureOffset + 20; // 8 + 4 + 2 + 2 + 4 = 20
                if (patchCountOffset + 2 > textureData.Length)
                    continue;

                short patchCount = BitConverter.ToInt16(textureData, patchCountOffset);
                int patchDataStart = patchCountOffset + 2;

                for (int p = 0; p < patchCount; p++)
                {
                    int patchOffset = patchDataStart + p * 10; // 5 * int16 = 10 bytes per patch
                    if (patchOffset + 6 > textureData.Length)
                        break;

                    // originX (2), originY (2), patchIndex (2)
                    short patchIndex = BitConverter.ToInt16(textureData, patchOffset + 4);

                    if (patchIndex < 0 || patchIndex >= pnamesCount)
                    {
                        issues.Add(new ResourceDependencyIssue
                        {
                            Severity = IssueSeverity.Error,
                            Category = "Patch",
                            Source = $"{textureLumpName} texture '{texName}'",
                            ReferencedName = $"PNAMES[{patchIndex}]",
                            Message = $"Texture '{texName}' in {textureLumpName} references patch index {patchIndex} which exceeds PNAMES count of {pnamesCount}."
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Checks if any lump name starts with the given 4-character sprite prefix.
        /// </summary>
        private static bool HasSpriteWithPrefix(string prefix, HashSet<string> lumpNames)
        {
            // HashSet doesn't support prefix matching, so we iterate.
            // The prefix is always 4 characters (uppercase).
            foreach (var name in lumpNames)
            {
                if (name.Length >= 4 && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
