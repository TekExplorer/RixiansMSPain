using BaseLib.Config;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace HideDetailsMod.HideDetailsModCode;

//You're recommended but not required to keep all your code in this package and all your assets in the HideDetailsMod folder.
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "HideDetailsMod"; //At the moment, this is used only for the Logger and harmony names.

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        ModConfigRegistry.Register(ModId, new MyModConfig());
        CustomLocTableManager.Register("usernames");
        CustomLocTableManager.Register("artists");
        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        //Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        Harmony harmony = new(ModId);

        harmony.PatchAll();
    }

    // public static void Setup()
    // {
    //     var altArts = AlternateArts.Cards.Values
    //         .SelectMany(pair => pair.cardImgs)
    //         .Where(img => img != null) // Optional: Filters out any null values
    //         .ToList();

    //     foreach (var art in altArts)
    //     {
    //         if (art == null) return;
    //         PreloadManager.Cache.GetTexture2D(art.PortraitPath);
    //     }
    // }
}