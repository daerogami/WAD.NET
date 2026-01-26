using System;
using System.Text;
using WAD.NET.Abstract;
using WAD.NET.Interfaces;
using WAD.NET.UDMF;

namespace WAD.NET.Concrete.Maps
{
    /// <summary>
    /// Lump containing UDMF text map data.
    /// </summary>
    public sealed class TextMapLump : Lump, IMapLump
    {
        /// <summary>
        /// The raw UDMF text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The parsed UDMF map (lazy loaded).
        /// </summary>
        public UdmfMap Map => _map ??= ParseMap();
        private UdmfMap _map;

        /// <summary>
        /// Creates a new TextMapLump.
        /// </summary>
        /// <param name="name">Lump name.</param>
        /// <param name="source">Source WAD name.</param>
        /// <param name="data">Binary lump data.</param>
        public TextMapLump(string name, string source, ReadOnlySpan<byte> data)
            : base(name, source)
        {
#if NETSTANDARD2_1_OR_GREATER
            Text = Encoding.UTF8.GetString(data);
#else
            Text = Encoding.UTF8.GetString(data.ToArray());
#endif
        }

        /// <summary>
        /// Creates a new TextMapLump from text.
        /// </summary>
        /// <param name="name">Lump name.</param>
        /// <param name="source">Source WAD name.</param>
        /// <param name="text">UDMF text.</param>
        public TextMapLump(string name, string source, string text)
            : base(name, source)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        private UdmfMap ParseMap()
        {
            var parser = new UdmfParser();
            return parser.Parse(Text);
        }
    }
}
