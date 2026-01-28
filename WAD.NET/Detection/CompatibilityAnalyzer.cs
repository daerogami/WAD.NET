using System;
using System.Text;
using WAD.NET.Archives;
using WAD.NET.Enums;
using WAD.NET.Maps;
using WAD.NET.SourcePorts.Boom;
using WAD.NET.SourcePorts.MBF21;

namespace WAD.NET.Detection
{
    /// <summary>
    /// Analyzes a WAD/PK3 archive to determine its source port compatibility.
    /// </summary>
    public class CompatibilityAnalyzer
    {
        /// <summary>
        /// Analyzes an archive for compatibility requirements.
        /// </summary>
        /// <param name="archive">The archive to analyze.</param>
        /// <returns>Compatibility information for the archive.</returns>
        public ModCompatibility Analyze(IArchiveReader archive)
        {
            if (archive == null)
                throw new ArgumentNullException(nameof(archive));

            var result = new ModCompatibility
            {
                MinimumPort = SourcePort.Vanilla
            };

            // Check for ZScript (highest requirement)
            CheckZScript(archive, result);

            // Check for DECORATE
            CheckDecorate(archive, result);

            // Check for MAPINFO variants
            CheckMapInfo(archive, result);

            // Check for DEHACKED
            CheckDehacked(archive, result);

            // Check for SNDINFO
            CheckSndInfo(archive, result);

            // Check for ACS
            CheckACS(archive, result);

            // Check for UMAPINFO
            CheckUMapInfo(archive, result);

            // Check for specific lumps that indicate port requirements
            CheckSpecificLumps(archive, result);

            // Analyze maps for Boom features
            CheckMapsForBoomFeatures(archive, result);

            return result;
        }

        private void CheckZScript(IArchiveReader archive, ModCompatibility result)
        {
            var entry = archive.GetEntry("ZSCRIPT");
            if (entry != null)
            {
                result.UsesZScript = true;
                UpgradeMinimum(result, SourcePort.GZDoom);
                result.RequiredFeatures.Add("ZScript");

                try
                {
                    var data = archive.ReadLump(entry);
                    var content = Encoding.UTF8.GetString(data);
                    var info = ZScriptScanner.Scan(content);

                    result.ActorCount += info.Classes.Count;

                    if (!string.IsNullOrEmpty(info.Version))
                    {
                        result.RequiredFeatures.Add($"ZScript version {info.Version}");
                    }
                }
                catch
                {
                    result.Warnings.Add("Could not parse ZSCRIPT lump");
                }
            }
        }

        private void CheckDecorate(IArchiveReader archive, ModCompatibility result)
        {
            var entry = archive.GetEntry("DECORATE");
            if (entry != null)
            {
                result.UsesDecorate = true;
                UpgradeMinimum(result, SourcePort.ZDoom);
                result.RequiredFeatures.Add("DECORATE");

                try
                {
                    var data = archive.ReadLump(entry);
                    var content = Encoding.UTF8.GetString(data);
                    var info = DecorateScanner.Scan(content);

                    result.ActorCount += info.Actors.Count;
                }
                catch
                {
                    result.Warnings.Add("Could not parse DECORATE lump");
                }
            }
        }

        private void CheckMapInfo(IArchiveReader archive, ModCompatibility result)
        {
            // Check for ZMAPINFO (ZDoom extended)
            if (archive.Contains("ZMAPINFO"))
            {
                result.UsesMapInfo = true;
                UpgradeMinimum(result, SourcePort.ZDoom);
                result.RequiredFeatures.Add("ZMAPINFO");
            }

            // Check for MAPINFO (generic ZDoom)
            if (archive.Contains("MAPINFO"))
            {
                result.UsesMapInfo = true;
                UpgradeMinimum(result, SourcePort.ZDoom);
                result.RequiredFeatures.Add("MAPINFO");
            }

            // Check for EMAPINFO (Eternity)
            if (archive.Contains("EMAPINFO"))
            {
                result.UsesMapInfo = true;
                UpgradeMinimum(result, SourcePort.Eternity);
                result.RequiredFeatures.Add("EMAPINFO");
            }
        }

        private void CheckDehacked(IArchiveReader archive, ModCompatibility result)
        {
            var entry = archive.GetEntry("DEHACKED");
            if (entry != null)
            {
                result.UsesDehacked = true;
                result.RequiredFeatures.Add("DEHACKED");

                try
                {
                    var data = archive.ReadLump(entry);
                    var content = Encoding.ASCII.GetString(data);

                    // Check for MBF21 features
                    if (MBF21Detector.ContainsMBF21Features(content))
                    {
                        UpgradeMinimum(result, SourcePort.MBF21);
                        result.RequiredFeatures.Add("MBF21 DEHACKED");
                    }
                    // Check for MBF features
                    else if (content.Contains("[CODEPTR]") || content.Contains("HELPER"))
                    {
                        UpgradeMinimum(result, SourcePort.MBF);
                        result.RequiredFeatures.Add("MBF DEHACKED");
                    }
                    // Check for Boom features
                    else if (content.Contains("BEX") || content.Contains("[STRINGS]") ||
                             content.Contains("[PARS]") || content.Contains("[CHEATS]"))
                    {
                        UpgradeMinimum(result, SourcePort.Boom);
                        result.RequiredFeatures.Add("BEX Extensions");
                    }
                }
                catch
                {
                    result.Warnings.Add("Could not parse DEHACKED lump");
                }
            }
        }

        private void CheckSndInfo(IArchiveReader archive, ModCompatibility result)
        {
            if (archive.Contains("SNDINFO"))
            {
                result.UsesSndInfo = true;
                UpgradeMinimum(result, SourcePort.ZDoom);
                result.RequiredFeatures.Add("SNDINFO");
            }

            // SNDSEQ is also ZDoom-specific
            if (archive.Contains("SNDSEQ"))
            {
                UpgradeMinimum(result, SourcePort.ZDoom);
                result.RequiredFeatures.Add("SNDSEQ");
            }
        }

        private void CheckACS(IArchiveReader archive, ModCompatibility result)
        {
            // Check for BEHAVIOR lumps (compiled ACS) or SCRIPTS lump
            foreach (var entry in archive.GetEntries())
            {
                if (entry.Name.Equals("BEHAVIOR", StringComparison.OrdinalIgnoreCase) ||
                    entry.Name.Equals("SCRIPTS", StringComparison.OrdinalIgnoreCase))
                {
                    result.UsesACS = true;
                    UpgradeMinimum(result, SourcePort.ZDoom);
                    result.RequiredFeatures.Add("ACS Scripts");
                    break;
                }
            }

            // Check for LOADACS
            if (archive.Contains("LOADACS"))
            {
                result.UsesACS = true;
                UpgradeMinimum(result, SourcePort.ZDoom);
                result.RequiredFeatures.Add("LOADACS");
            }
        }

        private void CheckUMapInfo(IArchiveReader archive, ModCompatibility result)
        {
            if (archive.Contains("UMAPINFO"))
            {
                result.UsesUMapInfo = true;
                UpgradeMinimum(result, SourcePort.MBF21);
                result.RequiredFeatures.Add("UMAPINFO");
            }
        }

        private void CheckSpecificLumps(IArchiveReader archive, ModCompatibility result)
        {
            // GLDEFS indicates OpenGL features
            if (archive.Contains("GLDEFS") || archive.Contains("DOOMDEFS"))
            {
                UpgradeMinimum(result, SourcePort.GZDoom);
                result.RequiredFeatures.Add("OpenGL Definitions");
            }

            // MODELDEF indicates 3D models
            if (archive.Contains("MODELDEF"))
            {
                UpgradeMinimum(result, SourcePort.GZDoom);
                result.RequiredFeatures.Add("3D Models");
            }

            // VOXELDEF indicates voxels
            if (archive.Contains("VOXELDEF"))
            {
                UpgradeMinimum(result, SourcePort.GZDoom);
                result.RequiredFeatures.Add("Voxels");
            }

            // ANIMDEFS can be Boom or ZDoom
            if (archive.Contains("ANIMDEFS"))
            {
                UpgradeMinimum(result, SourcePort.Boom);
                result.RequiredFeatures.Add("ANIMDEFS");
            }

            // TEXTURES lump (ZDoom texture definitions)
            if (archive.Contains("TEXTURES"))
            {
                UpgradeMinimum(result, SourcePort.ZDoom);
                result.RequiredFeatures.Add("TEXTURES Definitions");
            }

            // TERRAIN (ZDoom)
            if (archive.Contains("TERRAIN"))
            {
                UpgradeMinimum(result, SourcePort.ZDoom);
                result.RequiredFeatures.Add("TERRAIN");
            }

            // X11R6RGB (color names)
            if (archive.Contains("X11R6RGB"))
            {
                UpgradeMinimum(result, SourcePort.ZDoom);
            }

            // LANGUAGE (translations)
            if (archive.Contains("LANGUAGE"))
            {
                UpgradeMinimum(result, SourcePort.ZDoom);
                result.RequiredFeatures.Add("LANGUAGE Definitions");
            }

            // KEYCONF (key bindings)
            if (archive.Contains("KEYCONF"))
            {
                UpgradeMinimum(result, SourcePort.ZDoom);
            }

            // LOCKDEFS
            if (archive.Contains("LOCKDEFS"))
            {
                UpgradeMinimum(result, SourcePort.ZDoom);
                result.RequiredFeatures.Add("LOCKDEFS");
            }

            // MENUDEF
            if (archive.Contains("MENUDEF"))
            {
                UpgradeMinimum(result, SourcePort.ZDoom);
                result.RequiredFeatures.Add("MENUDEF");
            }

            // CVARINFO
            if (archive.Contains("CVARINFO"))
            {
                UpgradeMinimum(result, SourcePort.ZDoom);
                result.RequiredFeatures.Add("CVARINFO");
            }
        }

        private void CheckMapsForBoomFeatures(IArchiveReader archive, ModCompatibility result)
        {
            // For WAD archives, we can check for Boom features in maps
            if (archive is WadArchiveReader wadArchive)
            {
                try
                {
                    var mapReader = new MapReader();
                    foreach (var mapName in wadArchive.GetMapNames())
                    {
                        try
                        {
                            // Check for UDMF
                            string textmapName = mapName + "+TEXTMAP";
                            if (wadArchive.Contains("TEXTMAP"))
                            {
                                result.MapFormats.Add("UDMF");
                                UpgradeMinimum(result, SourcePort.ZDoom);
                                result.RequiredFeatures.Add("UDMF Maps");
                                continue;
                            }

                            // Check for Hexen format (BEHAVIOR lump in map)
                            // This is harder to detect without fully parsing the map

                            result.MapFormats.Add("Doom/Hexen Binary");
                        }
                        catch
                        {
                            // Ignore parsing errors
                        }
                    }

                    // Detect Boom features from maps if possible
                    // This would require reading and analyzing linedefs
                }
                catch
                {
                    result.Warnings.Add("Could not analyze maps for Boom features");
                }
            }
        }

        private void UpgradeMinimum(ModCompatibility result, SourcePort port)
        {
            if (port > result.MinimumPort)
                result.MinimumPort = port;
        }
    }
}
