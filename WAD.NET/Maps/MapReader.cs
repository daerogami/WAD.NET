using System;
using System.Collections.Generic;
using System.Linq;
using WAD.NET.Concrete;
using WAD.NET.Concrete.Maps;
using WAD.NET.Definitions.Classic.Map;
using WAD.NET.Definitions.Hexen;
using WAD.NET.Enums;
using WAD.NET.Interfaces;

namespace WAD.NET.Maps
{
    /// <summary>
    /// Reads and assembles maps from WAD lumps.
    /// </summary>
    public class MapReader
    {
        /// <summary>
        /// Reads all maps from a WAD.
        /// </summary>
        /// <param name="wad">The WAD to read maps from.</param>
        /// <returns>A list of all maps in the WAD.</returns>
        public IReadOnlyList<IMap> ReadMaps(Wad wad)
        {
            var maps = new List<IMap>();
            var lumps = wad.Lumps.ToArray();

            for (int i = 0; i < lumps.Length; i++)
            {
                var lump = lumps[i];
                if (!MapDetector.IsMapMarker(lump.Name))
                    continue;

                // Collect map lumps following the marker
                var mapLumps = CollectMapLumps(lumps, i + 1);
                if (mapLumps.Count == 0)
                    continue;

                var format = MapDetector.DetectFormat(mapLumps.Keys);
                var map = CreateMap(lump.Name, format, mapLumps);

                if (map != null)
                {
                    maps.Add(map);
                }
            }

            return maps;
        }

        /// <summary>
        /// Reads a specific map from a WAD by name.
        /// </summary>
        /// <param name="wad">The WAD to read from.</param>
        /// <param name="mapName">The map name (e.g., E1M1, MAP01).</param>
        /// <returns>The map, or null if not found.</returns>
        public IMap ReadMap(Wad wad, string mapName)
        {
            var lumps = wad.Lumps.ToArray();

            for (int i = 0; i < lumps.Length; i++)
            {
                var lump = lumps[i];
                if (!string.Equals(lump.Name, mapName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!MapDetector.IsMapMarker(lump.Name))
                    continue;

                var mapLumps = CollectMapLumps(lumps, i + 1);
                if (mapLumps.Count == 0)
                    return null;

                var format = MapDetector.DetectFormat(mapLumps.Keys);
                return CreateMap(lump.Name, format, mapLumps);
            }

            return null;
        }

        /// <summary>
        /// Gets the names of all maps in a WAD.
        /// </summary>
        /// <param name="wad">The WAD to scan.</param>
        /// <returns>List of map names.</returns>
        public IReadOnlyList<string> GetMapNames(Wad wad)
        {
            return wad.Lumps
                .Where(l => MapDetector.IsMapMarker(l.Name))
                .Select(l => l.Name)
                .ToList();
        }

        private Dictionary<string, ILump> CollectMapLumps(ILump[] lumps, int startIndex)
        {
            var mapLumps = new Dictionary<string, ILump>(StringComparer.OrdinalIgnoreCase);

            for (int i = startIndex; i < lumps.Length; i++)
            {
                var lump = lumps[i];

                // Stop when we hit another map marker or non-map lump
                if (MapDetector.IsMapMarker(lump.Name))
                    break;

                if (!MapDetector.IsMapLump(lump.Name))
                    break;

                // ENDMAP signals end of UDMF map
                if (lump.Name == "ENDMAP")
                {
                    mapLumps[lump.Name] = lump;
                    break;
                }

                mapLumps[lump.Name] = lump;
            }

            return mapLumps;
        }

        private IMap CreateMap(string name, MapFormat format, Dictionary<string, ILump> lumps)
        {
            return format switch
            {
                MapFormat.Doom => CreateDoomMap(name, lumps),
                MapFormat.Hexen => CreateHexenMap(name, lumps),
                MapFormat.UDMF => CreateUdmfMap(name, lumps),
                _ => null
            };
        }

        private DoomMap CreateDoomMap(string name, Dictionary<string, ILump> lumps)
        {
            return new DoomMap
            {
                Name = name,
                Things = GetLump<ThingsLump>(lumps, "THINGS")?.Things ?? Array.Empty<DoomThing>(),
                Vertices = GetLump<VertexesLump>(lumps, "VERTEXES")?.Vertices ?? Array.Empty<MapVertex>(),
                Linedefs = GetLump<LinedefsLump>(lumps, "LINEDEFS")?.Linedefs ?? Array.Empty<DoomLinedef>(),
                Sidedefs = GetLump<SidedefsLump>(lumps, "SIDEDEFS")?.Sidedefs ?? Array.Empty<DoomSidedef>(),
                Sectors = GetLump<SectorsLump>(lumps, "SECTORS")?.Sectors ?? Array.Empty<DoomSector>(),
                Segs = GetLump<SegsLump>(lumps, "SEGS")?.Segs ?? Array.Empty<Seg>(),
                Subsectors = GetLump<SubsectorsLump>(lumps, "SSECTORS")?.Subsectors ?? Array.Empty<Subsector>(),
                Nodes = GetLump<NodesLump>(lumps, "NODES")?.Nodes ?? Array.Empty<Node>(),
                RejectTable = GetLump<RejectLump>(lumps, "REJECT")?.Data ?? Array.Empty<byte>(),
                BlockmapData = GetLump<BlockmapLump>(lumps, "BLOCKMAP")?.Data ?? Array.Empty<byte>()
            };
        }

        private HexenMap CreateHexenMap(string name, Dictionary<string, ILump> lumps)
        {
            // For Hexen, we need to re-parse THINGS and LINEDEFS lumps as Hexen format
            // Get the raw binary data and parse as Hexen format
            var thingsLump = GetLump<ILump>(lumps, "THINGS");
            var linedefsLump = GetLump<ILump>(lumps, "LINEDEFS");

            HexenThing[] things = Array.Empty<HexenThing>();
            HexenLinedef[] linedefs = Array.Empty<HexenLinedef>();

            if (thingsLump is BinaryLump binaryThings)
            {
                var hexenThingsLump = new HexenThingsLump("THINGS", binaryThings.SourceWad, binaryThings.Data);
                things = hexenThingsLump.Things;
            }
            else if (thingsLump is ThingsLump doomThings)
            {
                // Already parsed as Doom format - need to re-parse
                // This is a limitation; ideally we'd have raw data access
                things = Array.Empty<HexenThing>();
            }

            if (linedefsLump is BinaryLump binaryLinedefs)
            {
                var hexenLinedefsLump = new HexenLinedefsLump("LINEDEFS", binaryLinedefs.SourceWad, binaryLinedefs.Data);
                linedefs = hexenLinedefsLump.Linedefs;
            }
            else if (linedefsLump is LinedefsLump doomLinedefs)
            {
                // Already parsed as Doom format - need to re-parse
                linedefs = Array.Empty<HexenLinedef>();
            }

            return new HexenMap
            {
                Name = name,
                Things = things,
                Vertices = GetLump<VertexesLump>(lumps, "VERTEXES")?.Vertices ?? Array.Empty<MapVertex>(),
                Linedefs = linedefs,
                Sidedefs = GetLump<SidedefsLump>(lumps, "SIDEDEFS")?.Sidedefs ?? Array.Empty<DoomSidedef>(),
                Sectors = GetLump<SectorsLump>(lumps, "SECTORS")?.Sectors ?? Array.Empty<DoomSector>(),
                Segs = GetLump<SegsLump>(lumps, "SEGS")?.Segs ?? Array.Empty<Seg>(),
                Subsectors = GetLump<SubsectorsLump>(lumps, "SSECTORS")?.Subsectors ?? Array.Empty<Subsector>(),
                Nodes = GetLump<NodesLump>(lumps, "NODES")?.Nodes ?? Array.Empty<Node>(),
                RejectTable = GetLump<RejectLump>(lumps, "REJECT")?.Data ?? Array.Empty<byte>(),
                BlockmapData = GetLump<BlockmapLump>(lumps, "BLOCKMAP")?.Data ?? Array.Empty<byte>(),
                BehaviorData = GetLump<BehaviorLump>(lumps, "BEHAVIOR")?.Data ?? Array.Empty<byte>()
            };
        }

        private UdmfMapWrapper CreateUdmfMap(string name, Dictionary<string, ILump> lumps)
        {
            var textMapLump = GetLump<TextMapLump>(lumps, "TEXTMAP");

            return new UdmfMapWrapper
            {
                Name = name,
                Map = textMapLump?.Map
            };
        }

        private T GetLump<T>(Dictionary<string, ILump> lumps, string name) where T : class
        {
            if (lumps.TryGetValue(name, out var lump))
            {
                return lump as T;
            }
            return null;
        }
    }
}
