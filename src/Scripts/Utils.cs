using System.Collections.Generic;
using System.Linq;
using RWCustom;
using SlugBase.SaveData;


namespace TheCartographer
{
    public static class Utils
    {
        public static RainWorld RainWorld => Custom.rainWorld;
        public static Dictionary<string, FShader> Shaders => RainWorld.Shaders;
        public static InGameTranslator Translator => RainWorld.inGameTranslator;
        //public static SaveMiscProgression GetMiscProgression() => RainWorld.GetMiscProgression();

        public static bool IsPebbles(this SSOracleBehavior behavior) => behavior?.oracle?.IsPebbles() ?? false;
        public static bool IsMoon(this SLOracleBehavior behavior) => behavior?.oracle?.IsMoon() ?? false;

        public static bool IsPebbles(this Oracle oracle) => oracle?.ID == Oracle.OracleID.SS;
        public static bool IsMoon(this Oracle oracle) => oracle?.ID == Oracle.OracleID.SL;

        public static bool IsDrugged(this Player player) => player?.mushroomCounter > 0f;

        public static void AddTextPrompt(this RainWorldGame game, string text, int wait, int time, bool darken = false, bool? hideHud = null)
        {
            hideHud ??= ModManager.MMF;
            game.cameras.First().hud.textPrompt.AddMessage(Translator.Translate(text), wait, time, darken, (bool)hideHud);
        }
    }
}