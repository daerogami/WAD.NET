namespace WAD.NET.UDMF.Fields
{
    /// <summary>
    /// Thing
    /// </summary>
    /// <remarks>
    /// As defined by UDMF 1.1
    /// <see cref="https://github.com/coelckers/gzdoom/blob/master/specs/udmf.txt"/>
    /// </remarks>
    struct Thing
    {
        /// <summary>
        /// Thing ID. Default = 0.
        /// </summary>
        int id;

        /// <summary>
        /// X coordinate. No valid default.
        /// </summary>
        int x;

        /// <summary>
        /// Y coordinate. No valid default.
        /// </summary>
        int y;

        /// <summary>
        /// Z height relative to floor. Default = 0. (Relative to ceiling for SPAWNCEILING items).
        /// </summary>
        double height;

        /// <summary>
        /// Map angle of thing in degrees. Default = 0 (East).
        /// </summary>
        int angle;

        /// <summary>
        /// DoomedNum. No valid default.
        /// </summary>
        int type;

        /// <summary>
        /// true = in skill 1.
        /// </summary>
        bool skill1;

        /// <summary>
        /// true = in skill 2.
        /// </summary>
        bool skill2;

        /// <summary>
        /// true = in skill 3.
        /// </summary>
        bool skill3;

        /// <summary>
        /// true = in skill 4.
        /// </summary>
        bool skill4;

        /// <summary>
        /// true = in skill 5.
        /// </summary>
        bool skill5;

        /// <summary>
        /// true = thing is deaf.
        /// </summary>
        bool ambush;

        /// <summary>
        /// true = in SP mode.
        /// </summary>
        bool single;

        /// <summary>
        /// true = in DM mode.
        /// </summary>
        bool dm;

        /// <summary>
        /// true = in Coop.
        /// </summary>
        bool coop;

        /// <summary>
        /// true = MBF friend.
        /// </summary>
        /// <remarks>
        /// MBF friend flag not supported in Strife/Heretic/Hexen namespaces.
        /// </remarks>
        bool friend;

        /// <summary>
        /// true = dormant thing.
        /// </summary>
        /// <remarks>
        /// Hexen flag; not supported in Doom/Strife/Heretic namespaces.
        /// </remarks>
        [HexenFlag]
        bool dormant;

        /// <summary>
        /// true = Present for pclass 1.
        /// </summary>
        /// <remarks>
        /// Hexen flag; not supported in Doom/Strife/Heretic namespaces.
        /// </remarks>
        [HexenFlag]
        bool class1;

        /// <summary>
        /// true = Present for pclass 2.
        /// </summary>
        /// <remarks>
        /// Hexen flag; not supported in Doom/Strife/Heretic namespaces.
        /// </remarks>
        [HexenFlag]
        bool class2;

        /// <summary>
        /// true = Present for pclass 3.
        /// </summary>
        /// <remarks>
        /// Hexen flag; not supported in Doom/Strife/Heretic namespaces.
        /// </remarks>
        [HexenFlag]
        bool class3;

        /// <summary>
        /// true = Strife NPC flag.
        /// </summary>
        /// <remarks>
        /// Strife specific flags. Support for other games is not defined by default and these flags should be ignored when reading maps not for the Strife namespace or maps for a port which supports these flags.
        /// </remarks>
        [StrifeFlag]
        bool standing;

        /// <summary>
        /// true = Strife ally flag.
        /// </summary>
        /// <remarks>
        /// Strife specific flags. Support for other games is not defined by default and these flags should be ignored when reading maps not for the Strife namespace or maps for a port which supports these flags.
        /// </remarks>
        [StrifeFlag]
        bool strifeally;

        /// <summary>
        /// true = Strife translucency flag.
        /// </summary>
        /// <remarks>
        /// Strife specific flags. Support for other games is not defined by default and these flags should be ignored when reading maps not for the Strife namespace or maps for a port which supports these flags.
        /// </remarks>
        [StrifeFlag]
        bool translucent;

        /// <summary>
        /// true = Strife invisibility flag.
        /// </summary>
        /// <remarks>
        /// Strife specific flags. Support for other games is not defined by default and these flags should be ignored when reading maps not for the Strife namespace or maps for a port which supports these flags.
        /// </remarks>
        [StrifeFlag]
        bool invisible;


        // Note: suggested editor defaults for all skill, gamemode, and player
        // class flags is true rather than the UDMF default of false.

        /// <summary>
        /// Scripting special. Default = 0;
        /// </summary>
        /// <remarks>
        /// Thing special semantics are only defined for the Hexen namespace or ports which implement this feature in their own namespace.
        /// </remarks>
        [HexenFlag]
        int special;

        /// <summary>
        /// Argument 0. Default = 0.
        /// </summary>
        /// <remarks>
        /// Thing special semantics are only defined for the Hexen namespace or ports which implement this feature in their own namespace.
        /// </remarks>
        [HexenFlag]
        int arg0;

        /// <summary>
        /// Argument 1. Default = 0.
        /// </summary>
        /// <remarks>
        /// Thing special semantics are only defined for the Hexen namespace or ports which implement this feature in their own namespace.
        /// </remarks>
        [HexenFlag]
        int arg1;

        /// <summary>
        /// Argument 2. Default = 0.
        /// </summary>
        /// <remarks>
        /// Thing special semantics are only defined for the Hexen namespace or ports which implement this feature in their own namespace.
        /// </remarks>
        [HexenFlag]
        int arg2;

        /// <summary>
        /// Argument 3. Default = 0.
        /// </summary>
        /// <remarks>
        /// Thing special semantics are only defined for the Hexen namespace or ports which implement this feature in their own namespace.
        /// </remarks>
        [HexenFlag]
        int arg3;

        /// <summary>
        /// Argument 4. Default = 0.
        /// </summary>
        /// <remarks>
        /// Thing special semantics are only defined for the Hexen namespace or ports which implement this feature in their own namespace.
        /// </remarks>
        [HexenFlag]
        int arg4;

        /// <summary>
        /// A comment. Implementors should attach no special semantic meaning to this field.
        /// </summary>
        string comment;
    }
}
