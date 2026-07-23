using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Entities.Rngs;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace HideDetailsMod.HideDetailsModCode;

static partial class EventChatter
{
    internal record Chatter(string Key, Func<EventModel, Chatter[], bool>[] Conditions)
    {
        internal Chatter(string Key, Func<EventModel, Chatter[], bool> Condition) : this(Key, [Condition]) { }
        internal Chatter(string Key, Func<EventModel, bool>[] Conditions) : this(Key, (Event, _) => Conditions.All(Condition => Condition(Event))) { }
        internal Chatter(string Key, Func<EventModel, bool> Condition) : this(Key, [Condition]) { }
        internal Chatter(string Key) : this(Key, _ => true) { }
        public bool Validate(EventModel Event, Chatter[] Previous) => Conditions.All(Condition => Condition(Event, Previous));

        public LocString? Loc => LocString.GetIfExists("event_chatter", Key);

        public static List<DynamicVar> BuildDynamicVars(EventModel Event, Chatter[] Previous)
        {
            var Owner = Event.Owner;
            var rng = new Rng(Owner?.PlayerRng.Seed ?? 0, "EventChatterRng");
            List<DynamicVar> vars = [
                new StringVar("Ascension", Owner?.RunState?.AscensionLevel.ToString() ?? "???"),
                new StringVar("Character", Owner?.Character.Title.GetFormattedText() ?? "???"),
                new StringVar("RestSiteRelic", GetRestSiteRelics(Owner).TakeRandom(1, rng).FirstOrDefault()?.Title.GetFormattedText() ?? "???"),
            ];
            return vars;
        }
    }
    internal static Func<EventModel, Chatter[], bool> OnOwner(params Func<Player, bool>[] Conditions) => OnOwner((Event, _) => Conditions.All(Condition => Condition(Event)));
    internal static Func<EventModel, Chatter[], bool> OnOwner(params Func<Player, Chatter[], bool>[] Conditions) => (Event, Previous) =>
    {
        if (Event.Owner is Player player) return Conditions.All(Condition => Condition(player, Previous));
        return false;
    };
    internal static bool TinkerTimeOnly(EventModel Event) => Event is TinkerTime;
    internal static bool HasCardInDeck<T>(Player Owner) where T : CardModel
        => Owner.Deck.Cards.Any(card => card is T);
    internal static bool HasRelic<T>(Player Owner) where T : RelicModel
        => Owner.Relics.Any(relic => relic is T);

    internal static bool HasMiniatureTent(Player Owner) => HasRelic<MiniatureTent>(Owner);
    internal static bool HasRestSiteRelic(Player Owner)
        => GetRestSiteRelics(Owner).Any();
    internal static IEnumerable<RelicModel> GetRestSiteRelics(Player? Owner)
        => Owner?.Relics.Where(relic =>
        {
            List<RestSiteOption> options = [];
            relic.TryModifyRestSiteOptions(Owner, options);
            return options.Count != 0;
        }) ?? [];

    internal static bool HasSlayTheRelicsInstalled(EventModel _) => ModManager.Mods.Any(mod => mod.manifest?.id == "SlayTheRelicsExporter");

    static LocTable GetTable() => LocManager.Instance.GetTable("event_chatter");

    [GeneratedRegex(@"^Generic\.\d+$")]
    static internal partial Regex GenericRegex();
    static IEnumerable<string> GenericKeys => GetTable().Keys.Where(s => GenericRegex().IsMatch(s));

    [GeneratedRegex(@"^TinkerTime\.\d+$")]
    static internal partial Regex TinkerTimeRegex();
    static IEnumerable<string> TinkerTimeKeys => GetTable().Keys.Where(s => TinkerTimeRegex().IsMatch(s));

    static readonly Chatter[] Items = [
        ..GenericKeys.Select(Key => new Chatter(Key)),
        ..TinkerTimeKeys.Select(Key => new Chatter(Key, TinkerTimeOnly)),
        new("Generic.Praise.Rixian"),
        new("Generic.Praise.Helios"),
        new("Generic.Praise.TekExplorer"),
        new("Generic.Conditional.NotA10", _ => RunManager.Instance.AscensionManager.HasLevel(AscensionLevel.DoubleBoss)),
        new("Generic.Conditional.TheBall.InDeck", OnOwner(HasCardInDeck<TheBall>)),
        new("Generic.Conditional.Feral.InDeck", OnOwner(HasCardInDeck<Feral>)),
        new("Generic.Conditional.HasTentRelic", OnOwner(HasMiniatureTent)),
        new("Generic.Conditional.HasTentRelic.PlusRestRelic", OnOwner(HasMiniatureTent, HasRestSiteRelic)),
        ..Enumerable.Range(1,3).Select(index => new Chatter($"Generic.Conditional.SlayTheRelicsInstalled.{index}", HasSlayTheRelicsInstalled)),
        new("Generic.Conditional.Claw.InDeck.1", OnOwner(HasCardInDeck<Claw>)),
        new("Generic.Conditional.Claw.InDeck.2", OnOwner(HasCardInDeck<Claw>)),
        new("Generic.Conditional.Claw.InDeck.With2", OnOwner((Event, Previous) => {
            if (!HasCardInDeck<Claw>(Event) || Previous.Length == 0) return false;
            return Previous.Any(element => element.Key == "Generic.Conditional.Claw.InDeck.2");
        })),
        new("Generic.Conditional.MultipleJuzu", Event => Event.Owner?.RunState.Players.Any(HasRelic<JuzuBracelet>) ?? false),
        new("TinkerTime.Conditional.MoreThan4Players", [TinkerTimeOnly, Event => Event.Owner?.RunState.Players.Count > 4]),
        // Show what exactly? They don't have names!
        // new("Generic.Conditional.CardWithUpgrade.InDeck", OnOwner(Owner => Owner.Deck.Cards.Any(CardHasUpgradeArt))),
        // new("Generic.Conditional.CardWithAlt.InDeck", OnOwner(Owner => Owner.Deck.Cards.Any(CardHasAltArt))),
    ];

    static internal bool CardHasUpgradeArt(CardModel card)
    {
        return false;
        // card.AllPortraitPaths;
        // card.PortraitPath;
    }
    static internal bool CardHasAltArt(CardModel card)
    {
        return false;
        // card.AllPortraitPaths;
        // card.PortraitPath;
    }


}