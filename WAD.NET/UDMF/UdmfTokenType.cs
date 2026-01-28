namespace WAD.NET.UDMF
{
    /// <summary>
    /// Token types for UDMF lexer.
    /// </summary>
    public enum UdmfTokenType
    {
        /// <summary>
        /// An identifier (block type or property name).
        /// </summary>
        Identifier,

        /// <summary>
        /// An integer value.
        /// </summary>
        Integer,

        /// <summary>
        /// A floating-point value.
        /// </summary>
        Float,

        /// <summary>
        /// A quoted string value.
        /// </summary>
        String,

        /// <summary>
        /// A boolean keyword (true/false).
        /// </summary>
        Boolean,

        /// <summary>
        /// The equals sign (=).
        /// </summary>
        Equals,

        /// <summary>
        /// The semicolon (;).
        /// </summary>
        Semicolon,

        /// <summary>
        /// The opening brace ({).
        /// </summary>
        OpenBrace,

        /// <summary>
        /// The closing brace (}).
        /// </summary>
        CloseBrace,

        /// <summary>
        /// End of file/input.
        /// </summary>
        EndOfFile
    }
}
