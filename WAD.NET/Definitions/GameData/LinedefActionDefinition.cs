namespace WAD.NET.Definitions.GameData
{
    /// <summary>
    /// Defines a linedef action type with its trigger and behavior.
    /// </summary>
    public class LinedefActionDefinition
    {
        /// <summary>
        /// Linedef action number.
        /// </summary>
        public int Action { get; init; }

        /// <summary>
        /// Short name describing the action (e.g., "Door Open Wait Close").
        /// </summary>
        public string Name { get; init; } = "";

        /// <summary>
        /// Detailed description of the action's behavior.
        /// </summary>
        public string Description { get; init; } = "";

        /// <summary>
        /// Type classification for this action.
        /// </summary>
        public LinedefActionType ActionType { get; init; }

        /// <summary>
        /// How this action is triggered.
        /// </summary>
        public TriggerType Trigger { get; init; }

        /// <summary>
        /// True if this action can be triggered multiple times.
        /// </summary>
        public bool Repeatable { get; init; }

        /// <summary>
        /// True if monsters can trigger this action.
        /// </summary>
        public bool MonsterActivated { get; init; }

        /// <summary>
        /// Which game(s) this action appears in.
        /// </summary>
        public GameType Game { get; init; }
    }
}
