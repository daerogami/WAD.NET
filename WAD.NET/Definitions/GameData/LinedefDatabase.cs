using System.Collections.Generic;

namespace WAD.NET.Definitions.GameData
{
    /// <summary>
    /// Database of linedef action types for all supported games.
    /// </summary>
    public static class LinedefDatabase
    {
        private static readonly Dictionary<int, LinedefActionDefinition> _actions = new Dictionary<int, LinedefActionDefinition>();

        static LinedefDatabase()
        {
            RegisterDoomLinedefActions();
            RegisterBoomLinedefActions();
        }

        /// <summary>
        /// Looks up a linedef action definition by its number.
        /// </summary>
        /// <param name="action">The linedef action number.</param>
        /// <returns>The linedef action definition, or null if not found.</returns>
        public static LinedefActionDefinition? Get(int action)
        {
            return _actions.TryGetValue(action, out var def) ? def : null;
        }

        /// <summary>
        /// Checks if a linedef action is a level exit.
        /// </summary>
        /// <param name="action">The linedef action number.</param>
        /// <returns>True if the action triggers a level exit.</returns>
        public static bool IsExit(int action)
        {
            var def = Get(action);
            return def?.ActionType == LinedefActionType.Exit;
        }

        /// <summary>
        /// Checks if a linedef action is a teleporter.
        /// </summary>
        /// <param name="action">The linedef action number.</param>
        /// <returns>True if the action triggers teleportation.</returns>
        public static bool IsTeleporter(int action)
        {
            var def = Get(action);
            return def?.ActionType == LinedefActionType.Teleport;
        }

        private static void Register(LinedefActionDefinition def)
        {
            _actions[def.Action] = def;
        }

        private static void RegisterDoomLinedefActions()
        {
            // Doors
            Register(new LinedefActionDefinition
            {
                Action = 1,
                Name = "Door Open Wait Close",
                Description = "Door opens, waits, then closes (player use)",
                ActionType = LinedefActionType.Door,
                Trigger = TriggerType.Push,
                Repeatable = true,
                Game = GameType.AllDoom
            });
            Register(new LinedefActionDefinition
            {
                Action = 2,
                Name = "Door Open Stay",
                Description = "Door opens and stays open (walk trigger)",
                ActionType = LinedefActionType.Door,
                Trigger = TriggerType.Walk,
                Game = GameType.AllDoom
            });
            Register(new LinedefActionDefinition
            {
                Action = 3,
                Name = "Door Close",
                Description = "Door closes (walk trigger)",
                ActionType = LinedefActionType.Door,
                Trigger = TriggerType.Walk,
                Game = GameType.AllDoom
            });
            Register(new LinedefActionDefinition
            {
                Action = 4,
                Name = "Door Open Wait Close",
                Description = "Door opens, waits, then closes (walk trigger)",
                ActionType = LinedefActionType.Door,
                Trigger = TriggerType.Walk,
                MonsterActivated = true,
                Game = GameType.AllDoom
            });
            Register(new LinedefActionDefinition
            {
                Action = 26,
                Name = "Door Open Wait Close (Blue Key)",
                Description = "Blue key door, opens, waits, closes",
                ActionType = LinedefActionType.Door,
                Trigger = TriggerType.Push,
                Repeatable = true,
                Game = GameType.AllDoom
            });
            Register(new LinedefActionDefinition
            {
                Action = 27,
                Name = "Door Open Wait Close (Yellow Key)",
                Description = "Yellow key door, opens, waits, closes",
                ActionType = LinedefActionType.Door,
                Trigger = TriggerType.Push,
                Repeatable = true,
                Game = GameType.AllDoom
            });
            Register(new LinedefActionDefinition
            {
                Action = 28,
                Name = "Door Open Wait Close (Red Key)",
                Description = "Red key door, opens, waits, closes",
                ActionType = LinedefActionType.Door,
                Trigger = TriggerType.Push,
                Repeatable = true,
                Game = GameType.AllDoom
            });

            // Floors
            Register(new LinedefActionDefinition
            {
                Action = 5,
                Name = "Floor Raise to Lowest Ceiling",
                Description = "Raises floor to lowest adjacent ceiling",
                ActionType = LinedefActionType.Floor,
                Trigger = TriggerType.Walk,
                Game = GameType.AllDoom
            });
            Register(new LinedefActionDefinition
            {
                Action = 18,
                Name = "Floor Raise to Next Higher",
                Description = "Raises floor to next higher adjacent floor",
                ActionType = LinedefActionType.Floor,
                Trigger = TriggerType.Switch,
                Game = GameType.AllDoom
            });
            Register(new LinedefActionDefinition
            {
                Action = 23,
                Name = "Floor Lower to Lowest",
                Description = "Lowers floor to lowest adjacent floor",
                ActionType = LinedefActionType.Floor,
                Trigger = TriggerType.Switch,
                Game = GameType.AllDoom
            });

            // Lifts
            Register(new LinedefActionDefinition
            {
                Action = 10,
                Name = "Lift Lower Wait Raise",
                Description = "Platform lowers, waits, returns to original height",
                ActionType = LinedefActionType.Lift,
                Trigger = TriggerType.Walk,
                Game = GameType.AllDoom
            });
            Register(new LinedefActionDefinition
            {
                Action = 62,
                Name = "Lift Lower Wait Raise",
                Description = "Platform lowers, waits, returns (switch, repeatable)",
                ActionType = LinedefActionType.Lift,
                Trigger = TriggerType.Switch,
                Repeatable = true,
                Game = GameType.AllDoom
            });

            // Exits
            Register(new LinedefActionDefinition
            {
                Action = 11,
                Name = "Exit Level",
                Description = "Exits to next level (switch)",
                ActionType = LinedefActionType.Exit,
                Trigger = TriggerType.Switch,
                Game = GameType.AllDoom
            });
            Register(new LinedefActionDefinition
            {
                Action = 51,
                Name = "Exit Secret Level",
                Description = "Exits to secret level (switch)",
                ActionType = LinedefActionType.Exit,
                Trigger = TriggerType.Switch,
                Game = GameType.AllDoom
            });
            Register(new LinedefActionDefinition
            {
                Action = 52,
                Name = "Exit Level",
                Description = "Exits to next level (walk)",
                ActionType = LinedefActionType.Exit,
                Trigger = TriggerType.Walk,
                Game = GameType.AllDoom
            });
            Register(new LinedefActionDefinition
            {
                Action = 124,
                Name = "Exit Secret Level",
                Description = "Exits to secret level (walk)",
                ActionType = LinedefActionType.Exit,
                Trigger = TriggerType.Walk,
                Game = GameType.AllDoom
            });

            // Teleporters
            Register(new LinedefActionDefinition
            {
                Action = 39,
                Name = "Teleport",
                Description = "Teleport to thing with matching tag (walk, monster)",
                ActionType = LinedefActionType.Teleport,
                Trigger = TriggerType.Walk,
                MonsterActivated = true,
                Game = GameType.AllDoom
            });
            Register(new LinedefActionDefinition
            {
                Action = 97,
                Name = "Teleport Retrigger",
                Description = "Teleport to thing with matching tag (walk, retrigger, monster)",
                ActionType = LinedefActionType.Teleport,
                Trigger = TriggerType.Walk,
                Repeatable = true,
                MonsterActivated = true,
                Game = GameType.AllDoom
            });
            Register(new LinedefActionDefinition
            {
                Action = 125,
                Name = "Teleport (Monsters Only)",
                Description = "Teleport monsters only, walk trigger",
                ActionType = LinedefActionType.Teleport,
                Trigger = TriggerType.Walk,
                MonsterActivated = true,
                Game = GameType.AllDoom
            });
            Register(new LinedefActionDefinition
            {
                Action = 126,
                Name = "Teleport (Monsters Only, Retrigger)",
                Description = "Teleport monsters only, walk trigger, repeatable",
                ActionType = LinedefActionType.Teleport,
                Trigger = TriggerType.Walk,
                Repeatable = true,
                MonsterActivated = true,
                Game = GameType.AllDoom
            });

            // Stairs
            Register(new LinedefActionDefinition
            {
                Action = 8,
                Name = "Stairs Build 8",
                Description = "Build stairs, 8 unit step height",
                ActionType = LinedefActionType.Stairs,
                Trigger = TriggerType.Walk,
                Game = GameType.AllDoom
            });
            Register(new LinedefActionDefinition
            {
                Action = 100,
                Name = "Stairs Build 16 + Crush",
                Description = "Build stairs, 16 unit step height, crush",
                ActionType = LinedefActionType.Stairs,
                Trigger = TriggerType.Walk,
                Game = GameType.AllDoom
            });

            // Crushers
            Register(new LinedefActionDefinition
            {
                Action = 6,
                Name = "Crusher Start (Fast)",
                Description = "Fast crusher ceiling (walk)",
                ActionType = LinedefActionType.Crusher,
                Trigger = TriggerType.Walk,
                Game = GameType.AllDoom
            });
            Register(new LinedefActionDefinition
            {
                Action = 25,
                Name = "Crusher Start (Slow)",
                Description = "Slow crusher ceiling (walk)",
                ActionType = LinedefActionType.Crusher,
                Trigger = TriggerType.Walk,
                Game = GameType.AllDoom
            });

            // Light
            Register(new LinedefActionDefinition
            {
                Action = 35,
                Name = "Light to 35",
                Description = "Set light level to 35 (walk)",
                ActionType = LinedefActionType.Light,
                Trigger = TriggerType.Walk,
                Game = GameType.AllDoom
            });
            Register(new LinedefActionDefinition
            {
                Action = 104,
                Name = "Light to Dimmest Adjacent",
                Description = "Set light to dimmest adjacent sector (walk)",
                ActionType = LinedefActionType.Light,
                Trigger = TriggerType.Walk,
                Game = GameType.AllDoom
            });
        }

        private static void RegisterBoomLinedefActions()
        {
            // Boom generalized linedef actions - stub for future expansion
        }
    }
}
