using MegaCrit.Sts2.Core.Models;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts;

public class BaseArt() : IAlternateCardArt(0)
{
    // static private HashSet<ModelId> MissingBaseArtIds { get; } = [];
    public override CardImg? Get(CardModel card) => new(card);
    public override IEnumerable<CardImg> GetAll(CardModel card)
    { yield return new(card); }
}
