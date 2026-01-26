namespace WAD.NET.Definitions
{
    /// <summary>
    /// Represents a patch reference within a composite texture definition.
    /// </summary>
    public readonly struct TexturePatch
    {
        /// <summary>
        /// X offset of the patch within the texture.
        /// </summary>
        public short OriginX { get; }

        /// <summary>
        /// Y offset of the patch within the texture.
        /// </summary>
        public short OriginY { get; }

        /// <summary>
        /// Index into the PNAMES lump for the patch name.
        /// </summary>
        public ushort PatchIndex { get; }

        public TexturePatch(short originX, short originY, ushort patchIndex)
        {
            OriginX = originX;
            OriginY = originY;
            PatchIndex = patchIndex;
        }
    }
}
