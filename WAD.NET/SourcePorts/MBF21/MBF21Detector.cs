using System.Text;
using WAD.NET.Archives;

namespace WAD.NET.SourcePorts.MBF21
{
    /// <summary>
    /// Detects MBF21-specific features in WAD archives.
    /// </summary>
    public static class MBF21Detector
    {
        /// <summary>
        /// Checks if a DEHACKED lump contains MBF21 features.
        /// </summary>
        /// <param name="dehContent">The DEHACKED file content as a string.</param>
        /// <returns>True if MBF21 features are detected.</returns>
        public static bool ContainsMBF21Features(string dehContent)
        {
            if (string.IsNullOrEmpty(dehContent))
                return false;

            // Check for explicit MBF21 indicators
            if (dehContent.Contains("MBF21 Bits") ||
                dehContent.Contains("Infighting group") ||
                dehContent.Contains("Projectile group") ||
                dehContent.Contains("Splash group") ||
                dehContent.Contains("Melee range") ||
                dehContent.Contains("Rip sound"))
            {
                return true;
            }

            // Check for MBF21 codepointers
            string[] mbf21Codepointers = new[]
            {
                "A_SpawnObject",
                "A_MonsterProjectile",
                "A_MonsterBulletAttack",
                "A_MonsterMeleeAttack",
                "A_RadiusDamage",
                "A_NoiseAlert",
                "A_HealChase",
                "A_SeekTracer",
                "A_FindTracer",
                "A_ClearTracer",
                "A_JumpIfHealthBelow",
                "A_JumpIfTargetInSight",
                "A_JumpIfTargetCloser",
                "A_JumpIfTracerInSight",
                "A_JumpIfTracerCloser",
                "A_JumpIfFlagsSet",
                "A_AddFlags",
                "A_RemoveFlags",
                "A_WeaponProjectile",
                "A_WeaponBulletAttack",
                "A_WeaponMeleeAttack",
                "A_WeaponSound",
                "A_WeaponJump",
                "A_ConsumeAmmo",
                "A_CheckAmmo",
                "A_RefireTo",
                "A_GunFlashTo",
                "A_WeaponAlert"
            };

            foreach (var codepointer in mbf21Codepointers)
            {
                if (dehContent.Contains(codepointer))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if an archive uses MBF21 features.
        /// </summary>
        /// <param name="archive">The archive to check.</param>
        /// <returns>True if MBF21 features are detected.</returns>
        public static bool UsesMBF21(IArchiveReader archive)
        {
            // Check for DEHACKED lump
            var dehEntry = archive.GetEntry("DEHACKED");
            if (dehEntry == null)
                return false;

            var dehData = archive.ReadLump(dehEntry);
            var dehContent = Encoding.ASCII.GetString(dehData);

            return ContainsMBF21Features(dehContent);
        }
    }
}
