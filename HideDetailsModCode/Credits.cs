using System.Text;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

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
    private readonly record struct RoleCredit(string RoleName, string FormattedAuthors);

    public static IEnumerable<IHoverTip> Tooltips(CardModel card)
    {
        var tips = new List<IHoverTip>();

        var key = CardCreditKeyFor(card);
        var defaultKey = DefaultCreditsKeyFor(card);

        bool isAlt = key.WithoutUpgrade() != defaultKey.WithoutUpgrade();
        bool isUpgrade = key.IsUpgraded;

        var roles = new List<RoleCredit>();

        // 1. Determine main art role localized name
        string artRoleKey = (isUpgrade, isAlt) switch
        {
            (false, false) => ".role.art",
            (true, false) => ".role.art.upgrade",
            (false, true) => ".role.art.alt",
            (true, true) => ".role.art.alt.upgrade",
        };

        if (ResolveAuthorsForKey(key, suffix: null, isUpgrade) is { } artAuthors)
            roles.Add(new RoleCredit(GetRoleLabel(artRoleKey, "Art"), FormatLocalizedAuthorList(artAuthors)));

        if (ResolveAuthorsForKey(key, suffix: "overlay", isUpgrade) is { } overlayAuthors)
            roles.Add(new RoleCredit(GetRoleLabel(".role.overlay", "Overlay"), FormatLocalizedAuthorList(overlayAuthors)));

        if (ResolveAuthorsForKey(key, suffix: "sfx", isUpgrade) is { } sfxAuthors)
            roles.Add(new RoleCredit(GetRoleLabel(".role.sfx", "SFX"), FormatLocalizedAuthorList(sfxAuthors)));

        if (ResolveAuthorsForKey(key, suffix: "vfx", isUpgrade) is { } vfxAuthors)
            roles.Add(new RoleCredit(GetRoleLabel(".role.vfx", "VFX"), FormatLocalizedAuthorList(vfxAuthors)));

        // Combine all asset contributors into a single cohesive tooltip
        if (BuildCombinedCreditTip(roles) is { } combinedTip)
        {
            tips.Add(combinedTip);
        }

        // 2. Memorial / Epitaph Credits remain distinct
        if (GetEpitaphTip(key, isUpgrade) is { } epitaphTip)
        {
            tips.Add(epitaphTip);
        }

        return tips;
    }

    private static IHoverTip? BuildCombinedCreditTip(List<RoleCredit> roles)
    {
        if (roles.Count == 0) return null;

        // If single role or all roles share identical authors, collapse to one line
        bool allSameAuthors = roles.All(r => r.FormattedAuthors == roles[0].FormattedAuthors);
        if (allSameAuthors)
        {
            string combinedRoles = FormatLocalizedList(roles.Select(r => r.RoleName).ToList());
            var tipLoc = new LocString("artists", ".credits.entry");
            tipLoc.Add("Role", combinedRoles);
            tipLoc.Add("Artists", roles[0].FormattedAuthors);
            return new HoverTip(tipLoc) { IsDebuff = true };
        }

        // Multiple distinct roles -> generate each line via .credits.entry and combine
        var lines = new List<string>(roles.Count);
        foreach (var role in roles)
        {
            var lineLoc = new LocString("artists", ".credits.entry");
            lineLoc.Add("Role", role.RoleName);
            lineLoc.Add("Artists", role.FormattedAuthors);
            lines.Add(lineLoc.GetFormattedText());
        }

        var multiLoc = new LocString("artists", ".credits.multi");
        multiLoc.Add("Body", string.Join('\n', lines));

        return new HoverTip(multiLoc) { IsDebuff = true };
    }

    private static string FormatLocalizedAuthorList(IReadOnlyList<string> authors) =>
        FormatLocalizedList(authors);

    /// <summary>
    /// Formats an arbitrary number of items using Unicode CLDR-style list reduction:
    /// - .credits.list.two    => "{0} and {1}"
    /// - .credits.list.middle => "{0}, {1}"
    /// - .credits.list.end    => "{0}, and {1}"
    /// </summary>
    private static string FormatLocalizedList(IReadOnlyList<string> items)
    {
        if (items.Count == 0) return string.Empty;
        if (items.Count == 1) return items[0];

        // 2 items: use pairwise template
        if (items.Count == 2)
        {
            return FormatPattern(".credits.list.two", items[0], items[1], $"{items[0]} and {items[1]}");
        }

        // 3+ items: Fold left-to-right through 'middle', then close with 'end'
        string accumulated = items[0];
        for (int i = 1; i < items.Count - 1; i++)
        {
            accumulated = FormatPattern(".credits.list.middle", accumulated, items[i], $"{accumulated}, {items[i]}");
        }

        return FormatPattern(".credits.list.end", accumulated, items[^1], $"{accumulated}, and {items[^1]}");
    }

    private static string FormatPattern(string locKey, string left, string right, string fallback)
    {
        if (LocString.GetIfExists("artists", locKey) is { } pattern)
        {
            pattern.Add("0", left);
            pattern.Add("1", right);
            return pattern.GetFormattedText();
        }

        return fallback;
    }

    private static string GetRoleLabel(string locKey, string fallback)
    {
        return LocString.GetIfExists("artists", locKey)?.GetFormattedText() ?? fallback;
    }

    private static List<string>? ResolveAuthorsForKey(CardCreditKey key, string? suffix, bool isUpgraded, bool strictUpgrade = false)
    {
        if (isUpgraded)
        {
            string upgKey = key.FormatKey(upgraded: true, overrideSubKey: suffix);
            var upg = ResolveAuthors(upgKey);

            if (upg.isExplicitlySuppressed)
                return null;

            if (upg.names.Count > 0)
                return upg.names;

            if (strictUpgrade)
                return null;
        }

        string baseKey = key.FormatKey(upgraded: false, overrideSubKey: suffix);
        var baseRes = ResolveAuthors(baseKey);

        if (baseRes.isExplicitlySuppressed || baseRes.names.Count == 0)
            return null;

        return baseRes.names;
    }

    private static (List<string> names, bool isExplicitlySuppressed) ResolveAuthors(string key)
    {
        var authors = new List<string>();

        if (LocString.GetIfExists("artists", key) is { } directLoc)
        {
            string raw = directLoc.GetRawText().Trim();
            if (string.IsNullOrEmpty(raw))
            {
                return (authors, isExplicitlySuppressed: true);
            }

            // Split on '|' delimiter for multiple authors
            var parts = raw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                authors.Add(ResolveDisplayName(part));
            }

            return (authors, isExplicitlySuppressed: false);
        }

        return (authors, isExplicitlySuppressed: false);
    }

    private static string ResolveDisplayName(string rawName)
    {
        string lookupKey = rawName.ToLowerInvariant();
        string displayName = rawName;

        if (LocString.GetIfExists("usernames", lookupKey) is { } loc)
        {
            displayName = loc.GetFormattedText();
        }

        // If the display name already contains explicit BBCode markup, leave as-is
        if (displayName.Contains('['))
        {
            return displayName;
        }

        string handle = displayName.StartsWith('@') ? displayName : $"@{displayName}";

        // Check for color override key, e.g. "tekexplorer.color"
        if (LocString.GetIfExists("usernames", $"{lookupKey}.color") is { } colorLoc)
        {
            string colorDef = colorLoc.GetFormattedText().Trim();

            // Gradient syntax: "#startColor->#endColor"
            if (colorDef.Contains("->"))
            {
                var parts = colorDef.Split("->", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && TryParseGodotColor(parts[0], out var startCol) && TryParseGodotColor(parts[1], out var endCol))
                {
                    return ApplyGradient(handle, startCol, endCol);
                }
            }

            // Single color (hex code, numeric hex, or named color)
            if (!string.IsNullOrEmpty(colorDef))
            {
                return colorDef.StartsWith('#')
                    ? $"[color={colorDef}]{handle}[/color]"
                    : $"[{colorDef}]{handle}[/{colorDef}]";
            }
        }

        // Default fallback: Gold
        return $"[gold]{handle}[/gold]";
    }

    private static bool TryParseGodotColor(string input, out Color color)
    {
        var sentinel = new Color(-1f, -1f, -1f, -1f);
        color = Color.FromString(input, sentinel);
        if (color != sentinel)
        {
            return true;
        }

        color = Colors.White;
        return false;
    }

    private static string ApplyGradient(string text, Color start, Color end)
    {
        if (text.Length <= 1)
        {
            return $"[color=#{start.ToHtml(false)}]{text}[/color]";
        }

        var sb = new StringBuilder(text.Length * 24);
        for (int i = 0; i < text.Length; i++)
        {
            float weight = (float)i / (text.Length - 1);
            Color c = start.Lerp(end, weight);
            sb.Append("[color=#").Append(c.ToHtml(false)).Append(']').Append(text[i]).Append("[/color]");
        }

        return sb.ToString();
    }

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