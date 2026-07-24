using BaseLib.Config;
using BaseLib.Utils;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;
using LogType = MegaCrit.Sts2.Core.Logging.LogType;
using MegaCrit.Sts2.Core.Modding;
using Godot;
using HarmonyLib;
using BaseLib.Extensions;
using System.Reflection;
using BaseLib.Audio;

namespace HideDetailsMod.HideDetailsModCode;

//You're recommended but not required to keep all your code in this package and all your assets in the HideDetailsMod folder.
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "HideDetailsMod"; //At the moment, this is used only for the Logger and harmony names.

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static AutoModAudio Audio { get; } = new("res://HideDetailsMod/audio");

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        ModConfigRegistry.Register(ModId, new MyModConfig());
        CustomLocTableManager.Register("usernames");
        CustomLocTableManager.Register("artists");
        CustomLocTableManager.Register("event_chatter");
        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly);

        Harmony harmony = new(ModId);

        harmony.TryPatchAll(assembly);
        AlternateArts.InitCheck();
    }

#if CANARY
    public static bool IsCanary => true;
#else
    public static bool IsCanary => false;
#endif
}