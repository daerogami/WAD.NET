namespace WAD.NET.UDMF
{
    /// <summary>
    /// Represents a thing (entity) in a UDMF map.
    /// </summary>
    public class UdmfThing
    {
        /// <summary>
        /// Thing ID for scripting.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// X coordinate.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y coordinate.
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Height above floor (or below ceiling if SPAWNCEILING).
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Angle in degrees (0 = East).
        /// </summary>
        public int Angle { get; set; }

        /// <summary>
        /// Thing type (DoomEd number).
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Appears on skill 1.
        /// </summary>
        public bool Skill1 { get; set; } = true;

        /// <summary>
        /// Appears on skill 2.
        /// </summary>
        public bool Skill2 { get; set; } = true;

        /// <summary>
        /// Appears on skill 3.
        /// </summary>
        public bool Skill3 { get; set; } = true;

        /// <summary>
        /// Appears on skill 4.
        /// </summary>
        public bool Skill4 { get; set; } = true;

        /// <summary>
        /// Appears on skill 5.
        /// </summary>
        public bool Skill5 { get; set; } = true;

        /// <summary>
        /// Deaf/ambush flag.
        /// </summary>
        public bool Ambush { get; set; }

        /// <summary>
        /// Appears in single player.
        /// </summary>
        public bool Single { get; set; } = true;

        /// <summary>
        /// Appears in deathmatch.
        /// </summary>
        public bool Dm { get; set; } = true;

        /// <summary>
        /// Appears in coop.
        /// </summary>
        public bool Coop { get; set; } = true;

        /// <summary>
        /// Friendly monster (MBF).
        /// </summary>
        public bool Friend { get; set; }

        /// <summary>
        /// Dormant (Hexen).
        /// </summary>
        public bool Dormant { get; set; }

        /// <summary>
        /// Appears for player class 1 (Hexen).
        /// </summary>
        public bool Class1 { get; set; } = true;

        /// <summary>
        /// Appears for player class 2 (Hexen).
        /// </summary>
        public bool Class2 { get; set; } = true;

        /// <summary>
        /// Appears for player class 3 (Hexen).
        /// </summary>
        public bool Class3 { get; set; } = true;

        /// <summary>
        /// Special action type.
        /// </summary>
        public int Special { get; set; }

        /// <summary>
        /// Argument 0.
        /// </summary>
        public int Arg0 { get; set; }

        /// <summary>
        /// Argument 1.
        /// </summary>
        public int Arg1 { get; set; }

        /// <summary>
        /// Argument 2.
        /// </summary>
        public int Arg2 { get; set; }

        /// <summary>
        /// Argument 3.
        /// </summary>
        public int Arg3 { get; set; }

        /// <summary>
        /// Argument 4.
        /// </summary>
        public int Arg4 { get; set; }

        /// <summary>
        /// Comment (not used by engine).
        /// </summary>
        public string? Comment { get; set; }

        // ZDoom extensions
        /// <summary>
        /// Gravity factor (ZDoom extension).
        /// </summary>
        public double Gravity { get; set; } = 1.0;

        /// <summary>
        /// Health factor (ZDoom extension).
        /// </summary>
        public double Health { get; set; } = 1.0;

        /// <summary>
        /// Scale factor (ZDoom extension).
        /// </summary>
        public double ScaleX { get; set; } = 1.0;

        /// <summary>
        /// Scale factor (ZDoom extension).
        /// </summary>
        public double ScaleY { get; set; } = 1.0;

        /// <summary>
        /// Render style (ZDoom extension).
        /// </summary>
        public string? RenderStyle { get; set; }

        /// <summary>
        /// Alpha/translucency (ZDoom extension).
        /// </summary>
        public double Alpha { get; set; } = 1.0;

        /// <summary>
        /// Fill color (ZDoom extension).
        /// </summary>
        public int FillColor { get; set; }
    }
}
