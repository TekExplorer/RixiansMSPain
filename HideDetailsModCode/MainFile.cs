using BaseLib.Utils;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;
using LogType = MegaCrit.Sts2.Core.Logging.LogType;
using MegaCrit.Sts2.Core.Modding;
using Godot;
using HarmonyLib;
using BaseLib.Extensions;
using System.Reflection;
using BaseLib.Audio;
using MegaCrit.Sts2.Core.Debug;
using System.Runtime.Loader;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace HideDetailsMod.HideDetailsModCode;

//You're recommended but not required to keep all your code in this package and all your assets in the HideDetailsMod folder.
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public static Mod Mod => ModManager.Mods.FirstOrDefault(mod => mod.manifest?.id == ModId)!;
    public const string ModId = "HideDetailsMod"; //At the moment, this is used only for the Logger and harmony names.

    // TODO: remove
    public static bool NecrobinderSetIsPublic => true;

    public static Logger Logger { get; } = new(ModId, LogType.Generic);
    public static AutoModAudio Audio { get; } = new("res://HideDetailsMod/audio");
#if DEBUG 
    public static bool DefectSetActive => true;
    public static bool IroncladSetActive => true;
#elif CANARY
    public static bool DefectSetActive => Mod.version?.Minor >= 3;
    public static bool IroncladSetActive => Mod.version?.Minor >= 4;
#else 
    public static bool DefectSetActive => Mod.version?.Minor >= 4;
    public static bool IroncladSetActive => Mod.version?.Minor >= 5;
#endif 

    static public bool? IsV107
    {
        get
        {
            var version = ReleaseInfoManager.Instance.SemVer;
            if (version == null) return null;
            return version.Minor <= 107;
        }
    }

    public static void Initialize()
    {
        Preload.Start();

        var assembly = Assembly.GetExecutingAssembly();

        if (IsV107 ?? false)
        {
#nullable disable
            string modFolder = Path.GetDirectoryName(assembly.Location);
            string betaPackPath = Path.Combine(modFolder, "HideDetailsMod.Beta.betapack");

            if (File.Exists(betaPackPath))
            {
                var asm = AssemblyLoadContext.GetLoadContext(typeof(ModManager).Assembly).LoadFromAssemblyPath(betaPackPath);
                AccessTools.Method(typeof(ModManager), "AssociateAssemblyWithMod").Invoke(null, [ModId, asm]);
                // ModManager.AssociateAssemblyWithMod(ModId, asm);
            }
#nullable restore
        }
        MyModConfig.Init();
        CustomLocTableManager.Register("usernames");
        CustomLocTableManager.Register("artists");
        CustomLocTableManager.Register("event_chatter");
        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly);

        Harmony harmony = new(ModId);

        harmony.TryPatchAll(assembly);

        InitModCredits();

        Preload.Wait();
    }

    static void InitModCredits()
    {
        ModCredits.Register(ModId,
            new ModCredits.Section("TEAM", ModCredits.Layout.Roles),
            new ModCredits.Section("ARTISTS", ModCredits.Layout.Columns3)
        );
        TaskHelper.RunSafely(Prep());
        static async Task Prep()
        {
            await Task.Delay(1);
            var artistsTable = LocManager.Instance.GetTable("artists");
            var artistKeys = artistsTable.Keys.Where(k => !k.StartsWith('.') && k.Count(".") == 1);
            var artistUsernames = artistKeys.Select(artistsTable.GetRawText).ToHashSet().Select(usrname => LocString.GetIfExists("usernames", usrname)?.GetRawText() ?? usrname);

            var creditsDict = new Dictionary<string, string>
            {
                ["HIDEDETAILSMOD-HIDEDETAILSMOD.title"] = "Rixian's MSPain",

                ["HIDEDETAILSMOD-TEAM.header"] = "Team",
                ["HIDEDETAILSMOD-TEAM.names"] = string.Join('\n', [
                    "Certified Cat Boi||Rixian",
                    "Lead Developer||TekExplorer",
                    "Unpaid Intern||Fiddlah",
                    "Artist Wrangler||Helios",
                    "Art Collector||an_gun"
                ]),

                ["HIDEDETAILSMOD-ARTISTS.header"] = "Art",
                ["HIDEDETAILSMOD-ARTISTS.names"] = string.Join('\n', artistUsernames)
            };

            LocManager.Instance.GetTable("credits").MergeWith(creditsDict);
        }
    }

    public static bool IsCanary => IsActuallyCanary && MyModConfig.EmulateCanaryMode;

#if CANARY
    public static bool IsActuallyCanary => true;
#else
    public static bool IsActuallyCanary => false;
#endif
}