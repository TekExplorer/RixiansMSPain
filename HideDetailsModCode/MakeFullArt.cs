using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
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
        NCard? __instance,
        MegaLabel? ____typeLabel,
        NinePatchRect? ____typePlaque
    )
    {
        var model = __instance?.Model;
        if (model == null) return;
        try
        {
            if (____typePlaque != null) ____typePlaque.Visible = !MyModConfig.HideType;
            if (____typeLabel != null) ____typeLabel.Visible = !MyModConfig.HideType;
            // if (____descriptionLabel != null) ____descriptionLabel.Visible = !MyModConfig.HideDescription;
            // if (____titleLabel != null) ____titleLabel.Visible = !MyModConfig.HideTitle;
        }
        catch (Exception err)
        {
            MainFile.Logger.Error($"Something broke 2 {err}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), nameof(NCard.Reload))]
    // [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    // [HarmonyPatch(typeof(NCard), nameof(NCard.UpdatePortrait))]

    static void UseFullArt(
        NCard? __instance,
        TextureRect? ____frame,
        TextureRect? ____portraitBorder,
        TextureRect? ____ancientBorder,
        TextureRect? ____portrait,
        TextureRect? ____ancientPortrait,
        TextureRect? ____banner,
        Control? ____ancientBanner,
        TextureRect? ____ancientTextBg,
        TextureRect? ____ancientBorderGlassOverlay,
        Material? ____canvasGroupMaskMaterial,
        CanvasGroup? ____portraitCanvasGroup
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
            // MainFile.Logger.Info($"HERE: {__instance.Model?.Id}");
            // if (__instance.Model?.Id.ToString() == "CARD.STRIKE_IRONCLAD")

            // TODO: Load the arts separately so we can make it a setting
            if (model.Rarity != CardRarity.Ancient) ____ancientPortrait.Texture = ____portrait.Texture;

            ____portrait.Visible = false;
            ____portraitBorder.Visible = false;

            CardModel referenceCard = model.Type switch
            {
                CardType.Attack => ancientAttack,
                CardType.Power => ancientPower,
                _ => ancientSkill
            };

            if (model.Rarity == CardRarity.Ancient) referenceCard = model;
            if (MyModConfig.HideType) referenceCard = ancientSkill;


            // ____banner.Material = referenceCard.BannerMaterial;
            // ____banner.Texture = null;
            ____frame.Visible = false;

            ____ancientPortrait.Visible = true;

            ____ancientBorder.Visible = true;
            ____ancientBorder.Texture = referenceCard.AncientBorder;

            // Title
            ____ancientBanner.Material = referenceCard.BannerMaterial;

            // reset first
            ____banner.Visible = false;
            ____ancientBanner.Visible = false;

            const float BannerTransparency = .7f;

            if (model.Rarity == CardRarity.Ancient || MyModConfig.MakeEverythingAncient)
            {
                ____ancientBanner.Visible = !MyModConfig.HideTitleBanner;
                ____ancientBanner.Modulate = ____ancientBanner.Modulate with { A = BannerTransparency };
            }
            else
            {
                ____banner.Visible = !MyModConfig.HideTitleBanner;
                ____banner.Modulate = ____banner.Modulate with { A = BannerTransparency };
            }

            // Description
            ____ancientTextBg.Texture = referenceCard.AncientTextBg;
            ____ancientTextBg.Visible = !MyModConfig.HideDescription;
            ____ancientBorderGlassOverlay.Visible = !MyModConfig.HideDescription;

            // Card Shape Mask
            ____canvasGroupMaskMaterial ??=
                PreloadManager.Cache.GetMaterial("res://scenes/cards/card_canvas_group_mask_material.tres");
            ____portraitCanvasGroup.Material = ____canvasGroupMaskMaterial;
        }
        catch (Exception err)
        {
            MainFile.Logger.Error($"UseFullArt: {err}");
        }
    }

    private static readonly CardModel ancientAttack = ModelDb.Card<NeowsFury>();
    private static readonly CardModel ancientPower = ModelDb.Card<Corruption>();
    private static readonly CardModel ancientSkill = ModelDb.Card<Apotheosis>();
}