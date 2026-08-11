using System.Collections.Generic;
using System.Diagnostics;
using BaseLib.Utils;
using HideDetailsMod.HideDetailsModCode;
using HideDetailsMod.HideDetailsModCode.AlternateArts;
using HideDetailsMod.HideDetailsModCode.Patches;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts;

public static class CardOverrideExtensions
{
    // internal static SpireField<CardModel, (CardImg? Base, CardImg? Override)> Overrides { get; } = new SpireField<CardModel, (CardImg? Base, CardImg? Override)>(() => (null, null)).CopyOnClone();

    // extension(CardModel card)
    // {
    //     // For C# 14 users: true read-write property
    //     public CardImg? OverrideArtImage
    //     {
    //         get => Overrides.Get(card);
    //         set => Overrides.Set(card, value);
    //     }

    // }   
    // For C# 13 users: standard method fallback
    // public static (CardImg? Base, CardImg? Upgrade) GetOverrideArt(this CardModel card) => Overrides.Get(card);
    // public static void SetOverrideArt(this CardModel card, CardImg? Base = null, CardImg? Upgrade = null) => Overrides.Set(card, (Base, Upgrade));
    // public static void ClearOverrideArt(this CardModel card) => Overrides.Set(card, (null, null));
}

public class OverrideArt() : IAlternateCardArt(-1)
{
    // Fetches directly from the extension class field
    public override CardImg? Get(CardModel card)
    {
        // var ImgOverride = card.GetOverrideArt();
        var (Base, Upgraded) = card.GetOverrideImage();
        if (card.IsUpgraded) return Upgraded;
        return Base;
    }


    public override IEnumerable<CardImg> GetAll(CardModel card)
    {
        if (Get(card) is { } img)
        {
            yield return img;
        }
    }
}