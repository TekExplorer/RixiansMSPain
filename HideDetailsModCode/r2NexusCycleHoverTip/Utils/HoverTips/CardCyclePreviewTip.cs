using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

public sealed class CardCyclePreviewHoverTip : IResolvingHoverTip
{
    private readonly Func<IEnumerable<CardModel>> _resolver;
    private readonly CardCyclePreviewOptions _options;
    private readonly string _id;

    public CardCyclePreviewHoverTip(
        Func<IEnumerable<CardModel>> resolver,
        CardCyclePreviewOptions? options = null)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _options = options ?? CardCyclePreviewOptions.Default;
        _id = $"BaseLib:CardCyclePreview:{Guid.NewGuid():N}";
    }

    public string Id => _id;

    public bool IsSmart => false;

    public bool IsDebuff => false;

    public bool IsInstanced => true;

    public AbstractModel? CanonicalModel => null;

    public int ResolveVersion
    {
        get
        {
            var cards = ResolveCards();

            if (cards.Count == 0)
                return -1;

            return CurrentIndex(cards.Count);
        }
    }

    public IHoverTip? ResolveHoverTip()
    {
        List<CardModel> cards = ResolveCards();

        if (cards.Count == 0)
            return null;

        CardModel card = cards[CurrentIndex(cards.Count)];

        if (_options.Upgrade)
        {
            card = (CardModel)card.MutableClone();
            card.UpgradeInternal();
            card.FinalizeUpgradeInternal();
        }

        return new ResolvedCardCycleHoverTip(card, this);
    }

    private int CurrentIndex(int count)
    {
        double secondsPerCard = Math.Max(0.1, _options.TimePerCard.TotalSeconds);
        double elapsedSeconds = Time.GetTicksMsec() / 1000.0;

        return (int)(Math.Floor(elapsedSeconds / secondsPerCard) % count);
    }

    private List<CardModel> ResolveCards()
    {
        try
        {
            IEnumerable<CardModel> cards = _resolver()
                .Where(card => card != null);

            if (_options.RemoveDuplicateTypes)
                cards = cards.DistinctBy(card => card.GetType());

            return cards
                .Take(Math.Max(1, _options.MaxCards))
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}