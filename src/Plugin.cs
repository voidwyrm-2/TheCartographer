using System;
using BepInEx;
using UnityEngine;
using ImprovedInput;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using SlugBase.SaveData;

namespace TheCartographer;

//[BepInDependency("fisobs", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("improved-input-config", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("slime-cubed.slugbase", BepInDependency.DependencyFlags.HardDependency)]
[BepInPlugin("nc.TheCartographer", "The Cartographer", "0.1.0")]
class Plugin : BaseUnityPlugin
{
    /* from Pocky(Pocky is great <-- NO POCKY IS *NOT* GREAT, HE'S BRITISH):
    Basically
    check if grasp 0 has a rock and grasp 1 has a rock and return true if that happens
    And then hook to SpitUpCraftedObject
    and run the same grasp check again
    If it's true
    Spawn a spear and make the scug grab it
    then call return if you did this before orig so it doesn't try any more crafts
    */

    //public static readonly GameFeature<float> ScaredCentis = GameFloat("Cartocarto/scared_centis");

    public static BepInEx.Logging.ManualLogSource Beplogger;

    //public static string dstr = "";

    public static bool AtePuff = false;
    public static bool AteWeed = false;

    public static PlayerKeybind CraftSpear;

    public static bool ImprovedInputEnabled = false;

    public static bool isInit = false;

    //public void BIXLog(string info) => Logger.LogDebug(info);

    public void OnEnable()
    {
        Beplogger = Logger;

        try
        {
            CraftSpear = PlayerKeybind.Register("Cartocarto:craftkey", "The Cartographer", "Craft Spear", KeyCode.C, KeyCode.JoystickButton3);
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }

        Hooks.ApplyHooks();

        On.Player.Update += Player_LogStuff; // logs certain values

        On.RainWorld.PostModsInit += RainWorld_PostModsInit;
        On.RainWorld.OnModsInit += RainWorld_OnModsInit;

        // IL.Menu.IntroRoll.ctor += IntroRoll_ctor; // allows for multiple(or at least two) title cards, courtesy of OlayColay(Vinki's creator), thanks OlayColay!

        On.RainWorld.OnModsInit += RainWorld_LoadOptions;
    }

    private void RainWorld_LoadOptions(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);

        Logger.LogInfo("Loading remix options...");
        MachineConnector.SetRegisteredOI("nc.TheCartographer", new Options());
        Logger.LogInfo("Loading complete!");
    }

    private static void IntroRoll_ctor(ILContext il)
    {
        var cursor = new ILCursor(il);

        if (cursor.TryGotoNext(i => i.MatchLdstr("Intro_Roll_C_"))
            && cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<string>(nameof(string.Concat))))
        {
            cursor.Emit(OpCodes.Ldloc_3);
            cursor.EmitDelegate<Func<string, string[], string>>((titleImage, oldTitleImages) =>
            {
                titleImage = (UnityEngine.Random.value < 1f) ? "TitleCard_CartoCarto" : "TitleCard_CartoCarto-S";
                return titleImage;
            });
        }
    }

    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        try
        {
            if (!isInit)
            {
                isInit = true;
                CraftSpear.Description = "The key held to craft a spear.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
        finally
        {
            orig.Invoke(self);
        }
    }

    private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
    {
        ImprovedInputEnabled = ModManager.ActiveMods.Exists((ModManager.Mod mod) => mod.id == "improved-input-config");
        orig(self);
    }

    private static readonly bool LogMushrooms = false;
    private static readonly bool LogKeys = false;
    private static readonly bool LogDrugs = false;
    private void Player_LogStuff(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        //Logger.LogDebug($"from Player_GraspsCanBeCrafted: dstr: '{"dstr"}', scugIsCarto: {General.scugIsCarto}, usingCustomCraftKey: {General.usingCustomCraftKey}, craftStatus: {General.isCraftSpearPressed}, {General.craftsNotNull}, A({self?.grasps[0]?.grabbed?.abstractPhysicalObject?.type}, {self?.grasps[0]?.grabbed?.abstractPhysicalObject?.type == AbstractPhysicalObject.AbstractObjectType.Rock}), B({self?.grasps[1]?.grabbed?.abstractPhysicalObject?.type}, {self?.grasps[1]?.grabbed?.abstractPhysicalObject?.type == AbstractPhysicalObject.AbstractObjectType.Rock})");

        if (General.scugIsCarto)
        {
            //Logger.LogDebug($"holding SpearCraftControl? {self.IsPressed(CraftSpear)}({self.IsPressed(CraftSpear)})");

            Logger.LogDebug($"{Crafting.IsCraftSpearPressed(self)}, {self.CraftingResults() != null}, {self.IsPressed(CraftSpear)}");

            //Logger.LogDebug($"IsCraftSpearPressed: {CartoCrafting.IsCraftSpearPressed(self)}, CraftingResults: {self.CraftingResults()}({self.CraftingResults() != null})");

            if (LogMushrooms)
            {
                Logger.LogDebug("mushroomEffect:" + self.mushroomEffect.ToString());
                Logger.LogDebug("mushroomCounter:" + self.mushroomCounter.ToString());
            }

            if (LogKeys)
            {
                Logger.LogDebug($"CraftSpearkey: {self.IsPressed(CraftSpear)}, CraftSpearControls: {self.input[0].y == 1 && self.input[0].pckp}");
            }

            if (LogDrugs)
            {
                Logger.LogDebug("AtePuff:" + AtePuff.ToString());
                Logger.LogDebug("AteWeed:" + AteWeed.ToString());
            }
        }
    }
}
