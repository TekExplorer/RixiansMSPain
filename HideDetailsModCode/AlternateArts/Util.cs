using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts;

static class Extensions
{
    [Obsolete("Switch off using this when possible")]
    static public bool HasPowerCompat(this Player player, string powerName)
    {
        return player.Creature.Powers.Any(power => power.IsComapt(powerName));
    }

    [Obsolete("Switch off using this when possible")]
    static public bool IsComapt(this object thing, string name)
    {
        return thing.GetType().Name == name;
    }
}
public static class Util
{
    public static bool HasCard<T>(Player? owner) where T : CardModel => HasCard(owner, card => card is T);
    public static bool HasCard(Player? owner, Func<CardModel, bool> predicate) => CardsOf(owner).Any(predicate);
    public static IEnumerable<CardModel> CombatCardsOf(Player? player) => CardsOf(player, IncludeDeck: false);
    public static IEnumerable<CardModel> CardsOf(Player? player, bool IncludeDeck = true)
    {
        if (player == null) return [];
        if (CombatManager.Instance.IsInProgress) return CardPile.GetCards(player, IncludeDeck ? AllPiles : AllPilesExceptDeck);
        return IncludeDeck ? CardPile.GetCards(player, PileType.Deck) : [];
    }
    public static PileType[] AllPilesExceptDeck => [PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust, PileType.Play];
    public static PileType[] AllPiles => [PileType.Deck, .. AllPilesExceptDeck];

    public static Player? GetOwner(PowerModel? power) => power?.Owner?.Player;

    public static Player? GetOwner(CardModel? card)
    {
        if (card == null) return null;
        if (card.IsCanonical) return null;
        Player? player = null;
        try
        { player ??= card.Owner; }
        catch (Exception e)
        { MainFile.Logger.Warn($"card.Owner errored with: {e}"); }

        try
        { player ??= LocalContext.GetMe(card.RunState); }
        catch (Exception e)
        { MainFile.Logger.Warn($"LocalContext.GetMe(card.RunState) errored with: {e}"); }

        return player;
    }
}
