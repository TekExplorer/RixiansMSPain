using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts;

static class Extensions
{
    [Obsolete("Switch off using this when possible")]
    static public bool HasPowerIdCompat(this Player player, string powerId)
    {
        return player.Creature.Powers.Any(power => power.Id.Entry == powerId);

    }
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
    public static NetModSettings ConfigFrom(this Player? player) => NetModSettings.GetPlayerConfig(player?.NetId) ?? new();
    public static NetModSettings ConfigFrom(this CardModel? card) => ConfigFrom(card.GetOwnerSafely());
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

    public static Player? GetOwnerSafely(this PowerModel? power) => power?.Owner?.Player;

    public static Player? GetOwnerSafely(this CardModel? card)
    {
        try
        {
            if (card == null) return null;
            if (card.IsCanonical) return null;
            if (card.Owner is { } owner) return owner;
            return LocalContext.GetMe(card.RunState);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"An error threw while trying to get the owner of card {card?.Id}: {e}");
            return null;
        }
    }
}
