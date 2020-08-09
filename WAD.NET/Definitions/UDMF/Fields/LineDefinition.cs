namespace WAD.NET.UDMF.Fields
{
    /// <summary>
    /// Line Definition
    /// </summary>
    /// <remarks>
    /// As defined by UDMF 1.1
    /// <see cref="https://github.com/coelckers/gzdoom/blob/master/specs/udmf.txt"/>
    /// </remarks>
    struct LineDefinition
    {
        /// <summary>
        /// ID of line. Interpreted as tag or scripting id.
        /// Default = -1. *** see below.
        /// </summary>
        int id;

        /// <summary>
        /// Index of first vertex. No valid default.
        /// </summary>
        int v1;

        /// <summary>
        /// Index of second vertex. No valid default.
        /// </summary>
        int v2;

        /// <summary>
        /// true = line blocks things.
        /// </summary>
        bool blocking;

        /// <summary>
        /// true = line blocks monsters.
        /// </summary>
        bool blockmonsters;

        /// <summary>
        /// true = line is 2S.
        /// </summary>
        bool twosided;

        /// <summary>
        /// true = upper texture unpegged.
        /// </summary>
        bool dontpegtop;

        /// <summary>
        /// true = lower texture unpegged.
        /// </summary>
        bool dontpegbottom;

        /// <summary>
        /// true = drawn as 1S on map.
        /// </summary>
        bool secret;

        /// <summary>
        /// true = blocks sound.
        /// </summary>
        bool blocksound;

        /// <summary>
        /// true = line never drawn on map.
        /// </summary>
        bool dontdraw;

        /// <summary>
        /// true = always appears on map.
        /// </summary>
        bool mapped;

        /// <summary>
        /// true = passes use action.
        /// </summary>
        /// <remarks>
        /// BOOM passuse flag not supported in Strife/Heretic/Hexen namespaces.
        /// </remarks>
        [BoomFlag]
        bool passuse;

        /// <summary>
        /// true = line is a Strife translucent line.
        /// </summary>
        /// <remarks>
        /// Strife specific flag. Support for other games is not defined by default and this flag should be ignored when reading maps not for the Strife namespace or maps for a port which supports this flag.
        /// </remarks>
        [StrifeFlag]
        bool translucent;

        /// <summary>
        /// true = line is a Strife railing.
        /// </summary>
        /// <remarks>
        /// Strife specific flag. Support for other games is not defined by default and this flag should be ignored when reading maps not for the Strife namespace or maps for a port which supports this flag.
        /// </remarks>
        [StrifeFlag]
        bool jumpover;

        /// <summary>
        /// true = line is a Strife float-blocker.
        /// </summary>
        /// <remarks>
        /// Strife specific flag. Support for other games is not defined by default and this flag should be ignored when reading maps not for the Strife namespace or maps for a port which supports this flag.
        /// </remarks>
        [StrifeFlag]
        bool blockfloaters;


        /// <summary>
        /// true = player can cross.
        /// </summary>
        /// <remarks>SPAC flags should be set false in Doom/Heretic/Strife namespace maps. Specials in those games do not support this mechanism and instead imply activation parameters through the special number. All flags default to false.</remarks>
        [SPACFlag]
        bool playercross;

        /// <summary>
        /// true = player can use.
        /// </summary>
        /// <remarks>SPAC flags should be set false in Doom/Heretic/Strife namespace maps. Specials in those games do not support this mechanism and instead imply activation parameters through the special number. All flags default to false.</remarks>
        [SPACFlag]
        bool playeruse;

        /// <summary>
        /// true = monster can cross.
        /// </summary>
        /// <remarks>SPAC flags should be set false in Doom/Heretic/Strife namespace maps. Specials in those games do not support this mechanism and instead imply activation parameters through the special number. All flags default to false.</remarks>
        [SPACFlag]
        bool monstercross;

        /// <summary>
        /// true = monster can use.
        /// </summary>
        /// <remarks>SPAC flags should be set false in Doom/Heretic/Strife namespace maps. Specials in those games do not support this mechanism and instead imply activation parameters through the special number. All flags default to false.</remarks>
        [SPACFlag]
        bool monsteruse;

        /// <summary>
        /// true = projectile can activate.
        /// </summary>
        /// <remarks>SPAC flags should be set false in Doom/Heretic/Strife namespace maps. Specials in those games do not support this mechanism and instead imply activation parameters through the special number. All flags default to false.</remarks>
        [SPACFlag]
        bool impact;

        /// <summary>
        /// true = player can push.
        /// </summary>
        /// <remarks>SPAC flags should be set false in Doom/Heretic/Strife namespace maps. Specials in those games do not support this mechanism and instead imply activation parameters through the special number. All flags default to false.</remarks>
        [SPACFlag]
        bool playerpush;

        /// <summary>
        /// true = monster can push.
        /// </summary>
        /// <remarks>SPAC flags should be set false in Doom/Heretic/Strife namespace maps. Specials in those games do not support this mechanism and instead imply activation parameters through the special number. All flags default to false.</remarks>
        [SPACFlag]
        bool monsterpush;

        /// <summary>
        /// true = projectile can cross.
        /// </summary>
        /// <remarks>SPAC flags should be set false in Doom/Heretic/Strife namespace maps. Specials in those games do not support this mechanism and instead imply activation parameters through the special number. All flags default to false.</remarks>
        [SPACFlag]
        bool missilecross;

        /// <summary>
        /// true = repeatable special.
        /// </summary>
        /// <remarks>SPAC flags should be set false in Doom/Heretic/Strife namespace maps. Specials in those games do not support this mechanism and instead imply activation parameters through the special number. All flags default to false.</remarks>
        [SPACFlag]
        bool repeatspecial;

        /// <summary>
        /// Special. Default = 0.
        /// </summary>
        int special;

        /// <summary>
        /// Argument 0. Default = 0.
        /// </summary>
        int arg0;

        /// <summary>
        /// Argument 1. Default = 0.
        /// </summary>
        int arg1;

        /// <summary>
        /// Argument 2. Default = 0.
        /// </summary>
        int arg2;

        /// <summary>
        /// Argument 3. Default = 0.
        /// </summary>
        int arg3;

        /// <summary>
        /// Argument 4. Default = 0.
        /// </summary>
        int arg4;

        /// <summary>
        /// Sidedef 1 index. No valid default.
        /// </summary>
        int sidefront;

        /// <summary>
        /// Sidedef 2 index. Default = -1.
        /// </summary>
        int sideback;

        /// <summary>
        /// A comment. Implementors should attach no special semantic meaning to this field.
        /// </summary>
        string comment;
    }
}
