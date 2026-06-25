using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch]
public class HideDetails
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), "UpdateEnergyCostVisuals")]
    static void MakeEnergyInvisible(ref NCard __instance, ref TextureRect ____energyIcon)
    {
        if (!MyModConfig.HideEnergy) return;
        if (MyModConfig.ExcludeFranticEscape && __instance.Model is FranticEscape) return;
        ____energyIcon.Visible = false;
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), "UpdateStarCostVisuals")]
    static void MakeStarsInvisible(ref TextureRect ____starIcon)
    {
        if (!MyModConfig.HideStars) return;
        ____starIcon.Visible = false;
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardModel), "HoverTips", MethodType.Getter)]
    // [HarmonyPatch(typeof(CardModel), "ExtraHoverTips", MethodType.Getter)]
    static void RemoveHoverTooltips(ref IEnumerable<IHoverTip> __result) => __result = [];


    [HarmonyPrefix]
    [HarmonyPatch(typeof(NCard), "ActivateRewardScreenGlow")]
    static bool RemoveRewardCardRarityGlow()
    {
        if (MyModConfig.HideCardRewardRarityGlow) return false;
        return true;
    }

    // TODO: make it a class and include hiding the description background
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), "Reload")]
    static void HideDescription(MegaLabel? ____descriptionLabel)
    {
        if (____descriptionLabel == null) return;
        if (MyModConfig.HideDescription) ____descriptionLabel.Visible = false;
    }


    [HarmonyPatch(typeof(CardModel), "Title", MethodType.Getter)]
    public class CardTitleIntercept
    {
        [HarmonyPostfix]
        static void Intercept(ref CardModel? __instance, ref String __result)
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