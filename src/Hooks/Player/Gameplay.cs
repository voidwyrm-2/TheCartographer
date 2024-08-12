using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;

namespace TheCartographer;

public class Gameplay
{
#pragma warning disable IDE0044
    static bool gameIsCartos = General.gameIsCartos;
    static bool scugIsCarto = General.scugIsCarto;
#pragma warning restore IDE0044

    static readonly float CentiScaredness = 1f;

    static bool EndedDrugtrip = false;

    public static void ApplyCartoGameplayHooks()
    {
        On.RoomSpecificScript.SU_A43SuperJumpOnly.ctor += RemoveSurvJumpTutorial;
        On.RoomSpecificScript.SU_C04StartUp.ctor += RemoveSurvFoodTutorial;

        On.CentipedeAI.IUseARelationshipTracker_UpdateDynamicRelationship += CentipedeAI_IUseARelationshipTracker_UpdateDynamicRelationship; // since Carto reeks of drugs(sporepuffs among them), he repels centipedes slightly

        On.Spear.PickedUp += Spear_RemoveDrugBuffs;

        On.Player.Update += Player_DruggieMode; // gives Carto stat boosts when under the effect of mushrooms

        On.Player.ThrownSpear += Player_ThrownSpear; // gives spears that Carto throws a boost
    }

    #region RemoveTutorials
    private static void RemoveSurvJumpTutorial(On.RoomSpecificScript.SU_A43SuperJumpOnly.orig_ctor orig, RoomSpecificScript.SU_A43SuperJumpOnly self)
    {
        if (gameIsCartos) return;
        orig(self);
    }

    private static void RemoveSurvFoodTutorial(On.RoomSpecificScript.SU_C04StartUp.orig_ctor orig, RoomSpecificScript.SU_C04StartUp self, Room room)
    {
        if (gameIsCartos) return;
        orig(self, room);
    }
    #endregion

    private static CreatureTemplate.Relationship CentipedeAI_IUseARelationshipTracker_UpdateDynamicRelationship(On.CentipedeAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, CentipedeAI self, RelationshipTracker.DynamicRelationship dRelation)
    {
        if (scugIsCarto)
        {
            dRelation.currentRelationship.type = CreatureTemplate.Relationship.Type.Afraid;
            dRelation.currentRelationship.intensity = CentiScaredness;
        }

        return orig(self, dRelation);
    }

    private static readonly bool logPlayerStats = true;
    private static bool canLogPlayerStatsOtherwise = false;
    private static void Player_DruggieMode(On.Player.orig_Update orig, Player self, bool eu)
    {
        scugIsCarto = General.scugIsCarto;
        gameIsCartos = General.gameIsCartos;
        if (scugIsCarto)
        {
            if (self.mushroomCounter != 0)
            {
                EndedDrugtrip = false;
                if (logPlayerStats)
                {
                    Plugin.Beplogger.LogDebug($"CartoSlug is under the influence of drugs, stats: " +
                    $"glowing={self.glowing}, " +
                    $"slugcatStats.throwingSkill={self.slugcatStats.throwingSkill}, " +
                    $"dynamicRunSpeed[0]=0;{self.dynamicRunSpeed[0]} 1;{self.dynamicRunSpeed[1]}, " +
                    $"playerState.permanentDamageTracking={self.playerState.permanentDamageTracking}, " +
                    $"Malnourished={self.Malnourished}"
                    );
                    canLogPlayerStatsOtherwise = true;
                }
                self.Blink(5);
                self.glowing = true;
                self.slugcatStats.throwingSkill = 5;
                self.dynamicRunSpeed[0] = 5f;
                self.dynamicRunSpeed[1] = 4.5f;
                self.HypothermiaGain = 0f;
                //self.mushroomEffect = 0.5f;
                self.Regurgitate();
                self.playerState.permanentDamageTracking = 0.0;
                if (self.Malnourished)
                {
                    self.SetMalnourished(false);
                }
            }
            else if (!EndedDrugtrip)
            {
                EndedDrugtrip = true;
                self.glowing = false;
                self.slugcatStats.throwingSkill = 1;
                if (canLogPlayerStatsOtherwise && logPlayerStats)
                {
                    Plugin.Beplogger.LogDebug($"CartoSlug is no longer under the influence of drugs, stats: " +
                    $"glowing={self.glowing}, " +
                    $"slugcatStats.throwingSkill={self.slugcatStats.throwingSkill}, " +
                    $"dynamicRunSpeed[0]=0;{self.dynamicRunSpeed[0]} 1;{self.dynamicRunSpeed[1]}, " +
                    $"playerState.permanentDamageTracking={self.playerState.permanentDamageTracking}, " +
                    $"Malnourished={self.Malnourished}"
                    );
                    canLogPlayerStatsOtherwise = false;
                }
            }
        }
        orig(self, eu);
    }

    private static void Spear_RemoveDrugBuffs(On.Spear.orig_PickedUp orig, Spear self, Creature upPicker)
    {
        Plugin.Beplogger.LogDebug("a spear was just picked up");
        if (General.cartoSpears.TryGetValue(self, out GenClasses.SpearStats spearStats))
        {
            Plugin.Beplogger.LogDebug("the spear has been effected by drugs, reseting stats...");

            try
            {
                spearStats.ApplyStats(self);
                Plugin.Beplogger.LogDebug("stats reset successfully");
            }
            catch (Exception e)
            {
                Plugin.Beplogger.LogError($"reset failed, got \"{e}\"");
            }

            Plugin.Beplogger.LogDebug("now removing spear from CWT...");

            try
            {
                General.cartoSpears.Remove(self);
                Plugin.Beplogger.LogDebug("spear was successfully removed");
            }
            catch (Exception e)
            {
                Plugin.Beplogger.LogError($"removal failed, got \"{e}\"");
            }
        }
        else
        {
            Plugin.Beplogger.LogDebug("the spear has NOT been effected by drugs, ignoring");
        }

        orig(self, upPicker);
    }

    private static readonly bool logSpearThrown = true;
    private static void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
    {
        if (scugIsCarto && self.mushroomEffect != 0f)
        {
            General.cartoSpears.Add(spear, new GenClasses.SpearStats(spear));
            if (logSpearThrown)
            {
                Plugin.Beplogger.LogDebug("Spear was thrown by CartoSlug with mushroom effect!");
                Plugin.Beplogger.LogDebug($"Before the boost its stats were: spearDamageBonus={spear.spearDamageBonus}, gravity={spear.gravity}, bodyChunks[0].vel.y={spear.bodyChunks[0].vel.y}");
            }

            if (self.mushroomEffect > 7.25f) spear.spearDamageBonus += 9f;
            else spear.spearDamageBonus += 5f;
            spear.gravity = 0.1f;
            spear.bodyChunks[0].vel.y *= 2.5f;

            if (logSpearThrown) Plugin.Beplogger.LogDebug($"Now its stats are: spearDamageBonus={spear.spearDamageBonus}, gravity={spear.gravity}, bodyChunks[0].vel.y={spear.bodyChunks[0].vel.y}");
        }

        orig(self, spear);
    }
}
