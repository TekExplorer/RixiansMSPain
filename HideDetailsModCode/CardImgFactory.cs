using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode;

public partial class AlternateArts
{
    public class CardImgFactory(Type CardType, string[] AllPaths, Func<CardModel, string?> Condition) : ICardImgFactory(AllPaths)
    {
        public CardImgFactory(Type CardType, string Path, Func<CardModel, bool?> Condition)
            : this(CardType, [Path], card => Condition(card) ?? false ? Path : null) { }

        // internal IEnumerable<CardImg> AllPathsAsImg => AllPaths.Select(Path => new CardImg(Path));
        // internal IEnumerable<CardImg> AllNormal => AllPathsAsImg.Where(Img => Img.Exists());
        // internal IEnumerable<CardImg> AllUpgraded => AllPathsAsImg.Select(Path => Path.Upgraded()).Where(Img => Img.Exists());
        // public IEnumerable<CardImg> All => [.. AllNormal, .. AllUpgraded];

        public Action<NCard, InspectionState>? WhenCardInspected { get; set; } = null;
        public Action<AbstractModel, CardModel>? WhenCardGenerated { get; set; } = null;
        public Action<AbstractModel, PlayerChoiceContext, CardPlay>? WhenCardPlayed { get; set; } = null;
        public Action<AbstractModel, PlayerChoiceContext, PowerModel, decimal>? WhenPowerApplied { get; set; } = null;



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
    }

    abstract public class ICardImgFactory(string[] AllPaths)
    {
        protected string[] AllPaths { get; } = AllPaths;
        internal IEnumerable<CardImg> AllPathsAsImg => AllPaths.Select(Path => new CardImg(Path));
        internal IEnumerable<CardImg> AllNormal => AllPathsAsImg.Where(Img => Img.Exists());
        internal IEnumerable<CardImg> AllUpgraded => AllPathsAsImg.Select(Path => Path.Upgraded()).Where(Img => Img.Exists());
        public IEnumerable<CardImg> All => [.. AllNormal, .. AllUpgraded];

        abstract public bool IsFor(CardModel card);
        abstract public CardImg? Get(CardModel card);
    }
    // public record CardImgFactory2<T>(string[] AllPaths, Func<T, string?> Condition) : ICardImgFactory where T : CardModel
    // {
    //     public CardImgFactory2(string Path, Func<T, bool?> Condition)
    //         : this([Path], card => Condition(card) ?? false ? Path : null) { }



    //     public Action<NCard, InspectionState>? WhenCardInspected { get; set; } = null;
    //     public Action<T, CardModel>? WhenCardGenerated { get; set; } = null;
    //     public Action<T, PlayerChoiceContext, CardPlay>? WhenCardPlayed { get; set; } = null;
    //     public Action<T, PlayerChoiceContext, PowerModel, decimal>? WhenPowerApplied { get; set; } = null;

    //     public bool IsFor(CardModel card) => card is T;
    //     public CardImg? Get(CardModel card)
    //     {
    //         if (card is not T _card)
    //         {
    //             MainFile.Logger.Error($"Attempted to Get an alt art img for {card.Id} without checking IsFor first. Expected {typeof(T)}");
    //             return null;
    //         }
    //         var result = Condition(_card);
    //         if (result == null) return null;
    //         return new(result);
    //     }
    // }
}