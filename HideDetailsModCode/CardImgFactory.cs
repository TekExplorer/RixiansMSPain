using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode;

public partial class AlternateArts
{
    public record CardImg(string Path)
    {
        public CardImg(CardModel card) : this($"{card.Pool.Title.ToLowerInvariant()}/{card.Id.Entry.ToLowerInvariant()}") { }
        public static CardImg Upgraded(CardModel card) => new CardImg(card).Upgraded();

        public string PortraitPath { get; } = $"res://images/atlases/card_atlas.sprites/{Path}.tres";
        private string _PortraitJpgPath { get; } = $"res://artist_assets/{Path}.jpg";
        private string _PortraitPngPath { get; } = $"res://artist_assets/{Path}.png";
        public string PortraitPngPath => ResourceLoader.Exists(_PortraitJpgPath) ? _PortraitJpgPath : _PortraitPngPath;

        // public string PortraitPngPath { get; } = ImageHelperExtensions.GetModImagePath($"{path}.png");
        public CardImg Upgraded() => Path.EndsWith("_plus") ? this : new(Path + "_plus");

        public bool Exists() => ResourceLoader.Exists(PortraitPath);
    }
    public class CardImgFactory(Type CardType, IEnumerable<string> AllPaths, Func<CardModel, string?> Condition) : ICardImgFactory(AllPaths)
    {
        public CardImgFactory(Type CardType, string Path, Func<CardModel, bool?> Condition)
            : this(CardType, [Path], card => Condition(card) ?? false ? Path : null) { }

        public override bool IsFor(CardModel card) => CardType.IsInstanceOfType(card);
        public override CardImg? Get(CardModel card)
        {
            if (!IsFor(card))
            {
                MainFile.Logger.Error($"Attempted to Get an alt art img for {card.Id} without checking IsFor first");
                return null;
            }
            var result = Condition(card);
            if (result == null) return null;
            return new(result);
        }

        public Action<NCard, InspectionState>? WhenCardInspected { get; set; }
        public Action<AbstractModel, CardModel>? WhenCardGenerated { get; set; }
        public Action<AbstractModel, PlayerChoiceContext, CardPlay>? WhenCardPlayed { get; set; }
        public Action<AbstractModel, PlayerChoiceContext, PowerModel, decimal>? WhenPowerApplied { get; set; }
        public Action<AbstractModel, PlayerChoiceContext, Creature, bool>? AfterDeath { get; set; }
        public Action<AbstractModel, PlayerChoiceContext, CombatSide, IEnumerable<Creature>>? WhenTurnEnd { get; set; }
        public Action<AbstractModel, CombatSide, IEnumerable<Creature>, ICombatState>? WhenTurnStart { get; set; }

        public override void OnCardInspected(NCard nCard, InspectionState state)
        { WhenCardInspected?.Invoke(nCard, state); }
        public override void OnCardGenerated(AbstractModel thisModel, CardModel generatedCard)
        { WhenCardGenerated?.Invoke(thisModel, generatedCard); }
        public override void OnCardPlayed(AbstractModel thisModel, PlayerChoiceContext choiceContext, CardPlay cardPlay)
        { WhenCardPlayed?.Invoke(thisModel, choiceContext, cardPlay); }
        public override void OnPowerApplied(AbstractModel thisModel, PlayerChoiceContext choiceContext, PowerModel power, decimal amount)
        { WhenPowerApplied?.Invoke(thisModel, choiceContext, power, amount); }
        public override void OnDeath(AbstractModel thisModel, PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented)
        { AfterDeath?.Invoke(thisModel, choiceContext, creature, wasRemovalPrevented); }

        public override void OnTurnEnd(AbstractModel thisModel, PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
        { WhenTurnEnd?.Invoke(thisModel, choiceContext, side, participants); }
        public override void OnTurnStart(AbstractModel thisModel, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
        { WhenTurnStart?.Invoke(thisModel, side, participants, combatState); }
    }

    abstract public class ICardImgFactory(IEnumerable<string> AllPaths)
    {
        public IEnumerable<string> AllPaths { get; } = AllPaths;
        internal IEnumerable<CardImg> AllPathsAsImg => AllPaths.Select(Path => new CardImg(Path));
        internal IEnumerable<CardImg> AllNormal => AllPathsAsImg.Where(Img => Img.Exists());
        internal IEnumerable<CardImg> AllUpgraded => AllPathsAsImg.Select(Path => Path.Upgraded()).Where(Img => Img.Exists());
        public IEnumerable<CardImg> All => [.. AllNormal, .. AllUpgraded];

        abstract public bool IsFor(CardModel card);
        abstract public CardImg? Get(CardModel card);

        public abstract void OnCardInspected(NCard nCard, InspectionState state);
        public abstract void OnCardGenerated(AbstractModel thisModel, CardModel generatedCard);
        public abstract void OnCardPlayed(AbstractModel thisModel, PlayerChoiceContext choiceContext, CardPlay cardPlay);
        public abstract void OnPowerApplied(AbstractModel thisModel, PlayerChoiceContext choiceContext, PowerModel power, decimal amount);
        public abstract void OnDeath(AbstractModel thisModel, PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented);
        public abstract void OnTurnEnd(AbstractModel thisModel, PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants);
        public abstract void OnTurnStart(AbstractModel thisModel, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState);
    }
    public class CardImgFactory2<T>(IEnumerable<string> AllPaths, Func<T, string?> Condition) : ICardImgFactory(AllPaths) where T : CardModel
    {
        public CardImgFactory2(string Path, Func<T, bool?> Condition)
            : this([Path], card => Condition(card) ?? false ? Path : null) { }

        public override bool IsFor(CardModel card) => card is T;
        public override CardImg? Get(CardModel card)
        {
            if (card is not T _card)
            {
                MainFile.Logger.Error($"Attempted to Get an alt art img for {card.Id} without checking IsFor first. Expected {typeof(T)}");
                return null;
            }
            var result = Condition(_card);
            if (result == null) return null;
            return new(result);
        }
        public Action<T, NCard, InspectionState>? WhenCardInspected { get; set; } = null;
        public Action<T, CardModel>? WhenCardGenerated { get; set; } = null;
        public Action<T, PlayerChoiceContext, CardPlay>? WhenCardPlayed { get; set; } = null;
        public Action<T, PlayerChoiceContext, PowerModel, decimal>? WhenPowerApplied { get; set; } = null;
        public Action<T, PlayerChoiceContext, Creature, bool>? AfterDeath { get; set; }
        public Action<T, PlayerChoiceContext, CombatSide, IEnumerable<Creature>>? WhenTurnEnd { get; set; }
        public Action<T, CombatSide, IEnumerable<Creature>, ICombatState>? WhenTurnStart { get; set; }

        public override void OnCardInspected(NCard nCard, InspectionState state)
        { if (nCard.Model is T self) WhenCardInspected?.Invoke(self, nCard, state); }
        public override void OnCardGenerated(AbstractModel thisModel, CardModel generatedCard)
        { if (thisModel is T self) WhenCardGenerated?.Invoke(self, generatedCard); }
        public override void OnCardPlayed(AbstractModel thisModel, PlayerChoiceContext choiceContext, CardPlay cardPlay)
        { if (thisModel is T self) WhenCardPlayed?.Invoke(self, choiceContext, cardPlay); }
        public override void OnPowerApplied(AbstractModel thisModel, PlayerChoiceContext choiceContext, PowerModel power, decimal amount)
        { if (thisModel is T self) WhenPowerApplied?.Invoke(self, choiceContext, power, amount); }
        public override void OnDeath(AbstractModel thisModel, PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented)
        { if (thisModel is T self) AfterDeath?.Invoke(self, choiceContext, creature, wasRemovalPrevented); }
        public override void OnTurnEnd(AbstractModel thisModel, PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
        { if (thisModel is T self) WhenTurnEnd?.Invoke(self, choiceContext, side, participants); }
        public override void OnTurnStart(AbstractModel thisModel, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
        { if (thisModel is T self) WhenTurnStart?.Invoke(self, side, participants, combatState); }
    }
}