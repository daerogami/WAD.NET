using System.Collections.Generic;

namespace WAD.NET.Parsers.Dehacked
{
    /// <summary>
    /// Represents a parsed DEHACKED patch file.
    /// </summary>
    public class DehackedPatch
    {
        /// <summary>
        /// The DOOM version this patch is for (e.g., 19 for DOOM 1.9).
        /// </summary>
        public int DoomVersion { get; set; }

        /// <summary>
        /// The patch format version.
        /// </summary>
        public int PatchFormat { get; set; }

        /// <summary>
        /// Thing (actor) modifications.
        /// </summary>
        public List<DehThing> Things { get; } = new List<DehThing>();

        /// <summary>
        /// Frame (state) modifications.
        /// </summary>
        public List<DehFrame> Frames { get; } = new List<DehFrame>();

        /// <summary>
        /// Weapon modifications.
        /// </summary>
        public List<DehWeapon> Weapons { get; } = new List<DehWeapon>();

        /// <summary>
        /// Ammo modifications.
        /// </summary>
        public List<DehAmmo> Ammo { get; } = new List<DehAmmo>();

        /// <summary>
        /// Sound modifications.
        /// </summary>
        public List<DehSound> Sounds { get; } = new List<DehSound>();

        /// <summary>
        /// Sprite name modifications.
        /// </summary>
        public List<DehSprite> Sprites { get; } = new List<DehSprite>();

        /// <summary>
        /// Binary text replacements.
        /// </summary>
        public List<DehText> Texts { get; } = new List<DehText>();

        /// <summary>
        /// BEX-style string replacements.
        /// </summary>
        public List<DehString> Strings { get; } = new List<DehString>();

        /// <summary>
        /// Code pointer assignments (MBF+).
        /// </summary>
        public List<DehCodePointer> CodePointers { get; } = new List<DehCodePointer>();

        /// <summary>
        /// Miscellaneous game settings.
        /// </summary>
        public DehMisc Misc { get; set; }

        /// <summary>
        /// Par times for maps.
        /// </summary>
        public List<DehPar> Pars { get; } = new List<DehPar>();

        /// <summary>
        /// Cheats modifications.
        /// </summary>
        public List<DehCheat> Cheats { get; } = new List<DehCheat>();
    }

    /// <summary>
    /// Thing (actor) modifications.
    /// </summary>
    public class DehThing
    {
        /// <summary>The thing index (1-based in DEH).</summary>
        public int Index { get; set; }

        /// <summary>Optional name from DEH comment.</summary>
        public string Name { get; set; }

        /// <summary>Hit points.</summary>
        public int? HitPoints { get; set; }

        /// <summary>Movement speed in fixed-point units.</summary>
        public int? Speed { get; set; }

        /// <summary>Reaction time (tics before monster attacks).</summary>
        public int? ReactionTime { get; set; }

        /// <summary>Pain chance (0-255).</summary>
        public int? PainChance { get; set; }

        /// <summary>Damage amount for projectiles/melee.</summary>
        public int? Damage { get; set; }

        /// <summary>Mass (affects thrust from damage).</summary>
        public int? Mass { get; set; }

        /// <summary>Width (radius) in fixed-point units.</summary>
        public int? Width { get; set; }

        /// <summary>Height in fixed-point units.</summary>
        public int? Height { get; set; }

        /// <summary>Standard thing flags (mobjflags).</summary>
        public uint? Flags { get; set; }

        /// <summary>MBF extended flags (mobjflags2).</summary>
        public uint? Flags2 { get; set; }

        /// <summary>MBF21 extended flags.</summary>
        public uint? MBF21Flags { get; set; }

        /// <summary>Initial state index.</summary>
        public int? SpawnState { get; set; }

        /// <summary>First walking state index.</summary>
        public int? SeeState { get; set; }

        /// <summary>Pain state index.</summary>
        public int? PainState { get; set; }

        /// <summary>Melee attack state index.</summary>
        public int? MeleeState { get; set; }

        /// <summary>Missile attack state index.</summary>
        public int? MissileState { get; set; }

        /// <summary>Death state index.</summary>
        public int? DeathState { get; set; }

        /// <summary>Extreme death (gib) state index.</summary>
        public int? XDeathState { get; set; }

        /// <summary>Raise (resurrection) state index.</summary>
        public int? RaiseState { get; set; }

        /// <summary>Alert sound index.</summary>
        public int? AlertSound { get; set; }

        /// <summary>Attack sound index.</summary>
        public int? AttackSound { get; set; }

        /// <summary>Pain sound index.</summary>
        public int? PainSound { get; set; }

        /// <summary>Death sound index.</summary>
        public int? DeathSound { get; set; }

        /// <summary>Active/idle sound index.</summary>
        public int? ActionSound { get; set; }

        /// <summary>Drop item type (MBF).</summary>
        public int? DropItem { get; set; }

        /// <summary>Infighting group (MBF21).</summary>
        public int? InfightingGroup { get; set; }

        /// <summary>Projectile group (MBF21).</summary>
        public int? ProjectileGroup { get; set; }

        /// <summary>Splash group (MBF21).</summary>
        public int? SplashGroup { get; set; }

        /// <summary>Melee range (MBF21).</summary>
        public int? MeleeRange { get; set; }

        /// <summary>Rip sound (MBF21).</summary>
        public int? RipSound { get; set; }
    }

    /// <summary>
    /// Frame (state) modifications.
    /// </summary>
    public class DehFrame
    {
        /// <summary>The frame index.</summary>
        public int Index { get; set; }

        /// <summary>Sprite number.</summary>
        public int? SpriteNumber { get; set; }

        /// <summary>Sprite subnumber (frame + fullbright flag).</summary>
        public int? SpriteSubnumber { get; set; }

        /// <summary>Duration in tics.</summary>
        public int? Duration { get; set; }

        /// <summary>Next state index.</summary>
        public int? NextState { get; set; }

        /// <summary>Misc1 parameter for action functions.</summary>
        public int? Misc1 { get; set; }

        /// <summary>Misc2 parameter for action functions.</summary>
        public int? Misc2 { get; set; }

        /// <summary>Args (MBF21) - 8 parameters.</summary>
        public int[] Args { get; set; }
    }

    /// <summary>
    /// Weapon modifications.
    /// </summary>
    public class DehWeapon
    {
        /// <summary>The weapon index.</summary>
        public int Index { get; set; }

        /// <summary>Optional name from DEH comment.</summary>
        public string Name { get; set; }

        /// <summary>Ammo type used.</summary>
        public int? AmmoType { get; set; }

        /// <summary>Deselect (lower) state.</summary>
        public int? DeselectState { get; set; }

        /// <summary>Select (raise) state.</summary>
        public int? SelectState { get; set; }

        /// <summary>Ready (bobbing) state.</summary>
        public int? ReadyState { get; set; }

        /// <summary>Fire state.</summary>
        public int? AttackState { get; set; }

        /// <summary>Muzzle flash state.</summary>
        public int? FlashState { get; set; }

        /// <summary>Ammo per shot (MBF21).</summary>
        public int? AmmoPerShot { get; set; }

        /// <summary>MBF21 weapon flags.</summary>
        public uint? MBF21Flags { get; set; }
    }

    /// <summary>
    /// Ammo type modifications.
    /// </summary>
    public class DehAmmo
    {
        /// <summary>The ammo index.</summary>
        public int Index { get; set; }

        /// <summary>Maximum ammo capacity.</summary>
        public int? MaxAmmo { get; set; }

        /// <summary>Ammo per pickup item.</summary>
        public int? PerAmmo { get; set; }
    }

    /// <summary>
    /// Sound modifications.
    /// </summary>
    public class DehSound
    {
        /// <summary>The sound index.</summary>
        public int Index { get; set; }

        /// <summary>Zero/One value for singularity.</summary>
        public int? ZeroOne { get; set; }

        /// <summary>Sound priority.</summary>
        public int? Priority { get; set; }

        /// <summary>Link to other sound.</summary>
        public int? Link { get; set; }

        /// <summary>Pitch variation.</summary>
        public int? Pitch { get; set; }

        /// <summary>Volume.</summary>
        public int? Volume { get; set; }
    }

    /// <summary>
    /// Sprite name modifications.
    /// </summary>
    public class DehSprite
    {
        /// <summary>The sprite index.</summary>
        public int Index { get; set; }

        /// <summary>Offset in sprite name table.</summary>
        public int? Offset { get; set; }
    }

    /// <summary>
    /// Binary text replacement (old DEH style).
    /// </summary>
    public class DehText
    {
        /// <summary>Length of original text.</summary>
        public int OldLength { get; set; }

        /// <summary>Length of new text.</summary>
        public int NewLength { get; set; }

        /// <summary>Original text.</summary>
        public string OldText { get; set; }

        /// <summary>Replacement text.</summary>
        public string NewText { get; set; }
    }

    /// <summary>
    /// BEX-style string replacement.
    /// </summary>
    public class DehString
    {
        /// <summary>String mnemonic (e.g., HUSTR_E1M1).</summary>
        public string Mnemonic { get; set; }

        /// <summary>New string value.</summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// Code pointer assignment (MBF+).
    /// </summary>
    public class DehCodePointer
    {
        /// <summary>Frame (state) index.</summary>
        public int FrameIndex { get; set; }

        /// <summary>Code pointer name (e.g., A_BFGSpray).</summary>
        public string CodePointerName { get; set; }
    }

    /// <summary>
    /// Miscellaneous game settings.
    /// </summary>
    public class DehMisc
    {
        /// <summary>Initial health.</summary>
        public int? InitialHealth { get; set; }

        /// <summary>Initial bullets.</summary>
        public int? InitialBullets { get; set; }

        /// <summary>Max health from regular pickups.</summary>
        public int? MaxHealth { get; set; }

        /// <summary>Max armor from green armor.</summary>
        public int? MaxArmor { get; set; }

        /// <summary>Green armor class.</summary>
        public int? GreenArmorClass { get; set; }

        /// <summary>Blue armor class.</summary>
        public int? BlueArmorClass { get; set; }

        /// <summary>Max soulsphere health.</summary>
        public int? MaxSoulsphere { get; set; }

        /// <summary>Soulsphere health bonus.</summary>
        public int? SoulsphereHealth { get; set; }

        /// <summary>Megasphere health.</summary>
        public int? MegasphereHealth { get; set; }

        /// <summary>God mode health.</summary>
        public int? GodModeHealth { get; set; }

        /// <summary>IDFA armor.</summary>
        public int? IdfaArmor { get; set; }

        /// <summary>IDFA armor class.</summary>
        public int? IdfaArmorClass { get; set; }

        /// <summary>IDKFA armor.</summary>
        public int? IdkfaArmor { get; set; }

        /// <summary>IDKFA armor class.</summary>
        public int? IdkfaArmorClass { get; set; }

        /// <summary>BFG cells per shot.</summary>
        public int? BfgCellsPerShot { get; set; }

        /// <summary>Monsters infight.</summary>
        public int? MonstersInfight { get; set; }
    }

    /// <summary>
    /// Par time definition.
    /// </summary>
    public class DehPar
    {
        /// <summary>Episode number (0 for DOOM II).</summary>
        public int Episode { get; set; }

        /// <summary>Map number.</summary>
        public int Map { get; set; }

        /// <summary>Par time in seconds.</summary>
        public int Seconds { get; set; }
    }

    /// <summary>
    /// Cheat code modification.
    /// </summary>
    public class DehCheat
    {
        /// <summary>Cheat name.</summary>
        public string Name { get; set; }

        /// <summary>New cheat sequence.</summary>
        public string Sequence { get; set; }
    }
}
