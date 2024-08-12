using ImprovedInput;
using System;
using BepInEx;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using SlugBase.SaveData;

namespace TheCartographer;

public static class Crafting
{
    static bool scugIsCarto = General.scugIsCarto;

    //static readonly BepInEx.Logging.ManualLogSource Logger = Plugin.Beplogger;

#pragma warning disable IDE0044
    static bool usingCustomCraftKey = General.usingCustomCraftKey;
#pragma warning restore IDE0044

    public static void ApplyCartoCraftingHooks()
    {
        On.Player.GraspsCanBeCrafted += Player_GraspsCanBeCrafted;
        On.Player.CraftingResults += Player_CraftingResults;
        On.RainWorld.PostModsInit += RainWorld_CraftingHooks;
        IL.Player.GrabUpdate += RemoveGrabButtonUse;
    }

    // fix for needing to hold the grab button when a Custom Input Config button is used
    private static void RemoveGrabButtonUse(ILContext il)
    {
        ILCursor c = new(il); // we create a new cursor that starts at IL_0
        if (scugIsCarto && usingCustomCraftKey && General.isCraftSpearPressed)
        {
            if (c.TryGotoNext(
                MoveType.After,
                x => x.MatchStloc(0),
                x => x.MatchLdcI4(0),
                x => x.MatchStloc(1)
                )) // we match the specific commands preceding IL_0137: ldarg.0
            {
                c.Index += 1; // we move forward to IL_0137: ldarg.0
                c.Next.OpCode = OpCodes.Ldc_I4_1; // we change IL_0138: ldc.i4.0 to ldc.i4.1; Player::craftingObject == false >>>> Player::craftingObject == true
            }

            if (c.TryGotoNext(
                MoveType.After,
                x => x.MatchStloc(2),
                x => x.MatchLdcI4(-1),
                x => x.MatchStloc(3)
                )) // we match the specific commands preceding IL_0142: ldarg.0
            {
                c.Next.OpCode = OpCodes.Ldc_I4_1; // we change IL_0142: ldc.i4.0 to ldc.i4.1; flag3 == false >>>> flag3 == true
            }

            // don't use anything below this line

            // var cursor = new ILCursor(il);

            /*
            cursor.GotoNext(MoveType.Before, instr => instr.MatchStfld("Player::craftingObject", "craftingObject"));
            cursor.Previous.Operand = 1;
            */

            /*
            cursor.Goto(0x0137);
            cursor.Remove();
            cursor.Emit(OpCodes.Ldc_I4_0);
            */
        }
    }

    public static bool IsCraftSpearPressed(Player player)
    {
        return usingCustomCraftKey ? player.IsPressed(Plugin.CraftSpear) : player.input[0].y == 1 && player.input[0].pckp;
    }

    #region BoringStuff
    private static void RainWorld_CraftingHooks(On.RainWorld.orig_PostModsInit orig, RainWorld self)
    {
        On.MoreSlugcats.GourmandCombos.CraftingResults += GourmandCombos_CraftingResults;
        orig(self);
    }

    private static bool Player_GraspsCanBeCrafted(On.Player.orig_GraspsCanBeCrafted orig, Player self)
    {
        General.isCraftSpearPressed = IsCraftSpearPressed(self);
        General.craftsNotNull = self.CraftingResults() != null;
        Plugin.Beplogger.LogDebug($"{IsCraftSpearPressed(self)}, {self.CraftingResults() != null}, {self.IsPressed(Plugin.CraftSpear)}");
        return scugIsCarto ? IsCraftSpearPressed(self) && self.CraftingResults() != null : orig(self);
    }

    private static AbstractPhysicalObject.AbstractObjectType Player_CraftingResults(On.Player.orig_CraftingResults orig, Player self)
    {
        scugIsCarto = General.scugIsCarto; // make sure the local scug check is up to date with the global scug check

        if (self.grasps.Length < 2 || !scugIsCarto) // We need to be holding at least two things
            return orig(self);

        var craftingResult = CartoCraft(self, self.grasps[0], self.grasps[1]);

        return craftingResult?.type;
    }

    private static AbstractPhysicalObject GourmandCombos_CraftingResults(On.MoreSlugcats.GourmandCombos.orig_CraftingResults orig, PhysicalObject crafter, Creature.Grasp graspA, Creature.Grasp graspB)
    {
        return scugIsCarto ? CartoCraft(crafter as Player, graspA, graspB) : orig(crafter, graspA, graspB);
    }
    #endregion

    public static AbstractPhysicalObject CartoCraft(Player player, Creature.Grasp graspA, Creature.Grasp graspB)
    {
        if (player == null || graspA?.grabbed == null || graspB?.grabbed == null || !scugIsCarto) return null;

        // Check grasps here
        AbstractPhysicalObject.AbstractObjectType grabbedObjectTypeA = graspA.grabbed.abstractPhysicalObject.type;
        AbstractPhysicalObject.AbstractObjectType grabbedObjectTypeB = graspB.grabbed.abstractPhysicalObject.type;

        #region Crafting

        #region Spears

        // normal
        if (grabbedObjectTypeA == AbstractPhysicalObject.AbstractObjectType.Rock && grabbedObjectTypeB == AbstractPhysicalObject.AbstractObjectType.Rock)
            return new AbstractSpear(player.room.world, null, player.abstractCreature.pos, player.room.game.GetNewID(), false, false);

        // explosive
        else if ((grabbedObjectTypeA == AbstractPhysicalObject.AbstractObjectType.FirecrackerPlant && grabbedObjectTypeB == AbstractPhysicalObject.AbstractObjectType.Spear) || (grabbedObjectTypeA == AbstractPhysicalObject.AbstractObjectType.Spear && grabbedObjectTypeB == AbstractPhysicalObject.AbstractObjectType.FirecrackerPlant))
            return new AbstractSpear(player.room.world, null, player.abstractCreature.pos, player.room.game.GetNewID(), true, false);

        // electric
        else if (((grabbedObjectTypeA == AbstractPhysicalObject.AbstractObjectType.Rock && grabbedObjectTypeB == AbstractPhysicalObject.AbstractObjectType.Spear) || (grabbedObjectTypeA == AbstractPhysicalObject.AbstractObjectType.Spear && grabbedObjectTypeB == AbstractPhysicalObject.AbstractObjectType.Rock)) && ModManager.MSC)
        {
            AbstractSpear electricSpear = new(player.room.world, null, player.abstractCreature.pos, player.room.game.GetNewID(), false, true)
            {
                electricCharge = 0
            };
            return electricSpear;
        }

        else if ((grabbedObjectTypeA == AbstractPhysicalObject.AbstractObjectType.Rock || grabbedObjectTypeB == AbstractPhysicalObject.AbstractObjectType.Rock) && player.mushroomCounter > 0)
        {
            player.mushroomCounter = 0;
            player.mushroomEffect = 0f;
            return new AbstractPhysicalObject(player.room.world, AbstractPhysicalObject.AbstractObjectType.PuffBall, null, player.abstractCreature.pos, player.room.game.GetNewID());
        }

        #endregion

        #endregion

        return null;
    }
}
