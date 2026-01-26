using System.Collections.Generic;

namespace WAD.NET.Definitions
{
    /// <summary>
    /// Represents a complete sprite definition with all frames and rotations.
    /// </summary>
    public class SpriteDefinition
    {
        /// <summary>
        /// Number of rotation angles for sprites with directional views.
        /// </summary>
        public const int RotationCount = 8;

        /// <summary>
        /// The 4-character sprite name (e.g., "POSS", "PLAY").
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Dictionary mapping frame letter to an array of rotations.
        /// Each array has 8 entries (one per rotation angle), or a single entry for rotation 0.
        /// </summary>
        public Dictionary<char, SpriteFrame[]> Frames { get; }

        public SpriteDefinition(string name)
        {
            Name = name;
            Frames = new Dictionary<char, SpriteFrame[]>();
        }

        /// <summary>
        /// Adds a frame to this sprite definition.
        /// </summary>
        public void AddFrame(SpriteFrame frame)
        {
            if (!Frames.TryGetValue(frame.Frame, out var rotations))
            {
                // Rotation 0 uses a single frame for all angles
                // Rotations 1-8 need an array of 8 frames
                rotations = new SpriteFrame[RotationCount];
                Frames[frame.Frame] = rotations;
            }

            if (frame.Rotation == 0)
            {
                // Rotation 0 means this single frame is used for all angles
                // Store it in all rotation slots
                for (int i = 0; i < RotationCount; i++)
                {
                    rotations[i] = frame;
                }
            }
            else if (frame.Rotation >= 1 && frame.Rotation <= 8)
            {
                // Store in the appropriate rotation slot (1-indexed to 0-indexed)
                rotations[frame.Rotation - 1] = frame;
            }
        }

        /// <summary>
        /// Gets a frame for the specified frame letter and rotation angle.
        /// </summary>
        /// <param name="frame">Frame letter (A-Z).</param>
        /// <param name="rotation">Rotation angle index (1-8).</param>
        /// <returns>The sprite frame, or null if not found.</returns>
        public SpriteFrame GetFrame(char frame, int rotation)
        {
            if (!Frames.TryGetValue(frame, out var rotations))
                return null;

            // Handle rotation 0 (all angles) or specific rotation
            if (rotation < 1 || rotation > 8)
                return rotations[0];

            return rotations[rotation - 1];
        }

        /// <summary>
        /// Gets all available frame letters for this sprite.
        /// </summary>
        public IEnumerable<char> GetFrameLetters()
        {
            return Frames.Keys;
        }

        /// <summary>
        /// Checks if this sprite has the specified frame.
        /// </summary>
        public bool HasFrame(char frame)
        {
            return Frames.ContainsKey(frame);
        }
    }
}
