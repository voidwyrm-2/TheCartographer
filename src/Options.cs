using Menu.Remix.MixedUI;
using UnityEngine;
using Menu;

namespace TheCartographer;

sealed class Options : OptionInterface
{
    //taken from https://github.com/Dual-Iron/no-damage-rng/blob/master/src/Plugin.cs
    //thanks dual, you're a life saver

    public static Configurable<bool> UseIICC;

    public Options()
    {
        UseIICC = config.Bind("nc_UseImprovedInputConfigControls", false);
    }

    public override void Initialize()
    {
        base.Initialize();

        Tabs = new OpTab[] { new(this) };

        var labelTitle = new OpLabel(20, 600 - 30, "The Cartographer Options", true);

        var top = 200;
        var labelUseIICC = new OpLabel(new(100, 600 - top), Vector2.zero, "Use Improved Input Config for spear crafting?", FLabelAlignment.Left);
        var checkUseIICC = new OpCheckBox(UseIICC, new Vector2(20, 600 - top - 6))
        {
            description = "Enables or disables the use of Improved Input Config for The Cartographer's spear crafting button",
        };

        Tabs[0].AddItems(
            labelTitle,
            labelUseIICC,
            checkUseIICC
        );
    }
}
