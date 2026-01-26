namespace WAD.NET.Detection
{
    /// <summary>
    /// Known DOOM source ports and their compatibility levels.
    /// </summary>
    public enum SourcePort
    {
        /// <summary>
        /// Original DOOM engine (vanilla compatibility).
        /// </summary>
        Vanilla = 0,

        /// <summary>
        /// Boom-compatible source ports (PRBoom, GLBoom, etc.).
        /// </summary>
        Boom = 1,

        /// <summary>
        /// MBF (Marine's Best Friend) compatible ports.
        /// </summary>
        MBF = 2,

        /// <summary>
        /// MBF21 standard compliant ports.
        /// </summary>
        MBF21 = 3,

        /// <summary>
        /// ZDoom family (ZDoom, LZDoom, older GZDoom).
        /// </summary>
        ZDoom = 4,

        /// <summary>
        /// GZDoom (OpenGL/Vulkan advanced features).
        /// </summary>
        GZDoom = 5,

        /// <summary>
        /// Eternity Engine.
        /// </summary>
        Eternity = 6,

        /// <summary>
        /// EDGE source port.
        /// </summary>
        Edge = 7,

        /// <summary>
        /// 3DGE (enhanced EDGE).
        /// </summary>
        Edge3D = 8,

        /// <summary>
        /// Doomsday Engine.
        /// </summary>
        Doomsday = 9,

        /// <summary>
        /// Unknown or undetectable.
        /// </summary>
        Unknown = 99
    }
}
