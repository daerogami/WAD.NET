using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WAD.NET.Concrete
{
    public static class ConfigUtilities
    {
        public static string ConvertZandronumJoinStringToConfigFile(string joinString, string configName)
        {
            var zandronumExecutablePath = GetExecutablePath(joinString);
            var iwad = GetIwad(joinString);
            var files = GetFiles(joinString);
            // HIGH: Move to resource file and define object around this file (or lookup Doomseeker's class)
            return $@"
[%General]
engine=Zandronum
executable={zandronumExecutablePath}
name={(string.IsNullOrWhiteSpace(configName) ? GetNewConfigFileName() : configName)}
port=10666
gamemode=0
map=
iwad={iwad}
pwads=""{string.Join(";", files)}""
pwadsOptional = ""0;0;0;0;0;0;0;0;0;0;0;0;0""
broadcastToLAN = 0
broadcastToMaster = 0
upnp = 0
upnpPort = 0

[Rules]
difficulty=3
modifier=0
maxClients=1
maxPlayers=1
%2Bsv_maxlives=0
maplist=
randomMapRotation=0

[Misc]
URL=
eMail=
connectPassword=
joinPassword=
RConPassword=
MOTD=
CustomParams=

[dmflags]
CompatUseOriginalMissileClippingHeight=0
CompatUseSectorBasedSoundTargetCode=0
CompatTraceIgnoreLinesWithoutSameSectorOnBothSides=0
CompatLimitDehMaxHealthToHealthBonus=0
CompatRavensScrollersUseOriginalSpeed=0
CompatAddNOGRAVITYFlagToSpheres=0
DontStopPlayerScriptsOnDisconnect=0
OldZDoomHorizontalThrust=0
OldZDoomBridgeDrops=0
OldZDoomJumpPhysics=0
CompatUseVanillaAutoaimTracerBehavior=0
CompatMaskedMidtex=0
CompatBadAngles=0
CompatFindShortestTexturesLikeDoom=0
CompatLimitPainElementals=0
CompatSpawnItemDropsOnTheFloor=0
CompatNETScriptsAreClientside=0
CompatActorsAreInfinitelyTall=0
CompatUseBuggierStairBuilding=0
CompatDisableBoomDoorLightEffect=0
CompatAllSpecialLinesCanDropUseLines=0
CompatOriginalSoundCurve=0
CompatFullWeaponLower=0
CompatWestSpawnsAreSilent=0
CompatFloorMove=0
CompatEnableWallRunning=0
CompatDontLetOthersHearPickups=0
CompatAllowInstantRespawn=0
CompatDisableStealthMonsters=0
CompatAllowSilentBFGTrick=0
CompatOriginalWeaponSwitch=0
CompatLimitedMovementInTheAir=0
CompatMonstersSeeSemiInvisiblePlayers=0
CompatPlasmaBumpBug=0
CompatAnyBossDeathActivatesMapSpecials=0
CompatFrictionPushersPullersAffectMonsters=0
CompatCrusherGibsByMorphingNotReplacement=0
CompatBlockMonsterLinesIgnoreFriendlyMonsters=0
CompatFindNeighboringLightLevelLikeDoom=0
CompatUseOldIntermissionScreensMusic=0
CompatScrollingSectorsAreAdditive=0
CompatSectorSoundsUseOriginalMethod=0
CompatNoMonstersDropoffMove=0
CompatInstantlyMovingFloorsArentSilent=0
CompatClientsSendFullButtonInfo=0
CompatOldRandomNumberGenerator=0
CompatMonstersCantBePushedOffCliffs=0
CompatOldDamageRadiusInfiniteHeight=0
CompatMinotaur=0
CompatOriginalVelocityCalcForMushroomInDehacked=0
CompatSpriteSortOrderInverted=0
CompatHitscansOriginalBlockmap=0
CompatDrawPolyobjectsOld=0
LMSChainsaw=0
LMSPistol=0
LMSShotgun=0
LMSSuperShotgun=0
LMSChaingun=0
LMSMinigun=0
LMSRocketLauncher=0
LMSGrenadeLauncher=0
LMSPlasmaRifle=0
LMSRailgun=0
LMSSpectatorsCanTalkToActivePlayers=0
LMSSpectatorsCanViewTheGame=0
RespawnAutomatically=0
DropWeaponOnDeath=1
RespawnFarthestAwayFromOthers=0
LoseAFragOnDeath=0
RespawnWithAShotgun=0
NoRespawnProtection=0
KeepFragsAfterMapChange=0
WeaponsStayAfterPickup=1
DoubleAmmo=1
DontSpawnHealth=0
DontSpawnArmor=0
DontSpawnRunes=0
ScoreDamageNotKills=0
DontSpawnDeathmatchWeapons=1
DontSpawnAnyMultiplayerItem=0
MonstersAreFast=0
MonstersRespawn=0
MonstersMustBeKilledToExit=0
KillBossMonsters=1
RespawnWhereDied=0
LoseAllInventory=1
LoseArmor=0
LoseKeys=0
LosePowerups=0
LoseWeapons=1
LoseAllAmmo=0
LoseHalfAmmo=0
ShareKeys=1
SurvivalNoMapResetOnDeath=0
NoSuicide=1
NoRespawn=0
NoRocketJump=0
NoTaunt=0
NoItemDrop=0
NoUseAutomap=0
NoTurnOffTranslucency=0
NoUseCrosshairs=0
NoUseCustomGLLightingSettings=0
NoTargetIdentify=0
NoDisplayCoopInfo=0
NoUseAutoaim=0
NoUseFOV=0
NoUseFreelook=0
NoUseLandConsoleCommand=0
NoMaxBloodScalar=0
InfiniteInventory=0
InfiniteAmmo=0
SlowlyLoseHealthWhenOver100=0
CanUseChasecam=0
AllowBFGFreeaiming=1
DontCheckAmmoWhenSwitchingWeapons=0
NoMonsters=0
ItemsRespawn=0
BarrelsRespawn=0
MegaPowerupsRespawn=0
ServerPicksTeams=0
PlayersCantSwitchTeams=0
KeepTeamsAfterAMapChange=0
HideAlliesOnTheAutomap=0
DontLetPlayersSpyOnAllies=0
InstantFlagSkullReturn=0
NoUnlagged=0
AlwaysApplyLMSSpectatorSettings=0
NoMedals=0
gameversion=2
defaultdmflags=0
falling_damage_type=0
jump_ability=1
crouch_ability=1
player_block=2
level_exit=1
killmonsters_percentage=100
force_inactive_players_spectating_mins=0
monsters_damage_factor=@Variant(\0\0\0\x87?\x80\0\0)

[voting]
UseThisPage=0
WhoCanVote=0
MinimumPlayersRequiredToVote=1
VoteFloodingProtection=1
KickVote=1
ChangeMapVote=1
MapVote=1
TimeLimitVote=1
FragLimitVote=1
DuelLimitVote=1
PointLimitVote=1
WinLimitVote=1
ForceSpectatorVote=1";
        }

        private static string GetExecutablePath(string joinString)
        {
            var match = Regex.Match(joinString, @"^(.+?)\s");
            if (!match.Success)
            {
                return "";
            }
            return match.Groups[0].Captures[0].Value;
        }

        private static string GetNewConfigFileName()
        {
            return
                $"NewConfig_{Guid.NewGuid().ToString().Substring(0, 4)}_{new DateTime().ToString("yyyy_MM_dd_hh_mm_ss")}";
        }

        private static string GetIwad(string joinString)
        {
            var match = Regex.Match(joinString, @"-iwad (.+?)\s");
            if (!match.Success)
            {
                return "";
            }
            return match.Groups[1].Captures[0].Value;
        }

        private static List<string> GetFiles(string joinString)
        {
            var fileList = new List<string>();
            var matches = Regex.Matches(joinString, @"-file (.+?)\s");
            var thing = matches[0];
            foreach (Match match in matches)
            {
                fileList.Add(match.Groups[1].Captures[0].Value);
            }
            return fileList;
        }
    }
}