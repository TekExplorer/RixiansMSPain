using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;

namespace HideDetailsMod.HideDetailsModCode.Patches;
// TODO: rename file
[HarmonyPatch]
static class CharacterSelectPatch
{
    public static bool Enabled => MainFile.NecrobinderSetIsPublic;
    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectBg), MethodType.Getter)]
    static class CharacterSelectBgPatch
    {
        // scene
        //    public string CharacterSelectBg => SceneHelper.GetScenePath("screens/char_select/char_select_bg_" + base.Id.Entry.ToLowerInvariant());
        static internal bool Prefix(CharacterModel __instance, ref string __result)
        {
            if (!Enabled) return true;
            if (!MainFile.DefectSetActive && __instance is Defect) return true;
            if (!MainFile.IroncladSetActive && __instance is Ironclad) return true;
            var customScene = "res://HideDetailsMod/scenes/character_select/" + __instance.Id.Entry.ToLowerInvariant() + "_bg.tscn"; ;
            if (!ResourceLoader.Exists(customScene)) return true;
            __result = customScene;
            return false;
        }
    }

    [HarmonyPatch(typeof(CharacterModel), "CharacterSelectIconPath", MethodType.Getter)]
    static class CharacterSelectIconPatch
    {
        static internal bool Prefix(CharacterModel __instance, ref string __result)
        {
            if (!Enabled) return true;
            if (!MainFile.DefectSetActive && __instance is Defect) return true;
            if (!MainFile.IroncladSetActive && __instance is Ironclad) return true;
            var filename = __instance.Id.Entry.ToLowerInvariant() + "_icon.png";
            var CharacterSelectIconPath = "res://HideDetailsMod/images/character_select/" + filename;
            if (!ResourceLoader.Exists(CharacterSelectIconPath)) return true;
            __result = CharacterSelectIconPath;
            return false;
        }
    }

    // [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectIcon), MethodType.Getter)]
    // static class CharacterSelectIconPatch
    // {
    //     static internal bool Prefix(CharacterModel __instance, ref CompressedTexture2D __result)
    //     {
    //         if (!MyModConfig.UseCustomCharacterSelect) return true;
    //         var filename = __instance.Id.Entry.ToLowerInvariant() + "_icon.png";
    //         var CharacterSelectIconPath = "res://HideDetailsMod/images/character_select/" + filename;
    //         if (!ResourceLoader.Exists(CharacterSelectIconPath)) return true;
    //         CompressedTexture2D CharacterSelectIcon = ResourceLoader.Load<CompressedTexture2D>(CharacterSelectIconPath);
    //         __result = CharacterSelectIcon;
    //         return false;
    //     }
    // }
    // [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectLockedIcon), MethodType.Getter)]
    // static class CharacterSelectLockedIconPatch
    // {
    //     static internal bool Prefix(CharacterModel __instance, ref CompressedTexture2D __result)
    //     {
    //         if (!MyModConfig.UseCustomCharacterSelect) return true;
    //         var CharacterSelectIconPath = "res://HideDetailsMod/images/character_select/" + __instance.Id.Entry.ToLowerInvariant() + "/locked_icon.png"; ;
    //         if (!ResourceLoader.Exists(CharacterSelectIconPath)) return true;
    //         CompressedTexture2D CharacterSelectIcon = ResourceLoader.Load<CompressedTexture2D>(CharacterSelectIconPath);
    //         __result = CharacterSelectIcon;
    //         return false;
    //     }
    // }
    // protected virtual string CharacterSelectIconPath => ImageHelper.GetImagePath("packed/character_select/char_select_" + base.Id.Entry.ToLowerInvariant() + ".png");
    // public CompressedTexture2D CharacterSelectIcon => ResourceLoader.Load<CompressedTexture2D>(CharacterSelectIconPath, null, ResourceLoader.CacheMode.Reuse);

    // protected virtual string CharacterSelectLockedIconPath => ImageHelper.GetImagePath("packed/character_select/char_select_" + base.Id.Entry.ToLowerInvariant() + "_locked.png");
    // public CompressedTexture2D CharacterSelectLockedIcon => ResourceLoader.Load<CompressedTexture2D>(CharacterSelectLockedIconPath, null, ResourceLoader.CacheMode.Reuse);

    //  public string CharacterSelectTransitionPath => "res://materials/transitions/" + base.Id.Entry.ToLowerInvariant() + "_transition_mat.tres";

    // protected virtual string MapMarkerPath => ImageHelper.GetImagePath("packed/map/icons/map_marker_" + base.Id.Entry.ToLowerInvariant() + ".png");
    // public CompressedTexture2D MapMarker => PreloadManager.Cache.GetCompressedTexture2D(MapMarkerPath);

    // public NCreatureVisuals CreateVisuals()
    // {
    //     return PreloadManager.Cache.GetScene(VisualsPath).Instantiate<NCreatureVisuals>();
    // }
}