using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts;

[HarmonyPatch]
public static class Tools
{
    internal class ColorBox(Color color) { public Color color = color; }
    static internal NotNullSpireField<NCard, ColorBox> NCardSparklesColor = new(card => new(card._sparkles.Modulate));
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    public static void ApplyGlow(NCard __instance, ref GpuParticles2D ____sparkles, ref NCardRareGlow? ____rareGlow, ref NCardUncommonGlow? ____uncommonGlow)
    {
        if (!GodotObject.IsInstanceValid(__instance)) return;
        if (!GodotObject.IsInstanceValid(__instance.Body)) return;
        if (!GodotObject.IsInstanceValid(____sparkles)) return;

        static void RemoveRarityGlow(ref NCardRareGlow? ____rareGlow, ref NCardUncommonGlow? ____uncommonGlow, NCard card)
        {
            card.KillRarityGlow();

            card.RemoveChildSafely(____rareGlow);
            ____rareGlow?.QueueFree();
            ____rareGlow = null;

            card.RemoveChildSafely(____uncommonGlow);
            ____uncommonGlow?.QueueFree();
            ____uncommonGlow = null;
        }
        void ResetSparkles(ref GpuParticles2D ____sparkles)
        {
            ____sparkles.Visible = false;
            ____sparkles.Modulate = NCardSparklesColor[__instance].color;
        }
        if (!MyModConfig.UseCustomArt)
        {
            // TODO: track if anything changed, and reset it if custom art was changed
            if (____sparkles.Modulate != NCardSparklesColor[__instance].color)
                ResetSparkles(ref ____sparkles); return;
        }

        var card = __instance;

        Color LuminesceColor = new Color(0.314f, 0.784f, 0.471f);
        bool IsLuminesce = card.Model is Luminesce { IsUpgraded: true };
        bool IsGlow = card.Model is Glow;

        if (!IsGlow && !IsLuminesce)
        {
            RemoveRarityGlow(ref ____rareGlow, ref ____uncommonGlow, card);
            card.CardHighlight.Modulate = NCardHighlight.playableColor;
            return;
        }

        if (!IsLuminesce) ResetSparkles(ref ____sparkles);

        if (IsLuminesce)
        {
            ____sparkles.Visible = true;
            ____sparkles.Modulate = LuminesceColor;
        }

        if (____rareGlow == null)
        {
            var glow = ____rareGlow = NCardRareGlow.Create();
            if (GodotObject.IsInstanceValid(glow))
            {
                card.Body.AddChildSafely(glow);
                card.Body.MoveChildSafely(glow, 1);
                if (IsLuminesce) glow.Modulate = LuminesceColor;
            }
        }

        if (____uncommonGlow == null)
        {
            var glow = ____uncommonGlow = NCardUncommonGlow.Create();
            if (GodotObject.IsInstanceValid(glow))
            {
                card.Body.AddChildSafely(glow);
                card.Body.MoveChildSafely(glow, 1);
                if (IsLuminesce) glow.Modulate = LuminesceColor;
            }
        }

        card.CardHighlight.Modulate = IsLuminesce ? LuminesceColor : NCardHighlight.gold;
    }

    public static NetModSettings ConfigFrom(Player? player) => NetModSettings.GetPlayerConfig(player?.NetId) ?? new();
    public static NetModSettings ConfigFrom(CardModel? card) => ConfigFrom(Util.GetOwner(card));
}