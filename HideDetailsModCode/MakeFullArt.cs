using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

// ReSharper disable InconsistentNaming

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch(typeof(NCard), "Reload")]
public class MakeFullArt
{
    [HarmonyPostfix]
    static void HideText(
        ref NCard? __instance,
        ref MegaLabel? ____titleLabel,
        ref MegaLabel? ____descriptionLabel,
        ref MegaLabel? ____typeLabel,
        ref NinePatchRect? ____typePlaque
    )
    {
        var model = __instance?.Model;
        if (model == null) return;
        try
        {
            if (____typePlaque != null) ____typePlaque.Visible = !MyModConfig.HideType;
            if (____typeLabel != null) ____typeLabel.Visible = !MyModConfig.HideType;

            if (MyModConfig.HideDescription && ____descriptionLabel != null)
            {
                ____descriptionLabel.Visible = false;
            }

            if (MyModConfig.HideTitle && ____titleLabel != null)
            {
                ____titleLabel.Visible = false;
                if (model.IsUpgraded)
                {
                    if (model.CurrentUpgradeLevel == 1)
                    {
                        ____titleLabel.SetTextAutoSize("+");
                    }
                    else
                    {
                        ____titleLabel.SetTextAutoSize($"+{model.CurrentUpgradeLevel}");
                    }

                    ____titleLabel.Visible = true;
                }
            }
        }
        catch (Exception err)
        {
            Log.Error($"Something broke 2 {err}");
        }
    }

    [HarmonyPostfix]
    static void UseFullArt(
        ref NCard? __instance,
        ref TextureRect? ____frame,
        ref TextureRect? ____ancientBorder,
        ref TextureRect? ____portraitBorder,
        ref TextureRect? ____portrait,
        ref TextureRect? ____ancientPortrait,
        ref TextureRect? ____banner,
        ref Material? ____canvasGroupMaskMaterial,
        ref CanvasGroup? ____portraitCanvasGroup,
        ref TextureRect? ____ancientTextBg,
        ref Control? ____ancientBanner,
        ref TextureRect? ____ancientBorderGlassOverlay
    )
    {
        CardModel? model = __instance?.Model;
        if (____frame == null || ____ancientBorder == null || ____portrait == null ||
            ____ancientPortrait == null || ____portraitBorder == null || ____banner == null ||
            ____portraitCanvasGroup == null || ____ancientTextBg == null || ____ancientBanner == null ||
            ____ancientBorderGlassOverlay == null
            || model == null) return;

        try
        {
            // Log.Info($"HERE: {__instance.Model?.Id}");
            // if (__instance.Model?.Id.ToString() == "CARD.STRIKE_IRONCLAD")
            if (true)
            {
                if (model.Rarity != CardRarity.Ancient) ____ancientPortrait.Texture = ____portrait.Texture;

                ____portrait.Visible = false;
                ____portraitBorder.Visible = false;
                ____banner.Visible = false;

                CardModel referenceCard = model.Type switch
                {
                    CardType.Attack => ancientAttack,
                    CardType.Power => ancientPower,
                    _ => ancientSkill
                };

                if (model.Rarity == CardRarity.Ancient) referenceCard = model;
                if (MyModConfig.HideType) referenceCard = ancientSkill;

                ____ancientBanner.Visible = !MyModConfig.HideTitle;
                ____ancientBanner.Material = referenceCard.BannerMaterial;

                // ____banner.Material = referenceCard.BannerMaterial;
                // ____banner.Texture = null;
                ____frame.Visible = false;

                ____ancientPortrait.Visible = true;

                ____ancientBorder.Visible = true;
                ____ancientBorder.Texture = referenceCard.AncientBorder;

                if (model.Rarity == CardRarity.Ancient)
                    ____ancientBanner.Visible = !MyModConfig.HideTitle;
                else
                    ____banner.Visible = !MyModConfig.HideTitle;

                ____ancientTextBg.Texture = referenceCard.AncientTextBg;
                ____ancientTextBg.Visible = !MyModConfig.HideDescription;
                
                ____ancientBorderGlassOverlay.Visible = !MyModConfig.HideDescription;

                if (____canvasGroupMaskMaterial == null)
                    ____canvasGroupMaskMaterial =
                        PreloadManager.Cache.GetMaterial("res://scenes/cards/card_canvas_group_mask_material.tres");
                ____portraitCanvasGroup.Material = ____canvasGroupMaskMaterial;
            }
        }
        catch (Exception err)
        {
            Log.Error($"Something broke {err}");
        }
    }

    private static CardModel ancientAttack = ModelDb.Card<NeowsFury>();
    private static CardModel ancientPower = ModelDb.Card<Corruption>();
    private static CardModel ancientSkill = ModelDb.Card<Apotheosis>();
}