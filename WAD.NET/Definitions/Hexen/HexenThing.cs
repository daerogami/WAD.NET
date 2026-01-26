using System;
using System.Buffers.Binary;

namespace WAD.NET.Definitions.Hexen
{
    /// <summary>
    /// Represents a thing (entity) in a Hexen format map.
    /// </summary>
    /// <remarks>
    /// Binary format: 20 bytes per thing.
    /// Extended from DOOM format with TID, Z position, and script args.
    /// </remarks>
    public readonly struct HexenThing
    {
        /// <summary>
        /// Size of a Hexen thing entry in bytes.
        /// </summary>
        public const int EntrySize = 20;

        /// <summary>
        /// Thing ID for scripting (TID).
        /// </summary>
        public ushort TID { get; init; }

        /// <summary>
        /// X coordinate in map units.
        /// </summary>
        public short X { get; init; }

        /// <summary>
        /// Y coordinate in map units.
        /// </summary>
        public short Y { get; init; }

        /// <summary>
        /// Z position relative to floor (or ceiling if spawning on ceiling).
        /// </summary>
        public short Z { get; init; }

        /// <summary>
        /// Angle in degrees (0-359). 0 = East, 90 = North.
        /// </summary>
        public ushort Angle { get; init; }

        /// <summary>
        /// Thing type (DoomEd number).
        /// </summary>
        public ushort Type { get; init; }

        /// <summary>
        /// Spawn flags.
        /// </summary>
        public HexenThingFlags Flags { get; init; }

        /// <summary>
        /// Special action to execute.
        /// </summary>
        public byte Special { get; init; }

        /// <summary>
        /// Argument 0 for the special.
        /// </summary>
        public byte Arg0 { get; init; }

        /// <summary>
        /// Argument 1 for the special.
        /// </summary>
        public byte Arg1 { get; init; }

        /// <summary>
        /// Argument 2 for the special.
        /// </summary>
        public byte Arg2 { get; init; }

        /// <summary>
        /// Argument 3 for the special.
        /// </summary>
        public byte Arg3 { get; init; }

        /// <summary>
        /// Argument 4 for the special.
        /// </summary>
        public byte Arg4 { get; init; }

        /// <summary>
        /// Gets all arguments as an array.
        /// </summary>
        public byte[] Args => new[] { Arg0, Arg1, Arg2, Arg3, Arg4 };

        /// <summary>
        /// True if this thing appears on skill 1 (easy).
        /// </summary>
        public bool AppearsOnSkill1 => (Flags & HexenThingFlags.SkillEasy) != 0;

        /// <summary>
        /// True if this thing appears on skill 2 (easy).
        /// </summary>
        public bool AppearsOnSkill2 => (Flags & HexenThingFlags.SkillEasy) != 0;

        /// <summary>
        /// True if this thing appears on skill 3 (medium).
        /// </summary>
        public bool AppearsOnSkill3 => (Flags & HexenThingFlags.SkillMedium) != 0;

        /// <summary>
        /// True if this thing appears on skill 4 (hard).
        /// </summary>
        public bool AppearsOnSkill4 => (Flags & HexenThingFlags.SkillHard) != 0;

        /// <summary>
        /// True if this thing appears on skill 5 (hard).
        /// </summary>
        public bool AppearsOnSkill5 => (Flags & HexenThingFlags.SkillHard) != 0;

        /// <summary>
        /// True if this thing is dormant (inactive until triggered).
        /// </summary>
        public bool IsDormant => (Flags & HexenThingFlags.Dormant) != 0;

        /// <summary>
        /// Parses a HexenThing from binary data.
        /// </summary>
        /// <param name="data">The binary data (must be at least 20 bytes).</param>
        /// <returns>A parsed HexenThing.</returns>
        public static HexenThing Parse(ReadOnlySpan<byte> data)
        {
            if (data.Length < EntrySize)
                throw new ArgumentException($"Data must be at least {EntrySize} bytes", nameof(data));

            return new HexenThing
            {
                TID = BinaryPrimitives.ReadUInt16LittleEndian(data),
                X = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(2)),
                Y = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(4)),
                Z = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(6)),
                Angle = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8)),
                Type = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(10)),
                Flags = (HexenThingFlags)BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(12)),
                Special = data[14],
                Arg0 = data[15],
                Arg1 = data[16],
                Arg2 = data[17],
                Arg3 = data[18],
                Arg4 = data[19]
            };
        }
    }
}
