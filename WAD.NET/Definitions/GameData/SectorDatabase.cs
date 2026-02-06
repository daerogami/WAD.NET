using System.Collections.Generic;

namespace WAD.NET.Definitions.GameData
{
    /// <summary>
    /// Database of sector special types for all supported games.
    /// </summary>
    public static class SectorDatabase
    {
        private static readonly Dictionary<int, SectorSpecialDefinition> _specials = new Dictionary<int, SectorSpecialDefinition>();

        static SectorDatabase()
        {
            RegisterDoomSectorSpecials();
            RegisterBoomSectorSpecials();
        }

        /// <summary>
        /// Looks up a sector special definition by its number.
        /// </summary>
        /// <param name="special">The sector special number.</param>
        /// <returns>The sector special definition, or null if not found.</returns>
        public static SectorSpecialDefinition? Get(int special)
        {
            return _specials.TryGetValue(special, out var def) ? def : null;
        }

        /// <summary>
        /// Checks if a sector special is a secret sector.
        /// </summary>
        /// <param name="special">The sector special number.</param>
        /// <returns>True if the sector is a secret.</returns>
        public static bool IsSecret(int special) => special == 9;

        /// <summary>
        /// Checks if a sector special causes damage.
        /// </summary>
        /// <param name="special">The sector special number.</param>
        /// <returns>True if the sector deals damage.</returns>
        public static bool IsDamaging(int special)
        {
            var def = Get(special);
            return def?.DamageAmount > 0;
        }

        /// <summary>
        /// Checks if a sector special has a lighting effect.
        /// </summary>
        /// <param name="special">The sector special number.</param>
        /// <returns>True if the sector has a light effect.</returns>
        public static bool IsLightEffect(int special)
        {
            var def = Get(special);
            return def?.LightEffect != LightEffect.None;
        }

        private static void Register(SectorSpecialDefinition def)
        {
            _specials[def.Special] = def;
        }

        private static void RegisterDoomSectorSpecials()
        {
            // Light effects
            Register(new SectorSpecialDefinition
            {
                Special = 1,
                Name = "Light Blink (random)",
                Type = SectorSpecialType.Light,
                LightEffect = LightEffect.BlinkRandom,
                Game = GameType.AllDoom
            });
            Register(new SectorSpecialDefinition
            {
                Special = 2,
                Name = "Light Blink (0.5 sec)",
                Type = SectorSpecialType.Light,
                LightEffect = LightEffect.BlinkHalf,
                Game = GameType.AllDoom
            });
            Register(new SectorSpecialDefinition
            {
                Special = 3,
                Name = "Light Blink (1 sec)",
                Type = SectorSpecialType.Light,
                LightEffect = LightEffect.BlinkFull,
                Game = GameType.AllDoom
            });

            // Damage sectors
            Register(new SectorSpecialDefinition
            {
                Special = 4,
                Name = "Damage -10/20% health + light blink",
                Description = "Nukage damage with blinking light",
                Type = SectorSpecialType.Damage,
                DamageAmount = 20,
                DamageInterval = 32,
                LightEffect = LightEffect.BlinkHalf,
                Game = GameType.AllDoom
            });
            Register(new SectorSpecialDefinition
            {
                Special = 5,
                Name = "Damage -5/10% health",
                Description = "Light nukage/slime damage",
                Type = SectorSpecialType.Damage,
                DamageAmount = 10,
                DamageInterval = 32,
                Game = GameType.AllDoom
            });
            Register(new SectorSpecialDefinition
            {
                Special = 7,
                Name = "Damage -2/5% health",
                Description = "Light damage (blood, etc.)",
                Type = SectorSpecialType.Damage,
                DamageAmount = 5,
                DamageInterval = 32,
                Game = GameType.AllDoom
            });

            // Oscillating light
            Register(new SectorSpecialDefinition
            {
                Special = 8,
                Name = "Light Oscillates",
                Type = SectorSpecialType.Light,
                LightEffect = LightEffect.Oscillate,
                Game = GameType.AllDoom
            });

            // Secret
            Register(new SectorSpecialDefinition
            {
                Special = 9,
                Name = "Secret",
                Description = "Counted as secret when entered",
                Type = SectorSpecialType.Secret,
                Game = GameType.AllDoom
            });

            // Door close
            Register(new SectorSpecialDefinition
            {
                Special = 10,
                Name = "Door Close (30 sec)",
                Description = "30 seconds after level start, ceiling closes",
                Type = SectorSpecialType.Door,
                Game = GameType.AllDoom
            });

            // Damage + End (sector 11 has dual behavior: damages and can end level)
            Register(new SectorSpecialDefinition
            {
                Special = 11,
                Name = "Damage -10/20% health",
                Description = "Damages player 20%. In original DOOM, also ends level at 11% health or less.",
                Type = SectorSpecialType.Damage,
                DamageAmount = 20,
                DamageInterval = 32,
                Game = GameType.AllDoom
            });

            // Sync blink
            Register(new SectorSpecialDefinition
            {
                Special = 12,
                Name = "Light Blink (sync, 0.5 sec)",
                Type = SectorSpecialType.Light,
                LightEffect = LightEffect.BlinkHalf,
                Game = GameType.AllDoom
            });
            Register(new SectorSpecialDefinition
            {
                Special = 13,
                Name = "Light Blink (sync, 1 sec)",
                Type = SectorSpecialType.Light,
                LightEffect = LightEffect.BlinkFull,
                Game = GameType.AllDoom
            });

            // Door open
            Register(new SectorSpecialDefinition
            {
                Special = 14,
                Name = "Door Open (300 sec)",
                Description = "300 seconds after level start, ceiling opens",
                Type = SectorSpecialType.Door,
                Game = GameType.AllDoom
            });

            // Super damage
            Register(new SectorSpecialDefinition
            {
                Special = 16,
                Name = "Damage -10/20% health",
                Description = "Super nukage damage",
                Type = SectorSpecialType.Damage,
                DamageAmount = 20,
                DamageInterval = 32,
                Game = GameType.AllDoom
            });

            // Fire flicker
            Register(new SectorSpecialDefinition
            {
                Special = 17,
                Name = "Light Flickers (fire)",
                Type = SectorSpecialType.Light,
                LightEffect = LightEffect.FlickerFire,
                Game = GameType.AllDoom
            });
        }

        private static void RegisterBoomSectorSpecials()
        {
            // Boom generalized sector specials - stub for future expansion
        }
    }
}
