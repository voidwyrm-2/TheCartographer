using SlugBase.SaveData;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using ImprovedInput;

namespace TheCartographer
{
    public class General
    {
        public static readonly SlugcatStats.Name CartoScug = new("nc.cartographer");
        public static readonly int CartoMaxFood = 10;

        public static bool gameIsCartos = false;
        public static bool scugIsCarto = false;
        public static bool usingCustomCraftKey = false;
        public static bool isCraftSpearPressed = false;
        public static bool craftsNotNull = false;
        public static bool IsOnDrugs;

        public static ConditionalWeakTable<Player, Dictionary<string, int>> drugCounters = new();

        public static ConditionalWeakTable<Spear, GenClasses.SpearStats> cartoSpears = new();

        private static bool VerifyCounterDict(Dictionary<string, int> Dict)
        {
            int neededCount = 3;
            if (Dict.Count < neededCount)
            {
                return false;
            }
            else if (neededCount > 3)
            {
                throw new Exception("idiot you forgot to change neededCount in VerifyCounterDict");
            }
            bool result = true;
            result = Dict.TryGetValue("MushroomCounter", out int _) && result;
            result = Dict.TryGetValue("WeedCounter", out int _) && result;
            result = Dict.TryGetValue("PuffCounter", out int _) && result;
            return result;
        }

        public static void ApplyGeneralPlayerHooks()
        {
            On.Player.ctor += Player_ctor; // does stuff at player creation
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            Plugin.Beplogger.LogInfo("beginning player ctor init");

            usingCustomCraftKey = CustomInputExt.IsKeyBound(self, Plugin.CraftSpear) && Options.UseIICC.Value && Plugin.ImprovedInputEnabled;
            Plugin.Beplogger.LogInfo($"usingCustomCraftKey is: {usingCustomCraftKey}");

            if (self.SlugCatClass == CartoScug)
                scugIsCarto = true;

            if (self.room != null)
            {
                if (self.room.game.session is StoryGameSession)
                {
                    if ((self.room.game.session as StoryGameSession).saveStateNumber == CartoScug && self.abstractCreature.world.game.StoryCharacter is not null)
                        gameIsCartos = true;
                }
            }
            Plugin.Beplogger.LogInfo($"scugIsCarto and gameIsCartos are: {scugIsCarto} and {gameIsCartos}");

            if (!scugIsCarto) return;
            Plugin.Beplogger.LogInfo("current scug is carto, continuing with drugCounters init");

            bool gotDrugCounters = drugCounters.TryGetValue(self, out Dictionary<string, int> drugCountersDict);
            if (!gotDrugCounters)
            {
                Plugin.Beplogger.LogDebug("drugCounters value does not exist, creating a new one...");

                Dictionary<string, int> tempCounters = new() { { "MushroomCounter", 0 }, { "WeedCounter", 0 }, { "PuffCounter", 0 } };
                drugCounters.Add(self, tempCounters);

                bool gotDrugCounters2 = drugCounters.TryGetValue(self, out drugCountersDict);
                if (!gotDrugCounters2)
                    throw new Exception("Hi this is from the Cartographer mod, if you're seeing this then something screwed up so ping me in the RW server with this info:\n'line 158, from bool gotDrugCounters2'");
            }

            if (!VerifyCounterDict(drugCountersDict))
                Plugin.Beplogger.LogDebug("drugCounters verification failed");

            try
            {
                if (self.room?.game.session is StoryGameSession && scugIsCarto)
                {
                    Plugin.Beplogger.LogDebug("StoryGameSession=true and CartoScug=true, attmpting to get save data");
                    SlugBaseSaveData miscSave = SaveDataExtension.GetSlugBaseData((self.abstractCreature.world.game.session as StoryGameSession).saveState.deathPersistentSaveData);
                    Plugin.Beplogger.LogDebug("getting gotCartoDrugCounters...");

                    if (miscSave.TryGet("CartoDrugCounters", out ConditionalWeakTable<Player, Dictionary<string, int>> savedcdCounters))
                    {
                        Plugin.Beplogger.LogDebug("success, accessing...");
                        drugCounters = savedcdCounters;
                        Plugin.Beplogger.LogDebug("accessed");
                    }
                    else
                    {
                        Plugin.Beplogger.LogDebug("failed, creating new");
                        miscSave.Set("CartoDrugCounters", drugCounters);
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Beplogger.LogError($"Got exception while trying to load save data:\n{e}");
            }

            Plugin.Beplogger.LogInfo("finished player ctor init");
        }
    }
}
