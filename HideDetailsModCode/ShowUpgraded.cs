using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
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
        if (!MyModConfig.HideTitle) return;
        var model = __instance?.Model;
        if (model == null || ____titleLabel == null) return;
        try
        {
            if (!model.IsUpgraded) return;
            var text = model.CurrentUpgradeLevel == 1 ? "+" : $"+{model.CurrentUpgradeLevel}";
            ____titleLabel.SetTextAutoSize(text);
            ____titleLabel.Visible = true;
        }
        catch (Exception err)
        {
            Log.Error($"ShowUpgradedText: {err}");
        }
    }
}