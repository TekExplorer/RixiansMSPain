using System;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

internal sealed class ResolvedCardCycleHoverTip : CardHoverTip, IHoverTip
{
    private readonly string _id;

    public IResolvingHoverTip Source { get; }

    public ResolvedCardCycleHoverTip(
        CardModel card,
        IResolvingHoverTip source)
        : base(card)
    {
        Source = source;
        _id = $"BaseLib:CardCyclePreview:{source.Id}:{base.Id}:{Guid.NewGuid():N}";
    }

    string IHoverTip.Id => _id;

    bool IHoverTip.IsSmart => IsSmart;

    bool IHoverTip.IsDebuff => IsDebuff;

    bool IHoverTip.IsInstanced => true;

    AbstractModel? IHoverTip.CanonicalModel => CanonicalModel;
}