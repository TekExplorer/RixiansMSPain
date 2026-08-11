using BaseLib.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

// TODO: UNUSED

namespace RixiansRePaint.HideDetailsModCode;

/// <summary>
/// In loc: {Card:We have {Card}|We have nothing :(}
/// </summary>
public class CardVar : StringVar
{
    public CardVar(string name, CardModel? card = null) : base(name, card?.Title ?? "")
    {
        _card = card;
        this.WithTooltip(var => Card == null ? null : HoverTipFactory.FromCard(Card));
    }
    public CardVar(CardModel? card = null) : this("Card", card) { }

    private CardModel? _card;
    public CardModel? Card
    {
        get => _card;
        set
        {
            _card = value;
            StringValue = _card?.Title ?? "";
        }
    }
}

static class DynamicVarToolTipExtensions
{
    public static TDynamicVar WithTooltip<TDynamicVar>(this TDynamicVar var, IHoverTip? tip) where TDynamicVar : DynamicVar
    {
        return var.WithTooltip(_ => tip);
    }
    public static TDynamicVar WithTooltip<TDynamicVar>(this TDynamicVar var, Func<DynamicVar, IHoverTip?> factory) where TDynamicVar : DynamicVar
    {
#nullable disable
        DynamicVarExtensions.DynamicVarTips[var] = factory;
#nullable restore
        return var;
    }
}