using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch]
internal class HideDetails
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), "UpdateEnergyCostVisuals")]
    private static void MakeEnergyInvisible(ref NCard __instance, ref TextureRect ____energyIcon)
    {
        if (!MyModConfig.HideEnergy) return;
        if (MyModConfig.ExcludeFranticEscape && __instance.Model is FranticEscape) return;
        ____energyIcon.Visible = false;
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), "UpdateStarCostVisuals")]
    private static void MakeStarsInvisible(ref TextureRect ____starIcon)
    {
        if (!MyModConfig.HideStars) return;
        ____starIcon.Visible = false;
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardModel), "ExtraHoverTips", MethodType.Getter)]
    private static void RemoveExtraHoverTooltips(ref IEnumerable<IHoverTip> __result)
    {
        if (MyModConfig.HideTooltips) __result = [];
    }

    internal static string CreditsKeyFor(CardModel card)
    {
        var pool = card.Pool.Title.ToLowerInvariant();
        var name = card.Id.Entry.ToLowerInvariant();
        return $"{pool}.{name}"; // "silent.predator"
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardModel), "HoverTips", MethodType.Getter)]
    // [HarmonyPatch(typeof(CardModel), "ExtraHoverTips", MethodType.Getter)]
    private static void RemoveHoverTooltips(CardModel? __instance, ref IEnumerable<IHoverTip> __result)
    {
        if (__instance == null) return;
        try
        {
            List<IHoverTip> tips = [];
            // tips.AddItem(new HoverTip(new LocString("credits", ".title")));
            if (MyModConfig.ShowCreditsTooltip)
            {
                var author = new LocString("artists", CreditsKeyFor(__instance));
                // var uploader = new LocString("artists", CreditsKeyFor(__instance) + ".uploader");
                if (author.Exists())
                {
                    var desc = new LocString("artists", ".description");
                    desc.Add("Artist", author);

                    tips.Add(new HoverTip(desc) { IsDebuff = true });
                    //

                    var overlayAuthor = new LocString("artists", CreditsKeyFor(__instance) + ".overlay");

                    if (overlayAuthor.Exists())
                    {
                        var overlayDesc = new LocString("artists", ".description.overlay");
                        overlayDesc.Add("Artist", overlayAuthor);
                        tips.Add(new HoverTip() { IsDebuff = true });
                    }
                }
            }

            if (!MyModConfig.HideTooltips) tips.AddRange(__result);
            __result = tips;
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"HoverTips Error: {e}");
        }
    }
// static string GetCardName(CardModel card, string variant = "")
// {
//     if (!variant.Equals("")) variant = "_" + variant;
//     return $"{card.Id.Entry.ToLowerInvariant()}{variant}";
// }
// static string GetAltImage(CardModel card, string variant = "")
// {
//     var img = $"atlases/alt_card_arts/{card.Pool.Title.ToLowerInvariant()}/{GetCardName(card, variant)}.tres";
//     return ImageHelper.GetImagePath(img);
// }

// static bool FileExists(string filePath)
// {
// ResourceLoader.Exists
//     return File.Exists(filePath);
// }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NCard), "ActivateRewardScreenGlow")]
    private static bool RemoveRewardCardRarityGlow()
    {
        if (MyModConfig.HideCardRewardRarityGlow) return false;
        return true;
    }

// TODO: make it a class and include hiding the description background
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), "Reload")]
    private static void HideDescription(MegaLabel? ____descriptionLabel)
    {
        if (____descriptionLabel == null) return;
        if (MyModConfig.HideDescription) ____descriptionLabel.Visible = false;
    }

    [HarmonyPatch(typeof(CardModel), "Title", MethodType.Getter)]
    public class CardTitleIntercept
    {
        [HarmonyPostfix]
        private static void Intercept(ref CardModel? __instance, ref String __result)
        {
            if (__instance == null) return;
            if (!MyModConfig.HideTitle) return;
            __result = _FormattedText(__instance);
        }

        private static string _FormattedText(CardModel card)
        {
            if (!card.IsUpgraded) return "";
            if (card.MaxUpgradeLevel <= 1) return "+";
            return $"+{card.CurrentUpgradeLevel}";
        }
    }
}