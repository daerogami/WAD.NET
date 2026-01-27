using WAD.NET;

namespace WAD.NET.UDMF.Fields
{
    /// <summary>
    /// UDMF Line Definition.
    /// </summary>
    /// <remarks>
    /// As defined by UDMF 1.1
    /// <see cref="https://github.com/coelckers/gzdoom/blob/master/specs/udmf.txt"/>
    /// </remarks>
    public struct LineDefinition
    {
        /// <summary>
        /// ID of line. Interpreted as tag or scripting id. Default = -1.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Index of first vertex. No valid default.
        /// </summary>
        public int V1 { get; set; }

        /// <summary>
        /// Index of second vertex. No valid default.
        /// </summary>
        public int V2 { get; set; }

        /// <summary>
        /// true = line blocks things.
        /// </summary>
        public bool Blocking { get; set; }

        /// <summary>
        /// true = line blocks monsters.
        /// </summary>
        public bool BlockMonsters { get; set; }

        /// <summary>
        /// true = line is 2S.
        /// </summary>
        public bool TwoSided { get; set; }

        /// <summary>
        /// true = upper texture unpegged.
        /// </summary>
        public bool DontPegTop { get; set; }

        /// <summary>
        /// true = lower texture unpegged.
        /// </summary>
        public bool DontPegBottom { get; set; }

        /// <summary>
        /// true = drawn as 1S on map.
        /// </summary>
        public bool Secret { get; set; }

        /// <summary>
        /// true = blocks sound.
        /// </summary>
        public bool BlockSound { get; set; }

        /// <summary>
        /// true = line never drawn on map.
        /// </summary>
        public bool DontDraw { get; set; }

        /// <summary>
        /// true = always appears on map.
        /// </summary>
        public bool Mapped { get; set; }

        /// <summary>
        /// true = passes use action.
        /// </summary>
        [BoomFlag]
        public bool PassUse { get; set; }

        /// <summary>
        /// true = line is a Strife translucent line.
        /// </summary>
        [StrifeFlag]
        public bool Translucent { get; set; }

        /// <summary>
        /// true = line is a Strife railing.
        /// </summary>
        [StrifeFlag]
        public bool JumpOver { get; set; }

        /// <summary>
        /// true = line is a Strife float-blocker.
        /// </summary>
        [StrifeFlag]
        public bool BlockFloaters { get; set; }

        /// <summary>
        /// true = player can cross.
        /// </summary>
        [SPACFlag]
        public bool PlayerCross { get; set; }

        /// <summary>
        /// true = player can use.
        /// </summary>
        [SPACFlag]
        public bool PlayerUse { get; set; }

        /// <summary>
        /// true = monster can cross.
        /// </summary>
        [SPACFlag]
        public bool MonsterCross { get; set; }

        /// <summary>
        /// true = monster can use.
        /// </summary>
        [SPACFlag]
        public bool MonsterUse { get; set; }

        /// <summary>
        /// true = projectile can activate.
        /// </summary>
        [SPACFlag]
        public bool Impact { get; set; }

        /// <summary>
        /// true = player can push.
        /// </summary>
        [SPACFlag]
        public bool PlayerPush { get; set; }

        /// <summary>
        /// true = monster can push.
        /// </summary>
        [SPACFlag]
        public bool MonsterPush { get; set; }

        /// <summary>
        /// true = projectile can cross.
        /// </summary>
        [SPACFlag]
        public bool MissileCross { get; set; }

        /// <summary>
        /// true = repeatable special.
        /// </summary>
        [SPACFlag]
        public bool RepeatSpecial { get; set; }

        /// <summary>
        /// Special. Default = 0.
        /// </summary>
        public int Special { get; set; }

        /// <summary>
        /// Argument 0. Default = 0.
        /// </summary>
        public int Arg0 { get; set; }

        /// <summary>
        /// Argument 1. Default = 0.
        /// </summary>
        public int Arg1 { get; set; }

        /// <summary>
        /// Argument 2. Default = 0.
        /// </summary>
        public int Arg2 { get; set; }

        /// <summary>
        /// Argument 3. Default = 0.
        /// </summary>
        public int Arg3 { get; set; }

        /// <summary>
        /// Argument 4. Default = 0.
        /// </summary>
        public int Arg4 { get; set; }

        /// <summary>
        /// Sidedef 1 index. No valid default.
        /// </summary>
        public int SideFront { get; set; }

        /// <summary>
        /// Sidedef 2 index. Default = -1.
        /// </summary>
        public int SideBack { get; set; }

        /// <summary>
        /// A comment.
        /// </summary>
        public string? Comment { get; set; }

        public LineDefinition()
        {
            Id = -1;
            V1 = 0;
            V2 = 0;
            Blocking = false;
            BlockMonsters = false;
            TwoSided = false;
            DontPegTop = false;
            DontPegBottom = false;
            Secret = false;
            BlockSound = false;
            DontDraw = false;
            Mapped = false;
            PassUse = false;
            Translucent = false;
            JumpOver = false;
            BlockFloaters = false;
            PlayerCross = false;
            PlayerUse = false;
            MonsterCross = false;
            MonsterUse = false;
            Impact = false;
            PlayerPush = false;
            MonsterPush = false;
            MissileCross = false;
            RepeatSpecial = false;
            Special = 0;
            Arg0 = 0;
            Arg1 = 0;
            Arg2 = 0;
            Arg3 = 0;
            Arg4 = 0;
            SideFront = 0;
            SideBack = -1;
            Comment = null;
        }
    }
}
