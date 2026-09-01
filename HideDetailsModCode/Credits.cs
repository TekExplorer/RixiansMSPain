using System;
using System.Collections.Generic;
using System.Text;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace HideDetailsMod.HideDetailsModCode;

public readonly record struct CardCreditKey(
    string Pool,
    string CardName,
    bool IsUpgraded,
    bool IsBeta,
    string? SubKey = null)
{
    private const string UpgradeSuffix = "_plus";

    public static CardCreditKey Parse(string rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
            return default;

        var parts = new List<string>(rawKey.Split('.'));
        string pool = parts.Count > 0 ? parts[0] : "";
        string cardSegment = parts.Count > 1 ? parts[1] : "";

        bool isUpgraded = cardSegment.EndsWith(UpgradeSuffix, StringComparison.OrdinalIgnoreCase);
        string cardName = isUpgraded ? cardSegment[..^UpgradeSuffix.Length] : cardSegment;

        // Consume the 3rd element if it is strictly the "beta" indicator
        bool isBeta = false;
        if (parts.Count > 2 && parts[2].Equals("beta", StringComparison.OrdinalIgnoreCase))
        {
            isBeta = true;
            parts.RemoveAt(2);
        }

        string? subKey = parts.Count > 2 ? string.Join('.', parts.GetRange(2, parts.Count - 2)) : null;
        return new CardCreditKey(pool, cardName, isUpgraded, isBeta, subKey);
    }

    public string FormatKey(bool? upgraded = null, string? overrideSubKey = null)
    {
        bool useUpgrade = upgraded ?? IsUpgraded;
        string? targetSubKey = overrideSubKey ?? SubKey;

        var sb = new StringBuilder(Pool).Append('.').Append(CardName);
        if (useUpgrade) sb.Append(UpgradeSuffix);
        if (IsBeta) sb.Append(".beta");

        if (!string.IsNullOrEmpty(targetSubKey))
        {
            if (!targetSubKey.StartsWith('.')) sb.Append('.');
            sb.Append(targetSubKey);
        }

        return sb.ToString();
    }

    public CardCreditKey WithoutUpgrade() => this with { IsUpgraded = false };
}

static class Credits
{
    public static IEnumerable<IHoverTip> Tooltips(CardModel card)
    {
        var tips = new List<IHoverTip>();

        var key = CardCreditKeyFor(card);
        var defaultKey = DefaultCreditsKeyFor(card);

        bool isAlt = key.WithoutUpgrade() != defaultKey.WithoutUpgrade();
        bool isUpgrade = key.IsUpgraded;

        // 1. Main Card Art Credit
        string variantTemplate = (isUpgrade, isAlt) switch
        {
            (false, false) => ".description",
            (true, false) => ".description.upgrade",
            (false, true) => ".description.alt",
            (true, true) => ".description.alt.upgrade",
        };

        if (GetCreditTip(key, variantTemplate, suffix: null, isUpgrade) is { } mainTip)
            tips.Add(mainTip);

        if (GetCreditTip(key, ".description.overlay", suffix: "overlay", isUpgrade) is { } overlayTip)
            tips.Add(overlayTip);

        // 3. Audio / Visual Effects Credits
        if (GetCreditTip(key, ".description.sfx", suffix: "sfx", isUpgrade) is { } sfxTip)
            tips.Add(sfxTip);

        if (GetCreditTip(key, ".description.vfx", suffix: "vfx", isUpgrade) is { } vfxTip)
            tips.Add(vfxTip);

        // 4. Epitaph Credits
        if (GetEpitaphTip(key, isUpgrade) is { } epitaphTip)
        {
            tips.Add(epitaphTip);
        }

        return tips;
    }

    /// <summary>
    /// Looks up the author string for the given asset/sub-asset key and injects it into the template.
    /// If an upgraded key maps to an empty string (""), it explicitly suppresses/skips the tooltip.
    /// </summary>
    private static IHoverTip? GetCreditTip(
        CardCreditKey key,
        string templateKey,
        string? suffix,
        bool isUpgraded,
        bool strictUpgrade = false,
        bool isDebuff = true)
    {
        var template = new LocString("artists", templateKey);
        if (!template.Exists()) return null;

        LocString? author = null;
        if (isUpgraded)
        {
            string upgKey = key.FormatKey(upgraded: true, overrideSubKey: suffix);
            if (LocString.GetIfExists("artists", upgKey) is { } upgAuthor)
            {
                // If the JSON explicitly defines an empty string, suppress the tip entirely
                if (string.IsNullOrEmpty(upgAuthor.GetRawText()))
                {
                    return null;
                }
                author = Replace(upgAuthor);
            }
            else if (strictUpgrade)
            {
                return null;
            }
        }

        if (author is null && LocString.GetIfExists("artists", key.FormatKey(upgraded: false, overrideSubKey: suffix)) is { } baseAuthor)
        {
            // If base author is also empty, treat as non-existent
            if (string.IsNullOrEmpty(baseAuthor.GetRawText()))
            {
                return null;
            }
            author = Replace(baseAuthor);
        }

        if (author is null) return null;

        template.Add("Artist", author);
        return new HoverTip(template) { IsDebuff = isDebuff };
    }

    /// <summary>
    /// Handles epitaph tooltips. If an upgraded key maps to an empty string (""), it explicitly suppresses/skips it.
    /// </summary>
    private static IHoverTip? GetEpitaphTip(CardCreditKey key, bool isUpgraded, bool strictUpgrade = false)
    {
        var epitaphTitle = new LocString("artists", ".epitaph.freddy.title");
        var freddy = new LocString("artists", ".epitaph.freddy.desc");

        LocString? epitaph = null;
        if (isUpgraded)
        {
            string upgKey = key.FormatKey(upgraded: true, overrideSubKey: "epitaph");
            if (LocString.GetIfExists("artists", upgKey) is { } upgEpitaph)
            {
                if (string.IsNullOrEmpty(upgEpitaph.GetRawText()))
                {
                    return null;
                }
                epitaph = upgEpitaph;
            }
            else if (strictUpgrade)
            {
                return null;
            }
        }

        if (epitaph is null && LocString.GetIfExists("artists", key.FormatKey(upgraded: false, overrideSubKey: "epitaph")) is { } baseEpitaph)
        {
            if (string.IsNullOrEmpty(baseEpitaph.GetRawText()))
            {
                return null;
            }
            epitaph = baseEpitaph;
        }

        if (epitaph is null) return null;

        epitaph.Add("Freddy", freddy);
        return new HoverTip(epitaphTitle, epitaph);
    }

    internal static LocString Replace(LocString str) =>
        LocString.GetIfExists("usernames", str.GetRawText().ToLowerInvariant()) ?? str;

    internal static CardCreditKey CardCreditKeyFor(CardModel card)
    {
        string pool = PoolOf(card);
        string name = card.PortraitPath.GetFile().GetBaseName().ToLowerInvariant();
        bool isBeta = card.PortraitPath.GetBaseDir() == "beta" || card.PortraitPath.Contains("/beta/");

        return CardCreditKey.Parse($"{pool}.{name}{(isBeta ? ".beta" : "")}");
    }

    internal static CardCreditKey DefaultCreditsKeyFor(CardModel card)
    {
        string pool = PoolOf(card);
        string name = card.Id.Entry.ToLowerInvariant();
        bool isBeta = card.PortraitPath.Contains("/beta/");

        return new CardCreditKey(pool, name, IsUpgraded: card.IsUpgraded, IsBeta: isBeta);
    }

    internal static string PoolOf(CardModel card) => card.Pool.Title.ToLowerInvariant();
}