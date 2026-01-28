namespace WAD.NET.UDMF.Fields
{
    /// <summary>
    /// UDMF Vertex definition.
    /// </summary>
    /// <remarks>
    /// As defined by UDMF 1.1
    /// <see cref="https://github.com/coelckers/gzdoom/blob/master/specs/udmf.txt"/>
    /// </remarks>
    public struct Vertex
    {
        /// <summary>
        /// X coordinate. No valid default.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y coordinate. No valid default.
        /// </summary>
        public double Y { get; set; }

        public Vertex(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
