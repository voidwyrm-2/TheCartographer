using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheCartographer;

public static class Hooks
{
    public static void ApplyHooks()
    {
        Crafting.ApplyCartoCraftingHooks();
        Drugs.ApplyCartoDRUGSHooks();
        Gameplay.ApplyCartoGameplayHooks();
        General.ApplyGeneralPlayerHooks();
        Oracles.ApplyOracleHooks();
    }
}
