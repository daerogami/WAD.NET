namespace WAD.NET.Definitions.GameData
{
    /// <summary>
    /// Types of lighting effects for sector specials.
    /// </summary>
    public enum LightEffect
    {
        /// <summary>No lighting effect.</summary>
        None,

        /// <summary>Random blinking light.</summary>
        BlinkRandom,

        /// <summary>Blinking light at 0.5 second interval.</summary>
        BlinkHalf,

        /// <summary>Blinking light at 1 second interval.</summary>
        BlinkFull,

        /// <summary>Smoothly oscillating light.</summary>
        Oscillate,

        /// <summary>Fire-like flickering light.</summary>
        FlickerFire
    }
}
