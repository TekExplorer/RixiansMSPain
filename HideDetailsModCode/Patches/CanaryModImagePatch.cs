using MegaCrit.Sts2.Core.Modding;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using MegaCrit.Sts2.Core.Assets;
// using MegaCrit.Sts2.Core.Assets;

namespace HideDetailsMod.HideDetailsModCode.Patches;

[HarmonyPatch(typeof(NModInfoContainer), nameof(NModInfoContainer.Fill))]
class CanaryModImagePatch
{
    [HarmonyPostfix]
    public static void Postfix(NModInfoContainer __instance, Mod mod)
    {
        if (mod.manifest?.id != MainFile.ModId) return;
        if (!MainFile.IsActuallyCanary) return;
        string path = "res://HideDetailsMod/mod_image_canary.png";
        if (ResourceLoader.Exists(path))
        {
            __instance._image.Texture = PreloadManager.Cache.GetAsset<Texture2D>(path);
        }
    }
}