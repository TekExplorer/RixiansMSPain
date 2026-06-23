using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

// ReSharper disable InconsistentNaming

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch(typeof(NCard), "UpdateTitleLabel")]
public class ShowUpgraded
{
    [HarmonyPostfix]
    static void ShowUpgradedText(
        ref NCard? __instance,
        ref MegaLabel? ____titleLabel
        // ref MegaLabel? ____descriptionLabel,
        // ref MegaLabel? ____typeLabel,
        // ref NinePatchRect? ____typePlaque
    )
    {
        var model = __instance?.Model;
        if (model == null || ____titleLabel == null) return;
        if (!MyModConfig.HideTitle) return;
        try
        {
            if (MyModConfig.ExcludeWither && model is Wither) ____titleLabel.Visible = true;
            
            if (!model.IsUpgraded) return;
            int upgradeLevel = model.CurrentUpgradeLevel;
            var text = upgradeLevel == 1 ? "+" : $"+{upgradeLevel}";
            
            ____titleLabel.SetTextAutoSize(text);
            ____titleLabel.Visible = true;
        }
        catch (Exception err)
        {
            Log.Error($"ShowUpgradedText: {err}");
        }
    }
}