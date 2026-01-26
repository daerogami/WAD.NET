using System;
using WAD.NET.Abstract;
using WAD.NET.Definitions.Classic.Map;
using WAD.NET.Interfaces;

namespace WAD.NET.Concrete.Maps
{
    /// <summary>
    /// Lump containing BSP node data for a DOOM format map.
    /// </summary>
    public sealed class NodesLump : Lump, IMapLump
    {
        /// <summary>
        /// The nodes in this lump.
        /// </summary>
        public Node[] Nodes { get; }

        /// <summary>
        /// Number of nodes in this lump.
        /// </summary>
        public int Count => Nodes.Length;

        /// <summary>
        /// Gets the root node (last node in the array).
        /// </summary>
        public Node? Root => Nodes.Length > 0 ? Nodes[Nodes.Length - 1] : null;

        /// <summary>
        /// Creates a new NodesLump by parsing binary data.
        /// </summary>
        /// <param name="name">Lump name.</param>
        /// <param name="source">Source WAD name.</param>
        /// <param name="data">Binary lump data.</param>
        public NodesLump(string name, string source, ReadOnlySpan<byte> data)
            : base(name, source)
        {
            int count = data.Length / Node.EntrySize;
            Nodes = new Node[count];

            for (int i = 0; i < count; i++)
            {
                Nodes[i] = Node.Parse(data.Slice(i * Node.EntrySize, Node.EntrySize));
            }
        }

        /// <summary>
        /// Gets the node at the specified index.
        /// </summary>
        public Node this[int index] => Nodes[index];
    }
}
