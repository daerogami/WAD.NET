namespace WAD.NET.UDMF
{
    /// <summary>
    /// Represents a vertex in a UDMF map.
    /// </summary>
    public class UdmfVertex
    {
        /// <summary>
        /// X coordinate.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y coordinate.
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Floor height at this vertex (ZDoom extension).
        /// </summary>
        public double? ZFloor { get; set; }

        /// <summary>
        /// Ceiling height at this vertex (ZDoom extension).
        /// </summary>
        public double? ZCeiling { get; set; }
    }
}
