namespace WAD.NET.Definitions
{
    /// <summary>
    /// Represents a composite texture definition from TEXTURE1/TEXTURE2 lumps.
    /// </summary>
    public class TextureDefinition
    {
        /// <summary>
        /// Name of the texture (up to 8 characters).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Width of the texture in pixels.
        /// </summary>
        public ushort Width { get; }

        /// <summary>
        /// Height of the texture in pixels.
        /// </summary>
        public ushort Height { get; }

        /// <summary>
        /// Array of patches that compose this texture.
        /// </summary>
        public TexturePatch[] Patches { get; }

        public TextureDefinition(string name, ushort width, ushort height, TexturePatch[] patches)
        {
            Name = name;
            Width = width;
            Height = height;
            Patches = patches;
        }
    }
}
