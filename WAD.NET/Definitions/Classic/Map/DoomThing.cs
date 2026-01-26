using System;
using System.Buffers.Binary;

namespace WAD.NET.Definitions.Classic.Map
{
    /// <summary>
    /// Represents a thing (entity) in a DOOM format map.
    /// </summary>
    /// <remarks>
    /// Binary format: 10 bytes per thing.
    /// </remarks>
    public readonly struct DoomThing
    {
        /// <summary>
        /// Size of a thing entry in bytes.
        /// </summary>
        public const int EntrySize = 10;

        /// <summary>
        /// X coordinate in map units.
        /// </summary>
        public short X { get; init; }

        /// <summary>
        /// Y coordinate in map units.
        /// </summary>
        public short Y { get; init; }

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
        public ThingFlags Flags { get; init; }

        /// <summary>
        /// True if this thing appears on skill 1 (I'm Too Young To Die).
        /// </summary>
        public bool AppearsOnSkill1 => (Flags & ThingFlags.SkillEasy) != 0;

        /// <summary>
        /// True if this thing appears on skill 2 (Hey, Not Too Rough).
        /// </summary>
        public bool AppearsOnSkill2 => (Flags & ThingFlags.SkillEasy) != 0;

        /// <summary>
        /// True if this thing appears on skill 3 (Hurt Me Plenty).
        /// </summary>
        public bool AppearsOnSkill3 => (Flags & ThingFlags.SkillMedium) != 0;

        /// <summary>
        /// True if this thing appears on skill 4 (Ultra-Violence).
        /// </summary>
        public bool AppearsOnSkill4 => (Flags & ThingFlags.SkillHard) != 0;

        /// <summary>
        /// True if this thing appears on skill 5 (Nightmare!).
        /// </summary>
        public bool AppearsOnSkill5 => (Flags & ThingFlags.SkillHard) != 0;

        /// <summary>
        /// True if this thing is deaf (won't react to sound).
        /// </summary>
        public bool IsDeaf => (Flags & ThingFlags.Ambush) != 0;

        /// <summary>
        /// True if this thing only appears in multiplayer.
        /// </summary>
        public bool IsMultiplayerOnly => (Flags & ThingFlags.Multiplayer) != 0;

        /// <summary>
        /// Parses a DoomThing from binary data.
        /// </summary>
        /// <param name="data">The binary data (must be at least 10 bytes).</param>
        /// <returns>A parsed DoomThing.</returns>
        public static DoomThing Parse(ReadOnlySpan<byte> data)
        {
            if (data.Length < EntrySize)
                throw new ArgumentException($"Data must be at least {EntrySize} bytes", nameof(data));

            return new DoomThing
            {
                X = BinaryPrimitives.ReadInt16LittleEndian(data),
                Y = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(2)),
                Angle = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(4)),
                Type = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(6)),
                Flags = (ThingFlags)BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8))
            };
        }
    }
}
