using System;
using System.Collections.Generic;

namespace WAD.NET.Definitions
{
    /// <summary>
    /// Builds composite textures from patches according to texture definitions.
    /// </summary>
    public class TextureBuilder
    {
        private readonly TextureDefinition _definition;
        private readonly string[]? _patchNames;
        private readonly Dictionary<string, DoomPicture> _patches;
        private readonly Palette? _palette;

        /// <summary>
        /// Creates a texture builder with explicit patch names.
        /// </summary>
        /// <param name="definition">The texture definition to build.</param>
        /// <param name="patchNames">Array of patch names from PNAMES lump.</param>
        /// <param name="patches">Dictionary of patch pictures by name.</param>
        /// <param name="palette">Optional palette for RGBA conversion.</param>
        public TextureBuilder(
            TextureDefinition definition,
            string[] patchNames,
            Dictionary<string, DoomPicture> patches,
            Palette? palette = null)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _patchNames = patchNames ?? throw new ArgumentNullException(nameof(patchNames));
            _patches = patches ?? throw new ArgumentNullException(nameof(patches));
            _palette = palette;
        }

        /// <summary>
        /// Creates a texture builder with direct patch name lookup.
        /// </summary>
        /// <param name="definition">The texture definition to build.</param>
        /// <param name="patchLookup">Function to look up patches by index.</param>
        /// <param name="palette">Optional palette for RGBA conversion.</param>
        public TextureBuilder(
            TextureDefinition definition,
            Func<ushort, DoomPicture?>? patchLookup,
            Palette? palette = null)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _palette = palette;

            // Build internal patch dictionary
            _patchNames = null;
            _patches = new Dictionary<string, DoomPicture>(StringComparer.OrdinalIgnoreCase);

            foreach (var patch in definition.Patches)
            {
                var picture = patchLookup?.Invoke(patch.PatchIndex);
                if (picture != null)
                {
                    _patches[patch.PatchIndex.ToString()] = picture;
                }
            }
        }

        /// <summary>
        /// Builds the texture as palette-indexed pixels.
        /// </summary>
        /// <returns>Array of palette indices (width * height).</returns>
        public byte[] BuildIndexed()
        {
            var pixels = new byte[_definition.Width * _definition.Height];
            Array.Fill(pixels, DoomPicture.TransparentIndex);

            foreach (var patchRef in _definition.Patches)
            {
                var patch = GetPatch(patchRef.PatchIndex);
                if (patch == null)
                    continue;

                ApplyPatch(pixels, patch, patchRef.OriginX, patchRef.OriginY);
            }

            return pixels;
        }

        /// <summary>
        /// Builds the texture as RGBA pixels.
        /// </summary>
        /// <returns>Array of RGBA bytes (width * height * 4).</returns>
        /// <exception cref="InvalidOperationException">If no palette was provided.</exception>
        public byte[] BuildRgba()
        {
            if (_palette == null)
                throw new InvalidOperationException("Cannot build RGBA texture without a palette");

            var indexed = BuildIndexed();
            var rgba = new byte[_definition.Width * _definition.Height * 4];

            for (int i = 0; i < indexed.Length; i++)
            {
                var colorIndex = indexed[i];
                if (colorIndex == DoomPicture.TransparentIndex)
                {
                    // Transparent pixel
                    rgba[i * 4 + 0] = 0;
                    rgba[i * 4 + 1] = 0;
                    rgba[i * 4 + 2] = 0;
                    rgba[i * 4 + 3] = 0;
                }
                else
                {
                    var color = _palette.Colors[colorIndex];
                    rgba[i * 4 + 0] = color.R;
                    rgba[i * 4 + 1] = color.G;
                    rgba[i * 4 + 2] = color.B;
                    rgba[i * 4 + 3] = 255;
                }
            }

            return rgba;
        }

        /// <summary>
        /// Gets the width of the texture being built.
        /// </summary>
        public int Width => _definition.Width;

        /// <summary>
        /// Gets the height of the texture being built.
        /// </summary>
        public int Height => _definition.Height;

        /// <summary>
        /// Gets the name of the texture being built.
        /// </summary>
        public string Name => _definition.Name;

        private DoomPicture? GetPatch(ushort patchIndex)
        {
            if (_patchNames != null)
            {
                // Look up by name from PNAMES
                if (patchIndex < _patchNames.Length)
                {
                    var patchName = _patchNames[patchIndex];
                    if (_patches.TryGetValue(patchName, out var patch))
                        return patch;
                }
            }
            else
            {
                // Look up by index string
                if (_patches.TryGetValue(patchIndex.ToString(), out var patch))
                    return patch;
            }

            return null;
        }

        private void ApplyPatch(byte[] pixels, DoomPicture patch, short originX, short originY)
        {
            for (int py = 0; py < patch.Height; py++)
            {
                int destY = originY + py;
                if (destY < 0 || destY >= _definition.Height)
                    continue;

                for (int px = 0; px < patch.Width; px++)
                {
                    int destX = originX + px;
                    if (destX < 0 || destX >= _definition.Width)
                        continue;

                    byte pixel = patch.GetPixel(px, py);
                    if (pixel != DoomPicture.TransparentIndex)
                    {
                        pixels[destY * _definition.Width + destX] = pixel;
                    }
                }
            }
        }
    }
}
