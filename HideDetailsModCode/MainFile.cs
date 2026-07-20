using BaseLib.Config;
using BaseLib.Utils;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;
using LogType = MegaCrit.Sts2.Core.Logging.LogType;
using MegaCrit.Sts2.Core.Modding;
using Godot;
using HarmonyLib;
using BaseLib.Extensions;
using System.Reflection;
// using MegaCrit.Sts2.Core.Assets;

namespace HideDetailsMod.HideDetailsModCode;

//You're recommended but not required to keep all your code in this package and all your assets in the HideDetailsMod folder.
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "HideDetailsMod"; //At the moment, this is used only for the Logger and harmony names.

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        ModConfigRegistry.Register(ModId, new MyModConfig());
        CustomLocTableManager.Register("usernames");
        CustomLocTableManager.Register("artists");
        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        //Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        Harmony harmony = new(ModId);

        harmony.TryPatchAll(assembly);
        AlternateArts.InitCheck();
    }

    // public static void LoadAll()
    // {
    //     var altArts = AlternateArts.Arts
    //         .SelectMany(factory => factory.AllPathsAsImg)
    //         .ToList();

    //     foreach (var art in altArts)
    //     {
    //         if (art == null) return;
    //         if (art.Exists()) PreloadManager.Cache.GetTexture2D(art.PortraitPath);
    //         if (art.Upgraded().Exists()) PreloadManager.Cache.GetTexture2D(art.Upgraded().PortraitPath);
    //     }
    // }
}