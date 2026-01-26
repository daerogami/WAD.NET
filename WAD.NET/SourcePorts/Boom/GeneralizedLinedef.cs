using System;

namespace WAD.NET.SourcePorts.Boom
{
    /// <summary>
    /// Boom generalized linedef parser. Boom uses special linedef types 0x2F80-0x7FFF for generalized actions.
    /// </summary>
    public static class BoomGeneralizedLinedef
    {
        /// <summary>Base type for generalized floor actions (24576).</summary>
        public const ushort FloorBase = 0x6000;

        /// <summary>Base type for generalized ceiling actions (16384).</summary>
        public const ushort CeilingBase = 0x4000;

        /// <summary>Base type for generalized door actions (15360).</summary>
        public const ushort DoorBase = 0x3C00;

        /// <summary>Base type for generalized locked door actions (14336).</summary>
        public const ushort LockedDoorBase = 0x3800;

        /// <summary>Base type for generalized lift actions (13312).</summary>
        public const ushort LiftBase = 0x3400;

        /// <summary>Base type for generalized stair actions (12288).</summary>
        public const ushort StairsBase = 0x3000;

        /// <summary>Base type for generalized crusher actions (12160).</summary>
        public const ushort CrusherBase = 0x2F80;

        /// <summary>
        /// Checks if a linedef type is a Boom generalized type.
        /// </summary>
        public static bool IsGeneralized(ushort type)
        {
            return type >= CrusherBase;
        }

        /// <summary>
        /// Parses a generalized linedef type into its component parts.
        /// </summary>
        /// <param name="type">The linedef special type.</param>
        /// <returns>A GeneralizedAction describing the action, or null if not a generalized type.</returns>
        public static GeneralizedAction Parse(ushort type)
        {
            if (!IsGeneralized(type))
                return null;

            if (type >= FloorBase)
                return ParseFloor(type);
            if (type >= CeilingBase)
                return ParseCeiling(type);
            if (type >= DoorBase)
                return ParseDoor(type);
            if (type >= LockedDoorBase)
                return ParseLockedDoor(type);
            if (type >= LiftBase)
                return ParseLift(type);
            if (type >= StairsBase)
                return ParseStairs(type);
            if (type >= CrusherBase)
                return ParseCrusher(type);

            return null;
        }

        private static GeneralizedFloorAction ParseFloor(ushort type)
        {
            int offset = type - FloorBase;

            return new GeneralizedFloorAction
            {
                ActionType = GeneralizedActionType.Floor,
                Trigger = (TriggerType)(offset & 0x07),
                Speed = (SpeedType)((offset >> 3) & 0x03),
                Model = (ModelType)((offset >> 5) & 0x01),
                Direction = (DirectionType)((offset >> 6) & 0x01),
                Target = (FloorTargetType)((offset >> 7) & 0x07),
                Change = (ChangeType)((offset >> 10) & 0x03),
                Crush = (offset & 0x1000) != 0
            };
        }

        private static GeneralizedCeilingAction ParseCeiling(ushort type)
        {
            int offset = type - CeilingBase;

            return new GeneralizedCeilingAction
            {
                ActionType = GeneralizedActionType.Ceiling,
                Trigger = (TriggerType)(offset & 0x07),
                Speed = (SpeedType)((offset >> 3) & 0x03),
                Model = (ModelType)((offset >> 5) & 0x01),
                Direction = (DirectionType)((offset >> 6) & 0x01),
                Target = (CeilingTargetType)((offset >> 7) & 0x07),
                Change = (ChangeType)((offset >> 10) & 0x03),
                Crush = (offset & 0x1000) != 0
            };
        }

        private static GeneralizedDoorAction ParseDoor(ushort type)
        {
            int offset = type - DoorBase;

            return new GeneralizedDoorAction
            {
                ActionType = GeneralizedActionType.Door,
                Trigger = (TriggerType)(offset & 0x07),
                Speed = (SpeedType)((offset >> 3) & 0x03),
                Kind = (DoorKindType)((offset >> 5) & 0x03),
                Monster = (offset & 0x0080) != 0,
                Delay = (DoorDelayType)((offset >> 8) & 0x03)
            };
        }

        private static GeneralizedLockedDoorAction ParseLockedDoor(ushort type)
        {
            int offset = type - LockedDoorBase;

            return new GeneralizedLockedDoorAction
            {
                ActionType = GeneralizedActionType.LockedDoor,
                Trigger = (TriggerType)(offset & 0x07),
                Speed = (SpeedType)((offset >> 3) & 0x03),
                Kind = (LockedDoorKindType)((offset >> 5) & 0x01),
                Lock = (LockType)((offset >> 6) & 0x07),
                SkullRequired = (offset & 0x0200) != 0
            };
        }

        private static GeneralizedLiftAction ParseLift(ushort type)
        {
            int offset = type - LiftBase;

            return new GeneralizedLiftAction
            {
                ActionType = GeneralizedActionType.Lift,
                Trigger = (TriggerType)(offset & 0x07),
                Speed = (SpeedType)((offset >> 3) & 0x03),
                Monster = (offset & 0x0020) != 0,
                Delay = (LiftDelayType)((offset >> 6) & 0x03),
                Target = (LiftTargetType)((offset >> 8) & 0x03)
            };
        }

        private static GeneralizedStairsAction ParseStairs(ushort type)
        {
            int offset = type - StairsBase;

            return new GeneralizedStairsAction
            {
                ActionType = GeneralizedActionType.Stairs,
                Trigger = (TriggerType)(offset & 0x07),
                Speed = (SpeedType)((offset >> 3) & 0x03),
                Monster = (offset & 0x0020) != 0,
                Step = (StairStepType)((offset >> 6) & 0x03),
                Direction = (DirectionType)((offset >> 8) & 0x01),
                IgnoreTexture = (offset & 0x0200) != 0
            };
        }

        private static GeneralizedCrusherAction ParseCrusher(ushort type)
        {
            int offset = type - CrusherBase;

            return new GeneralizedCrusherAction
            {
                ActionType = GeneralizedActionType.Crusher,
                Trigger = (TriggerType)(offset & 0x07),
                Speed = (SpeedType)((offset >> 3) & 0x03),
                Monster = (offset & 0x0020) != 0,
                Silent = (offset & 0x0040) != 0
            };
        }
    }

    /// <summary>
    /// Types of generalized actions.
    /// </summary>
    public enum GeneralizedActionType
    {
        Floor,
        Ceiling,
        Door,
        LockedDoor,
        Lift,
        Stairs,
        Crusher
    }

    /// <summary>
    /// Trigger type for generalized linedefs.
    /// </summary>
    public enum TriggerType
    {
        WalkOnce,
        WalkRepeatable,
        SwitchOnce,
        SwitchRepeatable,
        GunOnce,
        GunRepeatable,
        PushOnce,
        PushRepeatable
    }

    /// <summary>
    /// Movement speed types.
    /// </summary>
    public enum SpeedType
    {
        Slow,
        Normal,
        Fast,
        Turbo
    }

    /// <summary>
    /// Model reference types.
    /// </summary>
    public enum ModelType
    {
        Trigger,
        Numeric
    }

    /// <summary>
    /// Movement direction.
    /// </summary>
    public enum DirectionType
    {
        Down,
        Up
    }

    /// <summary>
    /// Floor target height types.
    /// </summary>
    public enum FloorTargetType
    {
        HighestNeighborFloor,
        LowestNeighborFloor,
        NextNeighborFloor,
        LowestNeighborCeiling,
        Ceiling,
        ShortestLowerTexture,
        TwentyFourUnits,
        ThirtyTwoUnits
    }

    /// <summary>
    /// Ceiling target height types.
    /// </summary>
    public enum CeilingTargetType
    {
        HighestNeighborCeiling,
        LowestNeighborCeiling,
        NextNeighborCeiling,
        HighestNeighborFloor,
        Floor,
        ShortestUpperTexture,
        TwentyFourUnits,
        ThirtyTwoUnits
    }

    /// <summary>
    /// Change types for floor/ceiling.
    /// </summary>
    public enum ChangeType
    {
        None,
        ZeroTexture,
        TextureOnly,
        TextureAndType
    }

    /// <summary>
    /// Door behavior types.
    /// </summary>
    public enum DoorKindType
    {
        OpenWaitClose,
        OpenStay,
        CloseWaitOpen,
        CloseStay
    }

    /// <summary>
    /// Door delay types.
    /// </summary>
    public enum DoorDelayType
    {
        OneSecond,
        FourSeconds,
        NineSeconds,
        ThirtySeconds
    }

    /// <summary>
    /// Locked door types.
    /// </summary>
    public enum LockedDoorKindType
    {
        OpenWaitClose,
        OpenStay
    }

    /// <summary>
    /// Lock types for locked doors.
    /// </summary>
    public enum LockType
    {
        Any,
        RedCard,
        BlueCard,
        YellowCard,
        RedSkull,
        BlueSkull,
        YellowSkull,
        All
    }

    /// <summary>
    /// Lift delay types.
    /// </summary>
    public enum LiftDelayType
    {
        OneSecond,
        ThreeSeconds,
        FiveSeconds,
        TenSeconds
    }

    /// <summary>
    /// Lift target types.
    /// </summary>
    public enum LiftTargetType
    {
        LowestNeighborFloor,
        NextNeighborFloor,
        LowestNeighborCeiling,
        Perpetual
    }

    /// <summary>
    /// Stair step height types.
    /// </summary>
    public enum StairStepType
    {
        FourUnits,
        EightUnits,
        SixteenUnits,
        TwentyFourUnits
    }

    /// <summary>
    /// Base class for generalized actions.
    /// </summary>
    public class GeneralizedAction
    {
        /// <summary>Type of generalized action.</summary>
        public GeneralizedActionType ActionType { get; init; }

        /// <summary>How the action is triggered.</summary>
        public TriggerType Trigger { get; init; }

        /// <summary>Movement speed.</summary>
        public SpeedType Speed { get; init; }
    }

    /// <summary>
    /// Generalized floor action.
    /// </summary>
    public class GeneralizedFloorAction : GeneralizedAction
    {
        /// <summary>Model reference type.</summary>
        public ModelType Model { get; init; }

        /// <summary>Movement direction.</summary>
        public DirectionType Direction { get; init; }

        /// <summary>Target height type.</summary>
        public FloorTargetType Target { get; init; }

        /// <summary>Change type.</summary>
        public ChangeType Change { get; init; }

        /// <summary>Whether this crushes the player.</summary>
        public bool Crush { get; init; }
    }

    /// <summary>
    /// Generalized ceiling action.
    /// </summary>
    public class GeneralizedCeilingAction : GeneralizedAction
    {
        /// <summary>Model reference type.</summary>
        public ModelType Model { get; init; }

        /// <summary>Movement direction.</summary>
        public DirectionType Direction { get; init; }

        /// <summary>Target height type.</summary>
        public CeilingTargetType Target { get; init; }

        /// <summary>Change type.</summary>
        public ChangeType Change { get; init; }

        /// <summary>Whether this crushes the player.</summary>
        public bool Crush { get; init; }
    }

    /// <summary>
    /// Generalized door action.
    /// </summary>
    public class GeneralizedDoorAction : GeneralizedAction
    {
        /// <summary>Door behavior.</summary>
        public DoorKindType Kind { get; init; }

        /// <summary>Whether monsters can trigger.</summary>
        public bool Monster { get; init; }

        /// <summary>How long door waits before closing.</summary>
        public DoorDelayType Delay { get; init; }
    }

    /// <summary>
    /// Generalized locked door action.
    /// </summary>
    public class GeneralizedLockedDoorAction : GeneralizedAction
    {
        /// <summary>Door behavior.</summary>
        public LockedDoorKindType Kind { get; init; }

        /// <summary>Required key type.</summary>
        public LockType Lock { get; init; }

        /// <summary>Whether skull keys are required (vs card keys).</summary>
        public bool SkullRequired { get; init; }
    }

    /// <summary>
    /// Generalized lift action.
    /// </summary>
    public class GeneralizedLiftAction : GeneralizedAction
    {
        /// <summary>Whether monsters can trigger.</summary>
        public bool Monster { get; init; }

        /// <summary>How long lift waits.</summary>
        public LiftDelayType Delay { get; init; }

        /// <summary>Target type.</summary>
        public LiftTargetType Target { get; init; }
    }

    /// <summary>
    /// Generalized stairs action.
    /// </summary>
    public class GeneralizedStairsAction : GeneralizedAction
    {
        /// <summary>Whether monsters can trigger.</summary>
        public bool Monster { get; init; }

        /// <summary>Step height.</summary>
        public StairStepType Step { get; init; }

        /// <summary>Stair direction.</summary>
        public DirectionType Direction { get; init; }

        /// <summary>Whether to ignore floor texture matches.</summary>
        public bool IgnoreTexture { get; init; }
    }

    /// <summary>
    /// Generalized crusher action.
    /// </summary>
    public class GeneralizedCrusherAction : GeneralizedAction
    {
        /// <summary>Whether monsters can trigger.</summary>
        public bool Monster { get; init; }

        /// <summary>Whether the crusher is silent.</summary>
        public bool Silent { get; init; }
    }
}
