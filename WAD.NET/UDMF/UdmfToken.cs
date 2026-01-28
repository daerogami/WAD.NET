namespace WAD.NET.UDMF
{
    /// <summary>
    /// Represents a token in UDMF text.
    /// </summary>
    public readonly struct UdmfToken
    {
        /// <summary>
        /// The type of this token.
        /// </summary>
        public UdmfTokenType Type { get; init; }

        /// <summary>
        /// The text value of this token.
        /// </summary>
        public string Value { get; init; }

        /// <summary>
        /// The line number where this token appears (1-based).
        /// </summary>
        public int Line { get; init; }

        /// <summary>
        /// The column number where this token starts (1-based).
        /// </summary>
        public int Column { get; init; }

        /// <summary>
        /// Creates a new token.
        /// </summary>
        public UdmfToken(UdmfTokenType type, string value, int line, int column)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
        }

        public override string ToString() => $"{Type}: '{Value}' at {Line}:{Column}";
    }
}
