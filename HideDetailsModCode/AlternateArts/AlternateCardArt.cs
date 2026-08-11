using System.Reflection;
using MegaCrit.Sts2.Core.Models;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts;

public abstract class AlternateCardArt<T> : IAlternateCardArt where T : CardModel
{
    protected AlternateCardArt() : base(1)
    {
        // "this.GetType()" captures the concrete subclass context safely inside the constructor
        _lazyImages = new Lazy<CardImg[]>(() =>
        {
            Type subclassType = GetType();
            return subclassType
                .GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.PropertyType == typeof(CardImg) && p.DeclaringType == subclassType)
                .Select(p => (CardImg)p.GetValue(null)!)
                .ToArray();
        });
    }
    private readonly Lazy<CardImg[]> _lazyImages;
    public virtual IEnumerable<CardImg> GetAll(T card) => _lazyImages.Value;
    public override IEnumerable<CardImg> GetAll(CardModel card)
    {
        if (card is T typed) return GetAll(typed);
        return [];
    }
    protected virtual bool ShowIfCanonical => false;
    public override CardImg? Get(CardModel card)
    {
        if (card.IsCanonical && !ShowIfCanonical) return null;
        if (card is T typed) return Get(typed);
        return null;
    }
    public abstract CardImg? Get(T card);
}
