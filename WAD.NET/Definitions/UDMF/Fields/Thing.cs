using WAD.NET;

namespace WAD.NET.UDMF.Fields
{
    /// <summary>
    /// UDMF Thing definition.
    /// </summary>
    /// <remarks>
    /// As defined by UDMF 1.1
    /// <see cref="https://github.com/coelckers/gzdoom/blob/master/specs/udmf.txt"/>
    /// </remarks>
    public struct Thing
    {
        /// <summary>
        /// Thing ID. Default = 0.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// X coordinate. No valid default.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y coordinate. No valid default.
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Z height relative to floor. Default = 0. (Relative to ceiling for SPAWNCEILING items).
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Map angle of thing in degrees. Default = 0 (East).
        /// </summary>
        public int Angle { get; set; }

        /// <summary>
        /// DoomedNum. No valid default.
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// true = in skill 1.
        /// </summary>
        public bool Skill1 { get; set; }

        /// <summary>
        /// true = in skill 2.
        /// </summary>
        public bool Skill2 { get; set; }

        /// <summary>
        /// true = in skill 3.
        /// </summary>
        public bool Skill3 { get; set; }

        /// <summary>
        /// true = in skill 4.
        /// </summary>
        public bool Skill4 { get; set; }

        /// <summary>
        /// true = in skill 5.
        /// </summary>
        public bool Skill5 { get; set; }

        /// <summary>
        /// true = thing is deaf.
        /// </summary>
        public bool Ambush { get; set; }

        /// <summary>
        /// true = in SP mode.
        /// </summary>
        public bool Single { get; set; }

        /// <summary>
        /// true = in DM mode.
        /// </summary>
        public bool Dm { get; set; }

        /// <summary>
        /// true = in Coop.
        /// </summary>
        public bool Coop { get; set; }

        /// <summary>
        /// true = MBF friend.
        /// </summary>
        /// <remarks>
        /// MBF friend flag not supported in Strife/Heretic/Hexen namespaces.
        /// </remarks>
        public bool Friend { get; set; }

        /// <summary>
        /// true = dormant thing.
        /// </summary>
        /// <remarks>
        /// Hexen flag; not supported in Doom/Strife/Heretic namespaces.
        /// </remarks>
        [HexenFlag]
        public bool Dormant { get; set; }

        /// <summary>
        /// true = Present for pclass 1.
        /// </summary>
        [HexenFlag]
        public bool Class1 { get; set; }

        /// <summary>
        /// true = Present for pclass 2.
        /// </summary>
        [HexenFlag]
        public bool Class2 { get; set; }

        /// <summary>
        /// true = Present for pclass 3.
        /// </summary>
        [HexenFlag]
        public bool Class3 { get; set; }

        /// <summary>
        /// true = Strife NPC flag.
        /// </summary>
        [StrifeFlag]
        public bool Standing { get; set; }

        /// <summary>
        /// true = Strife ally flag.
        /// </summary>
        [StrifeFlag]
        public bool StrifeAlly { get; set; }

        /// <summary>
        /// true = Strife translucency flag.
        /// </summary>
        [StrifeFlag]
        public bool Translucent { get; set; }

        /// <summary>
        /// true = Strife invisibility flag.
        /// </summary>
        [StrifeFlag]
        public bool Invisible { get; set; }

        /// <summary>
        /// Scripting special. Default = 0.
        /// </summary>
        [HexenFlag]
        public int Special { get; set; }

        /// <summary>
        /// Argument 0. Default = 0.
        /// </summary>
        [HexenFlag]
        public int Arg0 { get; set; }

        /// <summary>
        /// Argument 1. Default = 0.
        /// </summary>
        [HexenFlag]
        public int Arg1 { get; set; }

        /// <summary>
        /// Argument 2. Default = 0.
        /// </summary>
        [HexenFlag]
        public int Arg2 { get; set; }

        /// <summary>
        /// Argument 3. Default = 0.
        /// </summary>
        [HexenFlag]
        public int Arg3 { get; set; }

        /// <summary>
        /// Argument 4. Default = 0.
        /// </summary>
        [HexenFlag]
        public int Arg4 { get; set; }

        /// <summary>
        /// A comment. Implementors should attach no special semantic meaning to this field.
        /// </summary>
        public string Comment { get; set; }

        public Thing()
        {
            Id = 0;
            X = 0;
            Y = 0;
            Height = 0;
            Angle = 0;
            Type = 0;
            Skill1 = true;
            Skill2 = true;
            Skill3 = true;
            Skill4 = true;
            Skill5 = true;
            Ambush = false;
            Single = true;
            Dm = true;
            Coop = true;
            Friend = false;
            Dormant = false;
            Class1 = false;
            Class2 = false;
            Class3 = false;
            Standing = false;
            StrifeAlly = false;
            Translucent = false;
            Invisible = false;
            Special = 0;
            Arg0 = 0;
            Arg1 = 0;
            Arg2 = 0;
            Arg3 = 0;
            Arg4 = 0;
            Comment = null;
        }
    }
}
