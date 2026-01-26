namespace WAD.NET.Definitions
{
    /// <summary>
    /// Represents a single sprite frame with rotation information.
    /// </summary>
    /// <remarks>
    /// DOOM sprite naming convention: XXXXYZ[YZ]
    /// - XXXX = 4-letter sprite name (e.g., "POSS" for zombie)
    /// - Y = Frame letter (A-Z)
    /// - Z = Rotation (0-8, 0 = no rotation / all angles)
    /// For mirrored sprites: XXXXYZYZ (e.g., "POSSA2A8")
    /// where the second YZ indicates this frame is also used (mirrored) for rotation Z.
    /// </remarks>
    public class SpriteFrame
    {
        /// <summary>
        /// The 4-character sprite name (e.g., "POSS", "PLAY").
        /// </summary>
        public string SpriteName { get; }

        /// <summary>
        /// Frame letter (A-Z).
        /// </summary>
        public char Frame { get; }

        /// <summary>
        /// Rotation angle index (0-8).
        /// 0 = single angle used for all views.
        /// 1-8 = specific angle (1=front, 5=back, etc.)
        /// </summary>
        public int Rotation { get; }

        /// <summary>
        /// True if this frame should be horizontally mirrored.
        /// </summary>
        public bool IsMirrored { get; }

        /// <summary>
        /// The decoded picture data for this frame.
        /// </summary>
        public DoomPicture Picture { get; }

        public SpriteFrame(
            string spriteName,
            char frame,
            int rotation,
            bool isMirrored,
            DoomPicture picture)
        {
            SpriteName = spriteName;
            Frame = frame;
            Rotation = rotation;
            IsMirrored = isMirrored;
            Picture = picture;
        }
    }
}
