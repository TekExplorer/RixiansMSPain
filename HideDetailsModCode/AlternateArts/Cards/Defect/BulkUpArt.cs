using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Random;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

class BulkUpArt : AlternateCardArt<BulkUp>
{
    static CardImg Crunchy { get; } = new("defect/bulk_up_crunchy");
    public override CardImg? Get(BulkUp card)
    {
        if (UsesCrunchyArt[card]) return Crunchy;
        return null;
    }

    static SavedSpireField<BulkUp, bool> UsesCrunchyArt { get; } = new(() => false, "MSPain_IsCrunchy");
    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.BeforeHandDrawLate))]
    static class BulkUpArtPatch
    {
        [HarmonyPrefix]
        static void BeforeHandDrawLate(AbstractModel __instance, Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
        {
            if (__instance is not BulkUp card) return;
            // Rng rng = new(player.PlayerRng.Transformations.ToSerializable());
            Rng rng = Rng.Chaotic;
            uint v = rng.NextUnsignedInt(10);
            var shouldUseCrunchy = v == 0;
            UsesCrunchyArt[card] = shouldUseCrunchy;
            if (card.DeckVersion is BulkUp DeckVersion) UsesCrunchyArt[DeckVersion] = shouldUseCrunchy;
        }
    }
}