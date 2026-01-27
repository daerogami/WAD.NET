namespace WAD.NET.UDMF
{
    /// <summary>
    /// Represents a linedef in a UDMF map.
    /// </summary>
    public class UdmfLinedef
    {
        /// <summary>
        /// Linedef ID for scripting.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Start vertex index.
        /// </summary>
        public int V1 { get; set; }

        /// <summary>
        /// End vertex index.
        /// </summary>
        public int V2 { get; set; }

        /// <summary>
        /// Blocks players and monsters.
        /// </summary>
        public bool Blocking { get; set; }

        /// <summary>
        /// Blocks monsters only.
        /// </summary>
        public bool BlockMonsters { get; set; }

        /// <summary>
        /// Line has two sides.
        /// </summary>
        public bool TwoSided { get; set; }

        /// <summary>
        /// Upper texture unpegged.
        /// </summary>
        public bool DontPegTop { get; set; }

        /// <summary>
        /// Lower texture unpegged.
        /// </summary>
        public bool DontPegBottom { get; set; }

        /// <summary>
        /// Secret (shown as 1-sided on automap).
        /// </summary>
        public bool Secret { get; set; }

        /// <summary>
        /// Blocks sound propagation.
        /// </summary>
        public bool BlockSound { get; set; }

        /// <summary>
        /// Not shown on automap.
        /// </summary>
        public bool DontDraw { get; set; }

        /// <summary>
        /// Already on automap.
        /// </summary>
        public bool Mapped { get; set; }

        /// <summary>
        /// Passthrough (Boom/Strife).
        /// </summary>
        public bool PassUse { get; set; }

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
        /// Front sidedef index (-1 if none).
        /// </summary>
        public int SideFront { get; set; } = -1;

        /// <summary>
        /// Back sidedef index (-1 if none).
        /// </summary>
        public int SideBack { get; set; } = -1;

        /// <summary>
        /// Comment (not used by engine).
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Line is repeatable.
        /// </summary>
        public bool Repeatspecial { get; set; }

        /// <summary>
        /// Activation type (Hexen/ZDoom).
        /// </summary>
        public int Activation { get; set; }

        /// <summary>
        /// Blocks players.
        /// </summary>
        public bool Blockplayers { get; set; }

        /// <summary>
        /// Blocks everything.
        /// </summary>
        public bool Blockeverything { get; set; }
    }
}
