using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WAD.NET.Concrete;
using WAD.NET.Concrete.Maps;
using WAD.NET.Definitions.Classic.Map;
using WAD.NET.Definitions.GameData;
using WAD.NET.Definitions.Hexen;
using WAD.NET.Interfaces;
using WAD.NET.UDMF;

namespace WAD.NET.Validation
{
    /// <summary>
    /// Validation error codes for WAD validation.
    /// </summary>
    public static class ValidationCodes
    {
        // Map structure errors
        public const string MapMissingLumps = "MAP_MISSING_LUMPS";
        public const string MapIncomplete = "MAP_INCOMPLETE";
        public const string MapEmptyData = "MAP_EMPTY_DATA";

        // Linedef errors
        public const string LinedefInvalidStartVertex = "LINEDEF_INVALID_START_VERTEX";
        public const string LinedefInvalidEndVertex = "LINEDEF_INVALID_END_VERTEX";
        public const string LinedefInvalidFrontSidedef = "LINEDEF_INVALID_FRONT_SIDEDEF";
        public const string LinedefInvalidBackSidedef = "LINEDEF_INVALID_BACK_SIDEDEF";
        public const string LinedefMissingFrontSidedef = "LINEDEF_MISSING_FRONT_SIDEDEF";
        public const string LinedefTwoSidedNoBack = "LINEDEF_TWOSIDED_NO_BACK";

        // Sidedef errors
        public const string SidedefInvalidSector = "SIDEDEF_INVALID_SECTOR";

        // Marker errors
        public const string MarkerUnpaired = "MARKER_UNPAIRED";
        public const string MarkerNested = "MARKER_NESTED";
        public const string MarkerEmpty = "MARKER_EMPTY";

        // Resource errors
        public const string ResourceMissingPlaypal = "RESOURCE_MISSING_PLAYPAL";
        public const string ResourceMissingColormap = "RESOURCE_MISSING_COLORMAP";

        // Warnings
        public const string MapNoThings = "MAP_NO_THINGS";
        public const string MapNoPlayerStart = "MAP_NO_PLAYER_START";
        public const string TextureMissing = "TEXTURE_MISSING";
    }

    /// <summary>
    /// Provides validation functionality for WAD files.
    /// </summary>
    /// <remarks>
    /// The validator performs the following checks:
    /// <list type="bullet">
    ///   <item>Structural validation: Map markers have associated map lumps</item>
    ///   <item>Marker pair validation: F_START/F_END, S_START/S_END, P_START/P_END match</item>
    ///   <item>Cross-reference validation: Linedef indices reference valid vertices and sidedefs</item>
    ///   <item>Sidedef sector indices reference valid sectors</item>
    ///   <item>Resource validation: PLAYPAL exists if graphics lumps are present</item>
    /// </list>
    /// </remarks>
    public class WadValidator : IWadValidator
    {
        // Map marker patterns (DOOM E1M1-E4M9, DOOM II MAP01-MAP32)
        private static readonly Regex DoomMapPattern = new Regex(@"^E[1-9]M[1-9]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex Doom2MapPattern = new Regex(@"^MAP\d{2}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Required map lumps for DOOM/Hexen format
        private static readonly string[] RequiredMapLumps = { "THINGS", "LINEDEFS", "SIDEDEFS", "VERTEXES", "SECTORS" };

        // Optional map lumps
        private static readonly string[] OptionalMapLumps = { "SEGS", "SSECTORS", "NODES", "REJECT", "BLOCKMAP" };

        // UDMF map lumps
        private static readonly string[] UdmfMapLumps = { "TEXTMAP", "ENDMAP" };

        // Marker pairs that should be matched
        private static readonly (string Start, string End)[] MarkerPairs = new[]
        {
            ("F_START", "F_END"),
            ("FF_START", "FF_END"),
            ("F1_START", "F1_END"),
            ("F2_START", "F2_END"),
            ("F3_START", "F3_END"),
            ("S_START", "S_END"),
            ("SS_START", "SS_END"),
            ("P_START", "P_END"),
            ("PP_START", "PP_END"),
            ("P1_START", "P1_END"),
            ("P2_START", "P2_END"),
            ("P3_START", "P3_END"),
            ("TX_START", "TX_END"),
            ("HI_START", "HI_END"),
            ("V_START", "V_END"),
            ("VV_START", "VV_END"),
            ("A_START", "A_END"),
            ("AA_START", "AA_END"),
        };

        // Graphics lump names that require PLAYPAL
        private static readonly HashSet<string> KnownGraphicsLumps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "TITLEPIC", "INTERPIC", "CREDIT", "HELP", "HELP1", "HELP2", "VICTORY2", "ENDPIC",
            "STBAR", "STARMS", "STGNUM0", "STGNUM1", "STGNUM2", "STGNUM3", "STGNUM4",
            "STGNUM5", "STGNUM6", "STGNUM7", "STGNUM8", "STGNUM9"
        };

        // Player start thing types (derived from ThingDatabase)
        private static readonly HashSet<int> PlayerStartTypes = new HashSet<int>(
            ThingDatabase.GetByCategory(ThingCategory.Player).Select(t => t.DoomEdNum));

        /// <summary>
        /// Validates a WAD and returns a detailed validation result.
        /// </summary>
        /// <param name="wad">The WAD to validate.</param>
        /// <returns>A ValidationResult containing any errors and warnings found.</returns>
        public ValidationResult Validate(Wad wad)
        {
            if (wad == null)
                throw new ArgumentNullException(nameof(wad));

            var result = new ValidationResult();

            // Get lumps as a list for indexed access
            var lumps = wad.Lumps.ToList();

            // Validate map structure
            ValidateMaps(lumps, result);

            // Validate marker pairs
            ValidateMarkerPairs(lumps, result);

            // Validate resources
            ValidateResources(lumps, result);

            return result;
        }

        /// <summary>
        /// Checks if a WAD is valid (has no validation errors).
        /// </summary>
        /// <param name="wad">The WAD to validate.</param>
        /// <returns>True if the WAD is valid, false otherwise.</returns>
        public bool IsValid(Wad wad)
        {
            return Validate(wad).IsValid;
        }

        /// <summary>
        /// Validates a WAD configuration.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <returns>True if the configuration is valid, false otherwise.</returns>
        public bool IsValid(IWadConfig config)
        {
            return ValidateConfig(config).IsValid;
        }

        /// <summary>
        /// Validates a WAD configuration and returns a detailed result.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <returns>A ValidationResult containing any errors and warnings found.</returns>
        public ValidationResult ValidateConfig(IWadConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var result = new ValidationResult();

            // Validate IWAD
            if (config.IWad != null)
            {
                var iwadResult = Validate(config.IWad);
                result.Merge(iwadResult);
            }

            // Validate PWADs
            foreach (var pwad in config.PWads)
            {
                var pwadResult = Validate(pwad);
                result.Merge(pwadResult);
            }

            return result;
        }

        /// <summary>
        /// Validates all maps in the WAD.
        /// </summary>
        private void ValidateMaps(List<ILump> lumps, ValidationResult result)
        {
            for (int i = 0; i < lumps.Count; i++)
            {
                var lump = lumps[i];

                // Check if this is a map marker
                if (IsMapMarker(lump.Name))
                {
                    ValidateMap(lumps, i, lump.Name, result);
                }
            }
        }

        /// <summary>
        /// Validates a single map starting at the given index.
        /// </summary>
        private void ValidateMap(List<ILump> lumps, int mapIndex, string mapName, ValidationResult result)
        {
            // Check for UDMF format first
            if (mapIndex + 1 < lumps.Count && lumps[mapIndex + 1].Name.Equals("TEXTMAP", StringComparison.OrdinalIgnoreCase))
            {
                ValidateUdmfMap(lumps, mapIndex, mapName, result);
                return;
            }

            // Validate DOOM/Hexen format map
            ValidateClassicMap(lumps, mapIndex, mapName, result);
        }

        /// <summary>
        /// Validates a classic (DOOM/Hexen format) map.
        /// </summary>
        private void ValidateClassicMap(List<ILump> lumps, int mapIndex, string mapName, ValidationResult result)
        {
            // Find required map lumps
            var foundLumps = new Dictionary<string, ILump>(StringComparer.OrdinalIgnoreCase);

            // Scan up to 11 lumps after the map marker (standard max)
            int maxScan = Math.Min(mapIndex + 12, lumps.Count);
            for (int i = mapIndex + 1; i < maxScan; i++)
            {
                var lumpName = lumps[i].Name.ToUpperInvariant();

                // Stop if we hit another map marker or end marker
                if (IsMapMarker(lumpName) || lumpName == "ENDMAP")
                    break;

                if (RequiredMapLumps.Contains(lumpName) || OptionalMapLumps.Contains(lumpName) || lumpName == "BEHAVIOR")
                {
                    foundLumps[lumpName] = lumps[i];
                }
            }

            // Check for required lumps
            var missingLumps = RequiredMapLumps.Where(l => !foundLumps.ContainsKey(l)).ToList();
            if (missingLumps.Count > 0)
            {
                result.AddError(
                    ValidationCodes.MapMissingLumps,
                    $"Map is missing required lumps: {string.Join(", ", missingLumps)}",
                    mapName: mapName);
            }

            // Validate cross-references if we have the required lumps
            if (foundLumps.TryGetValue("VERTEXES", out var vertexLump) &&
                foundLumps.TryGetValue("LINEDEFS", out var linedefLump) &&
                foundLumps.TryGetValue("SIDEDEFS", out var sidedefLump) &&
                foundLumps.TryGetValue("SECTORS", out var sectorLump))
            {
                ValidateMapCrossReferences(vertexLump, linedefLump, sidedefLump, sectorLump, mapName, result);
            }

            // Check for things and player starts
            if (foundLumps.TryGetValue("THINGS", out var thingsLump))
            {
                ValidateThings(thingsLump, mapName, result);
            }
        }

        /// <summary>
        /// Validates a UDMF format map.
        /// </summary>
        private void ValidateUdmfMap(List<ILump> lumps, int mapIndex, string mapName, ValidationResult result)
        {
            // Find TEXTMAP lump
            if (mapIndex + 1 >= lumps.Count || !lumps[mapIndex + 1].Name.Equals("TEXTMAP", StringComparison.OrdinalIgnoreCase))
            {
                result.AddError(
                    ValidationCodes.MapMissingLumps,
                    "UDMF map is missing TEXTMAP lump",
                    mapName: mapName);
                return;
            }

            var textMapLump = lumps[mapIndex + 1];

            // Find ENDMAP marker
            bool foundEndmap = false;
            for (int i = mapIndex + 2; i < lumps.Count; i++)
            {
                if (lumps[i].Name.Equals("ENDMAP", StringComparison.OrdinalIgnoreCase))
                {
                    foundEndmap = true;
                    break;
                }
                if (IsMapMarker(lumps[i].Name))
                    break;
            }

            if (!foundEndmap)
            {
                result.AddError(
                    ValidationCodes.MapMissingLumps,
                    "UDMF map is missing ENDMAP marker",
                    mapName: mapName);
            }

            // Validate TEXTMAP content if it's the right type
            if (textMapLump is TextMapLump textMap)
            {
                ValidateUdmfMapData(textMap.Map, mapName, result);
            }
        }

        /// <summary>
        /// Validates UDMF map data cross-references.
        /// </summary>
        private void ValidateUdmfMapData(UdmfMap map, string mapName, ValidationResult result)
        {
            if (map == null) return;

            int vertexCount = map.VertexCount;
            int sidedefCount = map.SidedefCount;
            int sectorCount = map.SectorCount;

            // Validate linedefs
            for (int i = 0; i < map.LinedefCount; i++)
            {
                var linedef = map.Linedefs[i];

                // Validate vertex references
                if (linedef.V1 < 0 || linedef.V1 >= vertexCount)
                {
                    result.AddError(
                        ValidationCodes.LinedefInvalidStartVertex,
                        $"Linedef {i} references invalid start vertex {linedef.V1} (valid range: 0-{vertexCount - 1})",
                        mapName: mapName,
                        context: $"Linedef index: {i}");
                }

                if (linedef.V2 < 0 || linedef.V2 >= vertexCount)
                {
                    result.AddError(
                        ValidationCodes.LinedefInvalidEndVertex,
                        $"Linedef {i} references invalid end vertex {linedef.V2} (valid range: 0-{vertexCount - 1})",
                        mapName: mapName,
                        context: $"Linedef index: {i}");
                }

                // Validate sidedef references
                if (linedef.SideFront >= 0)
                {
                    if (linedef.SideFront >= sidedefCount)
                    {
                        result.AddError(
                            ValidationCodes.LinedefInvalidFrontSidedef,
                            $"Linedef {i} references invalid front sidedef {linedef.SideFront} (valid range: 0-{sidedefCount - 1})",
                            mapName: mapName,
                            context: $"Linedef index: {i}");
                    }
                }
                else
                {
                    result.AddError(
                        ValidationCodes.LinedefMissingFrontSidedef,
                        $"Linedef {i} has no front sidedef",
                        mapName: mapName,
                        context: $"Linedef index: {i}");
                }

                if (linedef.SideBack >= sidedefCount)
                {
                    result.AddError(
                        ValidationCodes.LinedefInvalidBackSidedef,
                        $"Linedef {i} references invalid back sidedef {linedef.SideBack} (valid range: 0-{sidedefCount - 1})",
                        mapName: mapName,
                        context: $"Linedef index: {i}");
                }

                // Check two-sided flag consistency
                if (linedef.TwoSided && linedef.SideBack < 0)
                {
                    result.AddWarning(
                        ValidationCodes.LinedefTwoSidedNoBack,
                        $"Linedef {i} has TwoSided flag but no back sidedef",
                        mapName: mapName,
                        context: $"Linedef index: {i}");
                }
            }

            // Validate sidedefs
            for (int i = 0; i < map.SidedefCount; i++)
            {
                var sidedef = map.Sidedefs[i];

                if (sidedef.Sector < 0 || sidedef.Sector >= sectorCount)
                {
                    result.AddError(
                        ValidationCodes.SidedefInvalidSector,
                        $"Sidedef {i} references invalid sector {sidedef.Sector} (valid range: 0-{sectorCount - 1})",
                        mapName: mapName,
                        context: $"Sidedef index: {i}");
                }
            }

            // Check for player starts
            if (map.ThingCount == 0)
            {
                result.AddWarning(
                    ValidationCodes.MapNoThings,
                    "Map has no things",
                    mapName: mapName);
            }
            else
            {
                bool hasPlayerStart = map.Things.Any(t => PlayerStartTypes.Contains(t.Type));
                if (!hasPlayerStart)
                {
                    result.AddWarning(
                        ValidationCodes.MapNoPlayerStart,
                        "Map has no player start (thing type 1)",
                        mapName: mapName);
                }
            }
        }

        /// <summary>
        /// Validates cross-references between map lumps.
        /// </summary>
        private void ValidateMapCrossReferences(ILump vertexLump, ILump linedefLump, ILump sidedefLump, ILump sectorLump, string mapName, ValidationResult result)
        {
            // Get counts from lumps
            int vertexCount = GetVertexCount(vertexLump);
            int sidedefCount = GetSidedefCount(sidedefLump);
            int sectorCount = GetSectorCount(sectorLump);

            if (vertexCount < 0 || sidedefCount < 0 || sectorCount < 0)
            {
                // Can't validate cross-references without proper lump types
                return;
            }

            // Validate linedefs
            ValidateLinedefs(linedefLump, vertexCount, sidedefCount, mapName, result);

            // Validate sidedefs
            ValidateSidedefs(sidedefLump, sectorCount, mapName, result);
        }

        /// <summary>
        /// Validates linedef references.
        /// </summary>
        private void ValidateLinedefs(ILump linedefLump, int vertexCount, int sidedefCount, string mapName, ValidationResult result)
        {
            if (linedefLump is LinedefsLump doomLinedefs)
            {
                for (int i = 0; i < doomLinedefs.Count; i++)
                {
                    var linedef = doomLinedefs[i];
                    ValidateDoomLinedef(linedef, i, vertexCount, sidedefCount, mapName, result);
                }
            }
            else if (linedefLump is HexenLinedefsLump hexenLinedefs)
            {
                for (int i = 0; i < hexenLinedefs.Count; i++)
                {
                    var linedef = hexenLinedefs[i];
                    ValidateHexenLinedef(linedef, i, vertexCount, sidedefCount, mapName, result);
                }
            }
        }

        /// <summary>
        /// Validates a DOOM format linedef.
        /// </summary>
        private void ValidateDoomLinedef(DoomLinedef linedef, int index, int vertexCount, int sidedefCount, string mapName, ValidationResult result)
        {
            // Validate vertex references
            if (linedef.StartVertex >= vertexCount)
            {
                result.AddError(
                    ValidationCodes.LinedefInvalidStartVertex,
                    $"Linedef {index} references invalid start vertex {linedef.StartVertex} (valid range: 0-{vertexCount - 1})",
                    lumpName: "LINEDEFS",
                    mapName: mapName);
            }

            if (linedef.EndVertex >= vertexCount)
            {
                result.AddError(
                    ValidationCodes.LinedefInvalidEndVertex,
                    $"Linedef {index} references invalid end vertex {linedef.EndVertex} (valid range: 0-{vertexCount - 1})",
                    lumpName: "LINEDEFS",
                    mapName: mapName);
            }

            // Validate sidedef references
            if (linedef.FrontSidedef != DoomLinedef.NoSidedef)
            {
                if (linedef.FrontSidedef >= sidedefCount)
                {
                    result.AddError(
                        ValidationCodes.LinedefInvalidFrontSidedef,
                        $"Linedef {index} references invalid front sidedef {linedef.FrontSidedef} (valid range: 0-{sidedefCount - 1})",
                        lumpName: "LINEDEFS",
                        mapName: mapName);
                }
            }
            else
            {
                result.AddError(
                    ValidationCodes.LinedefMissingFrontSidedef,
                    $"Linedef {index} has no front sidedef",
                    lumpName: "LINEDEFS",
                    mapName: mapName);
            }

            if (linedef.BackSidedef != DoomLinedef.NoSidedef && linedef.BackSidedef >= sidedefCount)
            {
                result.AddError(
                    ValidationCodes.LinedefInvalidBackSidedef,
                    $"Linedef {index} references invalid back sidedef {linedef.BackSidedef} (valid range: 0-{sidedefCount - 1})",
                    lumpName: "LINEDEFS",
                    mapName: mapName);
            }

            // Check two-sided flag consistency
            if (linedef.IsTwoSided && !linedef.HasBackSide)
            {
                result.AddWarning(
                    ValidationCodes.LinedefTwoSidedNoBack,
                    $"Linedef {index} has TwoSided flag but no back sidedef",
                    lumpName: "LINEDEFS",
                    mapName: mapName);
            }
        }

        /// <summary>
        /// Validates a Hexen format linedef.
        /// </summary>
        private void ValidateHexenLinedef(HexenLinedef linedef, int index, int vertexCount, int sidedefCount, string mapName, ValidationResult result)
        {
            // Validate vertex references
            if (linedef.StartVertex >= vertexCount)
            {
                result.AddError(
                    ValidationCodes.LinedefInvalidStartVertex,
                    $"Linedef {index} references invalid start vertex {linedef.StartVertex} (valid range: 0-{vertexCount - 1})",
                    lumpName: "LINEDEFS",
                    mapName: mapName);
            }

            if (linedef.EndVertex >= vertexCount)
            {
                result.AddError(
                    ValidationCodes.LinedefInvalidEndVertex,
                    $"Linedef {index} references invalid end vertex {linedef.EndVertex} (valid range: 0-{vertexCount - 1})",
                    lumpName: "LINEDEFS",
                    mapName: mapName);
            }

            // Validate sidedef references
            if (linedef.FrontSidedef != HexenLinedef.NoSidedef)
            {
                if (linedef.FrontSidedef >= sidedefCount)
                {
                    result.AddError(
                        ValidationCodes.LinedefInvalidFrontSidedef,
                        $"Linedef {index} references invalid front sidedef {linedef.FrontSidedef} (valid range: 0-{sidedefCount - 1})",
                        lumpName: "LINEDEFS",
                        mapName: mapName);
                }
            }
            else
            {
                result.AddError(
                    ValidationCodes.LinedefMissingFrontSidedef,
                    $"Linedef {index} has no front sidedef",
                    lumpName: "LINEDEFS",
                    mapName: mapName);
            }

            if (linedef.BackSidedef != HexenLinedef.NoSidedef && linedef.BackSidedef >= sidedefCount)
            {
                result.AddError(
                    ValidationCodes.LinedefInvalidBackSidedef,
                    $"Linedef {index} references invalid back sidedef {linedef.BackSidedef} (valid range: 0-{sidedefCount - 1})",
                    lumpName: "LINEDEFS",
                    mapName: mapName);
            }

            // Check two-sided flag consistency
            if (linedef.IsTwoSided && !linedef.HasBackSide)
            {
                result.AddWarning(
                    ValidationCodes.LinedefTwoSidedNoBack,
                    $"Linedef {index} has TwoSided flag but no back sidedef",
                    lumpName: "LINEDEFS",
                    mapName: mapName);
            }
        }

        /// <summary>
        /// Validates sidedef sector references.
        /// </summary>
        private void ValidateSidedefs(ILump sidedefLump, int sectorCount, string mapName, ValidationResult result)
        {
            if (sidedefLump is SidedefsLump sidedefs)
            {
                for (int i = 0; i < sidedefs.Count; i++)
                {
                    var sidedef = sidedefs[i];

                    if (sidedef.Sector >= sectorCount)
                    {
                        result.AddError(
                            ValidationCodes.SidedefInvalidSector,
                            $"Sidedef {i} references invalid sector {sidedef.Sector} (valid range: 0-{sectorCount - 1})",
                            lumpName: "SIDEDEFS",
                            mapName: mapName);
                    }
                }
            }
        }

        /// <summary>
        /// Validates things lump for player starts.
        /// </summary>
        private void ValidateThings(ILump thingsLump, string mapName, ValidationResult result)
        {
            if (thingsLump is ThingsLump things)
            {
                if (things.Count == 0)
                {
                    result.AddWarning(
                        ValidationCodes.MapNoThings,
                        "Map has no things",
                        lumpName: "THINGS",
                        mapName: mapName);
                    return;
                }

                bool hasPlayerStart = things.Things.Any(t => PlayerStartTypes.Contains(t.Type));
                if (!hasPlayerStart)
                {
                    result.AddWarning(
                        ValidationCodes.MapNoPlayerStart,
                        "Map has no player 1 start (thing type 1)",
                        lumpName: "THINGS",
                        mapName: mapName);
                }
            }
            else if (thingsLump is HexenThingsLump hexenThings)
            {
                if (hexenThings.Count == 0)
                {
                    result.AddWarning(
                        ValidationCodes.MapNoThings,
                        "Map has no things",
                        lumpName: "THINGS",
                        mapName: mapName);
                    return;
                }

                bool hasPlayerStart = hexenThings.Things.Any(t => PlayerStartTypes.Contains(t.Type));
                if (!hasPlayerStart)
                {
                    result.AddWarning(
                        ValidationCodes.MapNoPlayerStart,
                        "Map has no player 1 start (thing type 1)",
                        lumpName: "THINGS",
                        mapName: mapName);
                }
            }
        }

        /// <summary>
        /// Validates marker pairs in the WAD.
        /// </summary>
        private void ValidateMarkerPairs(List<ILump> lumps, ValidationResult result)
        {
            var markerStack = new Stack<(string Name, int Index)>();
            var lumpNames = lumps.Select(l => l.Name.ToUpperInvariant()).ToList();

            for (int i = 0; i < lumpNames.Count; i++)
            {
                var name = lumpNames[i];

                // Check if this is a start marker
                var startPair = MarkerPairs.FirstOrDefault(p => p.Start.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (startPair.Start != null)
                {
                    // Check for nesting
                    if (markerStack.Count > 0)
                    {
                        var current = markerStack.Peek();
                        // Only report nesting for the same marker type
                        if (current.Name == startPair.Start)
                        {
                            result.AddWarning(
                                ValidationCodes.MarkerNested,
                                $"Nested marker {name} found inside previous {current.Name}",
                                lumpName: name,
                                context: $"Index: {i}, Previous at index: {current.Index}");
                        }
                    }
                    markerStack.Push((startPair.Start, i));
                    continue;
                }

                // Check if this is an end marker
                var endPair = MarkerPairs.FirstOrDefault(p => p.End.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (endPair.End != null)
                {
                    if (markerStack.Count == 0)
                    {
                        result.AddError(
                            ValidationCodes.MarkerUnpaired,
                            $"End marker {name} has no matching start marker",
                            lumpName: name,
                            context: $"Index: {i}");
                    }
                    else
                    {
                        var start = markerStack.Pop();
                        if (start.Name != endPair.Start)
                        {
                            result.AddError(
                                ValidationCodes.MarkerUnpaired,
                                $"End marker {name} does not match start marker {start.Name}",
                                lumpName: name,
                                context: $"End index: {i}, Start index: {start.Index}");
                        }
                        else
                        {
                            // Check if the section is empty
                            int contentCount = i - start.Index - 1;
                            if (contentCount == 0)
                            {
                                result.AddWarning(
                                    ValidationCodes.MarkerEmpty,
                                    $"Marker section {start.Name}/{name} is empty",
                                    lumpName: start.Name,
                                    context: $"Start index: {start.Index}, End index: {i}");
                            }
                        }
                    }
                }
            }

            // Report any unclosed start markers
            while (markerStack.Count > 0)
            {
                var unclosed = markerStack.Pop();
                var expectedEnd = MarkerPairs.First(p => p.Start == unclosed.Name).End;
                result.AddError(
                    ValidationCodes.MarkerUnpaired,
                    $"Start marker {unclosed.Name} has no matching {expectedEnd}",
                    lumpName: unclosed.Name,
                    context: $"Index: {unclosed.Index}");
            }
        }

        /// <summary>
        /// Validates resource dependencies.
        /// </summary>
        private void ValidateResources(List<ILump> lumps, ValidationResult result)
        {
            var lumpNames = new HashSet<string>(lumps.Select(l => l.Name.ToUpperInvariant()));

            bool hasPlaypal = lumpNames.Contains("PLAYPAL");
            bool hasColormap = lumpNames.Contains("COLORMAP");

            // Check for graphics lumps that require PLAYPAL
            bool hasGraphicsLumps = lumpNames.Intersect(KnownGraphicsLumps).Any();

            // Check for flats (require PLAYPAL)
            bool hasFlats = lumps.Any(l => l is FlatLump);

            // Check for pictures (require PLAYPAL)
            bool hasPictures = lumps.Any(l => l is PictureLump);

            // Check for sprites (between S_START/S_END or SS_START/SS_END)
            bool hasSprites = HasMarkerSection(lumps, "S_START", "S_END") ||
                              HasMarkerSection(lumps, "SS_START", "SS_END");

            if ((hasGraphicsLumps || hasFlats || hasPictures || hasSprites) && !hasPlaypal)
            {
                result.AddWarning(
                    ValidationCodes.ResourceMissingPlaypal,
                    "WAD contains graphics lumps but no PLAYPAL lump",
                    context: "Graphics rendering will require a PLAYPAL from another source");
            }

            if ((hasFlats || hasPictures || hasSprites) && !hasColormap)
            {
                result.AddWarning(
                    ValidationCodes.ResourceMissingColormap,
                    "WAD contains graphics lumps but no COLORMAP lump",
                    context: "Light level effects will require a COLORMAP from another source");
            }
        }

        /// <summary>
        /// Checks if a marker section exists.
        /// </summary>
        private bool HasMarkerSection(List<ILump> lumps, string startMarker, string endMarker)
        {
            bool foundStart = false;
            foreach (var lump in lumps)
            {
                if (lump.Name.Equals(startMarker, StringComparison.OrdinalIgnoreCase))
                {
                    foundStart = true;
                }
                else if (foundStart && lump.Name.Equals(endMarker, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a lump name is a map marker.
        /// </summary>
        private bool IsMapMarker(string name)
        {
            return DoomMapPattern.IsMatch(name) || Doom2MapPattern.IsMatch(name);
        }

        /// <summary>
        /// Gets the vertex count from a vertex lump.
        /// </summary>
        private int GetVertexCount(ILump lump)
        {
            if (lump is VertexesLump vertexes)
                return vertexes.Count;
            return -1;
        }

        /// <summary>
        /// Gets the sidedef count from a sidedef lump.
        /// </summary>
        private int GetSidedefCount(ILump lump)
        {
            if (lump is SidedefsLump sidedefs)
                return sidedefs.Count;
            return -1;
        }

        /// <summary>
        /// Gets the sector count from a sector lump.
        /// </summary>
        private int GetSectorCount(ILump lump)
        {
            if (lump is SectorsLump sectors)
                return sectors.Count;
            return -1;
        }
    }
}
