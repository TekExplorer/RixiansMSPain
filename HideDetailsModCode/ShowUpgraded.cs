using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

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
        if (__instance == null) return;
        var Model = __instance.Model;
        if (Model == null) return;
        try
        {
            // if (____typePlaque != null) ____typePlaque.Visible = false;
            // if (____titleLabel != null) ____titleLabel.Visible = false;
            // if (____descriptionLabel != null) ____descriptionLabel.Visible = false;
            // if (____typeLabel != null) ____typeLabel.Visible = false;
            if (Model.IsUpgraded && ____titleLabel != null)
            {
                ____titleLabel.SetTextAutoSize("UPGRADED");
                ____titleLabel.Visible = true;
            }
        }
        catch (Exception err)
        {
            Log.Error($"Something broke 2 {err}");
        }
    }

    
}