using System;
using System.Collections.Generic;
using System.Linq;
using WAD.NET.Archives;
using WAD.NET.Concrete;
using WAD.NET.Definitions;
using WAD.NET.Enums;

namespace WAD.NET.Resources
{
    /// <summary>
    /// Provides unified access to WAD resources including palettes, textures, sprites, and flats.
    /// </summary>
    public class ResourceManager : IDisposable
    {
        private readonly IArchiveReader _reader;
        private readonly bool _ownsReader;

        private Palette _defaultPalette;
        private Palette[] _palettes;
        private byte[][] _colorMaps;
        private string[] _patchNames;
        private TextureDefinition[] _textures;
        private readonly Dictionary<string, DoomPicture> _patchCache;
        private readonly Dictionary<string, DoomPicture> _spriteCache;
        private readonly Dictionary<string, FlatLump> _flatCache;

        /// <summary>
        /// Creates a ResourceManager for the specified archive reader.
        /// </summary>
        /// <param name="reader">The archive reader to use.</param>
        /// <param name="ownsReader">If true, the reader will be disposed when this manager is disposed.</param>
        public ResourceManager(IArchiveReader reader, bool ownsReader = false)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _ownsReader = ownsReader;
            _patchCache = new Dictionary<string, DoomPicture>(StringComparer.OrdinalIgnoreCase);
            _spriteCache = new Dictionary<string, DoomPicture>(StringComparer.OrdinalIgnoreCase);
            _flatCache = new Dictionary<string, FlatLump>(StringComparer.OrdinalIgnoreCase);

            LoadSystemLumps();
        }

        /// <summary>
        /// Gets the default palette (palette 0).
        /// </summary>
        public Palette DefaultPalette => _defaultPalette;

        /// <summary>
        /// Gets all palettes (14 in standard DOOM).
        /// </summary>
        public Palette[] Palettes => _palettes ?? Array.Empty<Palette>();

        /// <summary>
        /// Gets the color maps.
        /// </summary>
        public byte[][] ColorMaps => _colorMaps ?? Array.Empty<byte[]>();

        /// <summary>
        /// Gets the patch names from PNAMES.
        /// </summary>
        public string[] PatchNames => _patchNames ?? Array.Empty<string>();

        /// <summary>
        /// Gets all texture definitions.
        /// </summary>
        public TextureDefinition[] Textures => _textures ?? Array.Empty<TextureDefinition>();

        private void LoadSystemLumps()
        {
            // Load PLAYPAL
            var playpalEntry = _reader.GetEntry("PLAYPAL");
            if (playpalEntry != null)
            {
                var data = _reader.ReadLump(playpalEntry);
                var paletteLump = new PaletteLump("PLAYPAL", _reader.Path, data);
                _palettes = paletteLump.Palettes;
                _defaultPalette = paletteLump.NormalPalette;
            }

            // Load COLORMAP
            var colormapEntry = _reader.GetEntry("COLORMAP");
            if (colormapEntry != null)
            {
                var data = _reader.ReadLump(colormapEntry);
                var colorMapLump = new ColorMapLump("COLORMAP", _reader.Path, data);
                _colorMaps = colorMapLump.Maps;
            }

            // Load PNAMES
            var pnamesEntry = _reader.GetEntry("PNAMES");
            if (pnamesEntry != null)
            {
                var data = _reader.ReadLump(pnamesEntry);
                var pnamesLump = new PatchNamesLump("PNAMES", _reader.Path, data);
                _patchNames = pnamesLump.PatchNames;
            }

            // Load TEXTURE1 and TEXTURE2
            var textureList = new List<TextureDefinition>();

            var texture1Entry = _reader.GetEntry("TEXTURE1");
            if (texture1Entry != null)
            {
                var data = _reader.ReadLump(texture1Entry);
                var textureLump = new TextureLump("TEXTURE1", _reader.Path, data);
                textureList.AddRange(textureLump.Textures);
            }

            var texture2Entry = _reader.GetEntry("TEXTURE2");
            if (texture2Entry != null)
            {
                var data = _reader.ReadLump(texture2Entry);
                var textureLump = new TextureLump("TEXTURE2", _reader.Path, data);
                textureList.AddRange(textureLump.Textures);
            }

            _textures = textureList.ToArray();
        }

        /// <summary>
        /// Gets a specific palette by index.
        /// </summary>
        /// <param name="index">Palette index (0-13 for standard DOOM).</param>
        public Palette GetPalette(int index)
        {
            if (_palettes == null || index < 0 || index >= _palettes.Length)
                return _defaultPalette;
            return _palettes[index];
        }

        /// <summary>
        /// Gets a sprite picture by lump name.
        /// </summary>
        /// <param name="name">The sprite lump name (e.g., "POSSA1").</param>
        /// <returns>The parsed picture, or null if not found.</returns>
        public DoomPicture GetSprite(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            if (_spriteCache.TryGetValue(name, out var cached))
                return cached;

            var entry = _reader.GetEntry(name);
            if (entry == null)
                return null;

            try
            {
                var data = _reader.ReadLump(entry);
                var picture = PictureLump.ParsePicture(name, data);
                _spriteCache[name] = picture;
                return picture;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a patch picture by name.
        /// </summary>
        /// <param name="name">The patch name.</param>
        /// <returns>The parsed picture, or null if not found.</returns>
        public DoomPicture GetPatch(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            if (_patchCache.TryGetValue(name, out var cached))
                return cached;

            var entry = _reader.GetEntry(name);
            if (entry == null)
                return null;

            try
            {
                var data = _reader.ReadLump(entry);
                var picture = PictureLump.ParsePicture(name, data);
                _patchCache[name] = picture;
                return picture;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a patch by index from PNAMES.
        /// </summary>
        public DoomPicture GetPatch(int index)
        {
            if (_patchNames == null || index < 0 || index >= _patchNames.Length)
                return null;
            return GetPatch(_patchNames[index]);
        }

        /// <summary>
        /// Finds a texture definition by name.
        /// </summary>
        public TextureDefinition FindTexture(string name)
        {
            if (_textures == null || string.IsNullOrEmpty(name))
                return null;

            return _textures.FirstOrDefault(t =>
                string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Builds a composite texture by name.
        /// </summary>
        /// <param name="name">The texture name.</param>
        /// <returns>Palette-indexed pixel data, or null if not found.</returns>
        public byte[] BuildTexture(string name)
        {
            var definition = FindTexture(name);
            if (definition == null)
                return null;

            return BuildTexture(definition);
        }

        /// <summary>
        /// Builds a composite texture from a definition.
        /// </summary>
        public byte[] BuildTexture(TextureDefinition definition)
        {
            if (definition == null)
                return null;

            // Collect required patches
            var patches = new Dictionary<string, DoomPicture>(StringComparer.OrdinalIgnoreCase);
            foreach (var patchRef in definition.Patches)
            {
                if (_patchNames != null && patchRef.PatchIndex < _patchNames.Length)
                {
                    var patchName = _patchNames[patchRef.PatchIndex];
                    if (!patches.ContainsKey(patchName))
                    {
                        var patch = GetPatch(patchName);
                        if (patch != null)
                            patches[patchName] = patch;
                    }
                }
            }

            var builder = new TextureBuilder(definition, _patchNames, patches, _defaultPalette);
            return builder.BuildIndexed();
        }

        /// <summary>
        /// Builds a composite texture as RGBA data.
        /// </summary>
        public byte[] BuildTextureRgba(string name)
        {
            var definition = FindTexture(name);
            if (definition == null || _defaultPalette == null)
                return null;

            // Collect required patches
            var patches = new Dictionary<string, DoomPicture>(StringComparer.OrdinalIgnoreCase);
            foreach (var patchRef in definition.Patches)
            {
                if (_patchNames != null && patchRef.PatchIndex < _patchNames.Length)
                {
                    var patchName = _patchNames[patchRef.PatchIndex];
                    if (!patches.ContainsKey(patchName))
                    {
                        var patch = GetPatch(patchName);
                        if (patch != null)
                            patches[patchName] = patch;
                    }
                }
            }

            var builder = new TextureBuilder(definition, _patchNames, patches, _defaultPalette);
            return builder.BuildRgba();
        }

        /// <summary>
        /// Gets a flat by name.
        /// </summary>
        /// <param name="name">The flat name (e.g., "FLOOR0_1").</param>
        public FlatLump GetFlat(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            if (_flatCache.TryGetValue(name, out var cached))
                return cached;

            var entry = _reader.GetEntry(name);
            if (entry == null || entry.Size != FlatLump.Size)
                return null;

            try
            {
                var data = _reader.ReadLump(entry);
                var flat = new FlatLump(name, _reader.Path, data);
                _flatCache[name] = flat;
                return flat;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets all sprite lump entries.
        /// </summary>
        public IEnumerable<LumpEntry> GetSpriteEntries()
        {
            return _reader.GetEntries().Where(e => e.Category == LumpCategory.Sprite);
        }

        /// <summary>
        /// Gets all flat lump entries.
        /// </summary>
        public IEnumerable<LumpEntry> GetFlatEntries()
        {
            return _reader.GetEntries().Where(e => e.Category == LumpCategory.Flat);
        }

        /// <summary>
        /// Gets all patch lump entries.
        /// </summary>
        public IEnumerable<LumpEntry> GetPatchEntries()
        {
            return _reader.GetEntries().Where(e => e.Category == LumpCategory.Patch);
        }

        /// <summary>
        /// Clears all caches.
        /// </summary>
        public void ClearCaches()
        {
            _patchCache.Clear();
            _spriteCache.Clear();
            _flatCache.Clear();
        }

        public void Dispose()
        {
            if (_ownsReader)
            {
                _reader?.Dispose();
            }
        }
    }
}
