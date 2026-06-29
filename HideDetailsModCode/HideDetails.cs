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
    [HarmonyPatch(typeof(CardModel), "HoverTips", MethodType.Getter)]
    // [HarmonyPatch(typeof(CardModel), "ExtraHoverTips", MethodType.Getter)]
    private static void RemoveHoverTooltips(CardModel? __instance, ref IEnumerable<IHoverTip> __result)
    {
        if (__instance == null) return;
        List<IHoverTip> tips = [];
        // tips.AddItem(new HoverTip(new LocString("credits", ".title")));
        if (MyModConfig.ShowCreditsTooltip)
        {
            var credit = AlternateArtsCredits.CreditFor(__instance);
            if (credit != null)
            {
                // Figure out localization stuff.
                // tips.Add(new HoverTip(new LocString("settings_ui", "HIDEDETAILSMOD-CREDITS.title"), $"Art by {credit}"));
                HoverTip item = new(
                        // new LocString("settings_ui", "HIDEDETAILSMOD-CREDITS.title"),
                        new LocString("artists", AlternateArtsCredits.KeyFor(__instance))
                    )
                    { IsDebuff = true };
                tips.Add(item);
            }
        }

        if (!MyModConfig.HideTooltips) tips.AddRange(__result);
        __result = tips;
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