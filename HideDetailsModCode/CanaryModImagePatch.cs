using MegaCrit.Sts2.Core.Modding;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using MegaCrit.Sts2.Core.Assets;
// using MegaCrit.Sts2.Core.Assets;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch(typeof(NModInfoContainer), nameof(NModInfoContainer.Fill))]
class CanaryModImagePatch
{
    public static void PostFix(NModInfoContainer __instance, Mod mod)
    {
        if (mod.manifest?.id != "HideDetailsMod") return;
        if (!(mod.manifest?.name ?? "").Contains("(Canary)")) return;
        string path = "res://HideDetailsMod/mod_image_canary.png";
        if (ResourceLoader.Exists(path))
        {
            __instance._image.Texture = PreloadManager.Cache.GetAsset<Texture2D>(path);
        }
    }
}