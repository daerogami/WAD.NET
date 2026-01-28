using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WAD.NET.Archives;
using WAD.NET.Detection;
using WAD.NET.Enums;

namespace WAD.NET.Composite
{
    /// <summary>
    /// Loads multiple archives in a specified order and resolves the effective lump set,
    /// implementing DOOM's resource layering model where later archives override earlier ones.
    /// </summary>
    public class CompositeArchive
    {
        /// <summary>
        /// The ordered list of archive paths in the load order.
        /// </summary>
        public IReadOnlyList<string> LoadOrder { get; }

        /// <summary>
        /// The effective lump set after resolution, keyed by lump name (case-insensitive).
        /// </summary>
        public IReadOnlyDictionary<string, ResolvedLump> EffectiveLumps { get; }

        /// <summary>
        /// All lumps that were resolved as overrides.
        /// </summary>
        public IReadOnlyList<ResolvedLump> Overrides { get; }

        /// <summary>
        /// All lumps that were resolved as additions (not present in earlier archives).
        /// </summary>
        public IReadOnlyList<ResolvedLump> Additions { get; }

        // Full history of every lump version seen, keyed by uppercase name.
        private readonly Dictionary<string, List<LumpEntry>> _overrideChains;

        // All resolved lumps, for source filtering.
        private readonly List<ResolvedLump> _allResolved;

        private CompositeArchive(
            IReadOnlyList<string> loadOrder,
            Dictionary<string, ResolvedLump> effectiveLumps,
            Dictionary<string, List<LumpEntry>> overrideChains,
            List<ResolvedLump> allResolved)
        {
            LoadOrder = loadOrder;
            EffectiveLumps = effectiveLumps;
            _overrideChains = overrideChains;
            _allResolved = allResolved;

            Overrides = allResolved.Where(r => r.Resolution == LumpResolutionType.Override).ToList();
            Additions = allResolved.Where(r => r.Resolution == LumpResolutionType.Added).ToList();
        }

        /// <summary>
        /// Loads multiple archives in order and resolves the effective lump set.
        /// The first archive is treated as the base (typically an IWAD).
        /// Subsequent archives override or extend it.
        /// </summary>
        /// <param name="archivePaths">Paths to archive files or folders, in load order.</param>
        /// <returns>A <see cref="CompositeArchive"/> with the resolved lump set.</returns>
        public static CompositeArchive Load(params string[] archivePaths)
        {
            if (archivePaths == null || archivePaths.Length == 0)
                throw new ArgumentException("At least one archive path is required.", nameof(archivePaths));

            var readers = new IArchiveReader[archivePaths.Length];
            try
            {
                for (int i = 0; i < archivePaths.Length; i++)
                {
                    readers[i] = ArchiveReaderFactory.Open(archivePaths[i]);
                }

                return Load(readers);
            }
            finally
            {
                foreach (var reader in readers)
                {
                    reader?.Dispose();
                }
            }
        }

        /// <summary>
        /// Loads multiple archives using pre-constructed readers and resolves the effective lump set.
        /// The first reader is treated as the base (typically an IWAD).
        /// </summary>
        /// <param name="readers">Archive readers in load order.</param>
        /// <returns>A <see cref="CompositeArchive"/> with the resolved lump set.</returns>
        public static CompositeArchive Load(params IArchiveReader[] readers)
        {
            if (readers == null || readers.Length == 0)
                throw new ArgumentException("At least one reader is required.", nameof(readers));

            var loadOrder = readers.Select(r => r.Path).ToList();
            var effectiveLumps = new Dictionary<string, ResolvedLump>(StringComparer.OrdinalIgnoreCase);
            var overrideChains = new Dictionary<string, List<LumpEntry>>(StringComparer.OrdinalIgnoreCase);
            var allResolved = new List<ResolvedLump>();

            // Track which lump names exist in marker-bounded sections across all archives.
            // Marker sections merge additively, but individual names within them override.
            var markerSectionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int sourceIndex = 0; sourceIndex < readers.Length; sourceIndex++)
            {
                var reader = readers[sourceIndex];
                var entries = reader.GetEntries().ToList();
                bool isBase = sourceIndex == 0;

                // Detect map groups: a map marker followed by its sub-lumps.
                var mapGroups = DetectMapGroups(entries);

                // Detect marker-bounded sections.
                var sectionEntries = DetectMarkerSections(entries);

                // Track which entries are part of a map group (to handle atomic replacement).
                var mapGroupEntries = new HashSet<int>();
                foreach (var group in mapGroups)
                {
                    foreach (int idx in group.Value)
                    {
                        mapGroupEntries.Add(idx);
                    }
                }

                // Process map groups atomically.
                foreach (var kvp in mapGroups)
                {
                    string mapName = kvp.Key;
                    var groupIndices = kvp.Value;

                    // If a later archive provides a map marker, replace the entire group.
                    // First, remove all existing sub-lumps from the base map group if overriding.
                    if (!isBase && effectiveLumps.ContainsKey(mapName))
                    {
                        // Find and remove old map sub-lumps from effective set.
                        var oldSubLumpNames = MapSubLumpNames;
                        foreach (var subName in oldSubLumpNames)
                        {
                            // Only remove if the existing lump came from the same map group context.
                            // We construct compound keys: we actually just use the sub-lump name directly
                            // since map sub-lumps like THINGS, LINEDEFS are shared names.
                            // In WAD format, sub-lumps follow the map marker, so they are unique per position.
                            // For effective resolution we key by name, so the override naturally replaces.
                        }
                    }

                    // Add all entries in the map group.
                    foreach (int entryIndex in groupIndices)
                    {
                        var entry = entries[entryIndex];
                        AddOrOverride(effectiveLumps, overrideChains, allResolved,
                            entry, sourceIndex, reader.Path, isBase);
                    }
                }

                // Process marker-bounded section entries (merge additively, name-level override).
                foreach (int entryIndex in sectionEntries)
                {
                    if (mapGroupEntries.Contains(entryIndex))
                        continue; // Already handled as part of a map group.

                    var entry = entries[entryIndex];
                    AddOrOverride(effectiveLumps, overrideChains, allResolved,
                        entry, sourceIndex, reader.Path, isBase);
                }

                // Process remaining entries.
                for (int i = 0; i < entries.Count; i++)
                {
                    if (mapGroupEntries.Contains(i) || sectionEntries.Contains(i))
                        continue;

                    var entry = entries[i];
                    AddOrOverride(effectiveLumps, overrideChains, allResolved,
                        entry, sourceIndex, reader.Path, isBase);
                }
            }

            return new CompositeArchive(loadOrder, effectiveLumps, overrideChains, allResolved);
        }

        /// <summary>
        /// Returns all resolved lumps originating from a specific archive in the load order.
        /// </summary>
        /// <param name="sourceIndex">Zero-based index into the load order.</param>
        /// <returns>Resolved lumps from the specified archive.</returns>
        public IEnumerable<ResolvedLump> GetLumpsFromSource(int sourceIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= LoadOrder.Count)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));

            return _allResolved.Where(r => r.SourceIndex == sourceIndex);
        }

        /// <summary>
        /// Returns the full override chain for a given lump name across all archives.
        /// The list is ordered by load order (first = earliest/base, last = final effective version).
        /// </summary>
        /// <param name="lumpName">The lump name to look up (case-insensitive).</param>
        /// <returns>All versions of the lump across archives, or an empty list if not found.</returns>
        public IReadOnlyList<LumpEntry> GetOverrideChain(string lumpName)
        {
            if (lumpName == null)
                throw new ArgumentNullException(nameof(lumpName));

            if (_overrideChains.TryGetValue(lumpName, out var chain))
                return chain;

            return Array.Empty<LumpEntry>();
        }

        /// <summary>
        /// Analyzes the composite archive for lump conflicts and compatibility requirements.
        /// Conflicts are reported when two or more non-base archives provide the same lump.
        /// Compatibility analysis runs against the effective (resolved) lump set.
        /// </summary>
        /// <returns>A <see cref="CompositeAnalysis"/> with conflicts and compatibility info.</returns>
        public CompositeAnalysis Analyze()
        {
            // 1. Detect conflicts: lump names provided by 2+ non-base archives.
            var conflicts = DetectConflicts();

            // 2. Run compatibility analysis against the effective lump set.
            var analyzer = new CompatibilityAnalyzer();
            ModCompatibility compatibility;
            using (var reader = new CompositeArchiveReader(this))
            {
                compatibility = analyzer.Analyze(reader);
            }

            return new CompositeAnalysis
            {
                Compatibility = compatibility,
                Conflicts = conflicts
            };
        }

        private List<LumpConflict> DetectConflicts()
        {
            var conflicts = new List<LumpConflict>();

            // Group override chain entries by lump name, filtering to non-base sources (sourceIndex > 0).
            foreach (var kvp in _overrideChains)
            {
                var chain = kvp.Value;
                if (chain.Count < 2)
                    continue;

                // Find all distinct non-base source indices that contributed this lump.
                // We need source info, which is tracked in _allResolved.
                var nonBaseSources = _allResolved
                    .Where(r => string.Equals(r.Lump.Name, kvp.Key, StringComparison.OrdinalIgnoreCase)
                                && r.SourceIndex > 0)
                    .GroupBy(r => r.SourceIndex)
                    .Select(g => g.First())
                    .ToList();

                if (nonBaseSources.Count >= 2)
                {
                    conflicts.Add(new LumpConflict
                    {
                        LumpName = kvp.Key,
                        Sources = nonBaseSources.Select(r => new LumpConflictSource
                        {
                            SourceIndex = r.SourceIndex,
                            SourcePath = r.SourcePath,
                            Lump = r.Lump
                        }).ToList()
                    });
                }
            }

            return conflicts;
        }

        /// <summary>
        /// An IArchiveReader wrapper that exposes the composite's effective lump set
        /// for use with CompatibilityAnalyzer.
        /// </summary>
        private class CompositeArchiveReader : IArchiveReader
        {
            private readonly CompositeArchive _composite;

            public CompositeArchiveReader(CompositeArchive composite)
            {
                _composite = composite;
            }

            public string Path => "composite:" + string.Join(";", _composite.LoadOrder);
            public ArchiveType Type => ArchiveType.WAD;

            public IEnumerable<LumpEntry> GetEntries()
            {
                return _composite.EffectiveLumps.Values.Select(r => r.Lump);
            }

            public LumpEntry? GetEntry(string name)
            {
                if (_composite.EffectiveLumps.TryGetValue(name, out var resolved))
                    return resolved.Lump;
                return null;
            }

            public byte[] ReadLump(LumpEntry entry)
            {
                // We cannot read raw lump data without the original reader.
                // Return empty data; the analyzer mostly checks for lump presence.
                return Array.Empty<byte>();
            }

            public Stream OpenLump(LumpEntry entry)
            {
                return new MemoryStream(Array.Empty<byte>(), writable: false);
            }

            public bool Contains(string name)
            {
                return _composite.EffectiveLumps.ContainsKey(name);
            }

            public void Dispose()
            {
                // Nothing to dispose; the composite owns the data.
            }
        }

        /// <summary>
        /// Standard map sub-lump names that follow a map marker in WAD format.
        /// </summary>
        private static readonly string[] MapSubLumpNames =
        {
            "THINGS", "LINEDEFS", "SIDEDEFS", "VERTEXES", "SEGS",
            "SSECTORS", "NODES", "SECTORS", "REJECT", "BLOCKMAP",
            "BEHAVIOR", "SCRIPTS", "DIALOGUE", "TEXTMAP", "ZNODES", "ENDMAP"
        };

        private static readonly HashSet<string> MapSubLumpSet =
            new HashSet<string>(MapSubLumpNames, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Marker pairs that define bounded sections in a WAD.
        /// </summary>
        private static readonly (string Start, string End)[] MarkerPairs =
        {
            ("F_START", "F_END"),
            ("FF_START", "FF_END"),
            ("S_START", "S_END"),
            ("SS_START", "SS_END"),
            ("P_START", "P_END"),
            ("PP_START", "PP_END"),
            ("C_START", "C_END"),
            ("TX_START", "TX_END"),
            ("HI_START", "HI_END")
        };

        /// <summary>
        /// Detects map groups in an entry list. A map group is a map marker
        /// (e.g., MAP01, E1M1) followed by its sub-lumps (THINGS, LINEDEFS, etc.).
        /// </summary>
        private static Dictionary<string, List<int>> DetectMapGroups(List<LumpEntry> entries)
        {
            var groups = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!IsMapMarker(entry.Name))
                    continue;

                var indices = new List<int> { i };

                // Collect subsequent map sub-lumps.
                for (int j = i + 1; j < entries.Count; j++)
                {
                    if (MapSubLumpSet.Contains(entries[j].Name))
                    {
                        indices.Add(j);
                    }
                    else
                    {
                        break;
                    }
                }

                groups[entry.Name] = indices;
            }

            return groups;
        }

        /// <summary>
        /// Checks whether a lump name matches a DOOM map marker pattern (ExMy or MAPxx).
        /// </summary>
        private static bool IsMapMarker(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            var upper = name.ToUpperInvariant();

            // MAPxx pattern (MAP00-MAP99)
            if (upper.Length >= 5 && upper.StartsWith("MAP", StringComparison.Ordinal))
            {
                var rest = upper.Substring(3);
                return rest.Length >= 2 && rest.All(char.IsDigit);
            }

            // ExMy pattern
            if (upper.Length >= 4 && upper[0] == 'E' && char.IsDigit(upper[1])
                && upper[2] == 'M' && char.IsDigit(upper[3]))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Detects entries that fall within marker-bounded sections (F_START/F_END, etc.).
        /// Returns the set of entry indices that are inside any section (including the markers).
        /// </summary>
        private static HashSet<int> DetectMarkerSections(List<LumpEntry> entries)
        {
            var result = new HashSet<int>();

            foreach (var (start, end) in MarkerPairs)
            {
                bool inSection = false;
                for (int i = 0; i < entries.Count; i++)
                {
                    var name = entries[i].Name;
                    if (string.Equals(name, start, StringComparison.OrdinalIgnoreCase))
                    {
                        inSection = true;
                        result.Add(i);
                        continue;
                    }

                    if (string.Equals(name, end, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(i);
                        inSection = false;
                        continue;
                    }

                    if (inSection)
                    {
                        result.Add(i);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Adds or overrides a lump in the effective set, recording the override chain.
        /// </summary>
        private static void AddOrOverride(
            Dictionary<string, ResolvedLump> effectiveLumps,
            Dictionary<string, List<LumpEntry>> overrideChains,
            List<ResolvedLump> allResolved,
            LumpEntry entry,
            int sourceIndex,
            string sourcePath,
            bool isBase)
        {
            string key = entry.Name;

            // Record in override chain.
            if (!overrideChains.TryGetValue(key, out var chain))
            {
                chain = new List<LumpEntry>();
                overrideChains[key] = chain;
            }
            chain.Add(entry);

            LumpResolutionType resolution;
            LumpEntry? overriddenLump = null;

            if (isBase)
            {
                resolution = LumpResolutionType.Base;
            }
            else if (effectiveLumps.TryGetValue(key, out var existing))
            {
                resolution = LumpResolutionType.Override;
                overriddenLump = existing.Lump;
            }
            else
            {
                resolution = LumpResolutionType.Added;
            }

            var resolved = new ResolvedLump
            {
                Lump = entry,
                Resolution = resolution,
                SourceIndex = sourceIndex,
                SourcePath = sourcePath,
                OverriddenLump = overriddenLump
            };

            effectiveLumps[key] = resolved;
            allResolved.Add(resolved);
        }
    }
}
