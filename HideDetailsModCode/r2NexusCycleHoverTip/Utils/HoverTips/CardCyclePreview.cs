using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

public static class CardCyclePreview
{
    public static IHoverTip FromCards(params CardModel[] cards) => FromCards(cards.AsEnumerable());

    public static IHoverTip FromCards(
        IEnumerable<CardModel> cards,
        CardCyclePreviewOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(cards);

        List<CardModel> resolvedCards = cards
            .Where(card => card != null)
            .ToList();

        return FromCards(() => resolvedCards, options);
    }

    public static IHoverTip FromCards(
        Func<IEnumerable<CardModel>> cardResolver,
        CardCyclePreviewOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(cardResolver);

        return new CardCyclePreviewHoverTip(cardResolver, options);
    }
}