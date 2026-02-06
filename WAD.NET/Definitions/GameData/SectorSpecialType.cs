namespace WAD.NET.Definitions.GameData
{
    /// <summary>
    /// Type classification for sector specials.
    /// </summary>
    public enum SectorSpecialType
    {
        /// <summary>No special effect.</summary>
        None,

        /// <summary>Lighting effect.</summary>
        Light,

        /// <summary>Damage-dealing sector.</summary>
        Damage,

        /// <summary>Secret area.</summary>
        Secret,

        /// <summary>Door behavior.</summary>
        Door,

        /// <summary>Level exit trigger.</summary>
        End,

        /// <summary>Other special behavior.</summary>
        Special
    }
}
