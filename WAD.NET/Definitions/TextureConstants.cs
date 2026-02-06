namespace WAD.NET.Definitions
{
    /// <summary>
    /// Texture name constants.
    /// </summary>
    public static class TextureConstants
    {
        /// <summary>Null texture marker - no texture on this surface.</summary>
        public const string NullTexture = "-";

        /// <summary>
        /// Checks if a texture name represents no texture.
        /// </summary>
        /// <param name="textureName">The texture name to check.</param>
        /// <returns>True if the texture name is null, empty, or the null texture marker.</returns>
        public static bool IsNull(string textureName)
        {
            return string.IsNullOrEmpty(textureName)
                || textureName == NullTexture;
        }
    }
}
