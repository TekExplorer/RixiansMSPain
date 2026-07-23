using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace HideDetailsMod.HideDetailsModCode;

static class Credits
{
    public static string WithoutUpgrade(this string self)
    {
        if (self.EndsWith("_plus")) return self[..self.LastIndexOf("_plus")];
        return self;
    }

    // TODO: every file gets a credit. Specially handle if the credit is the same as non-alt version
    public static IEnumerable<IHoverTip> Tooltips(CardModel card)
    {
        List<IHoverTip> tips = [];

        // tips.AddItem(new HoverTip(new LocString("credits", ".title")));
        var creditsKey = CreditsKeyFor(card);

        var author = new LocString("artists", creditsKey);
        if (!author.Exists())
        {
            creditsKey = creditsKey.WithoutUpgrade();
            author = new LocString("artists", creditsKey);
        }

        // TODO: BAD! FIX!
        bool isAlt = creditsKey.WithoutUpgrade() != DefaultCreditsKeyFor(card);
        bool isUpgrade = creditsKey.EndsWith("_plus");

        // var uploader = new LocString("artists", CreditsKeyFor(card) + ".uploader");
        if (author.Exists())
        {
            var desc = new LocString("artists", ".description");
            if (isAlt && isUpgrade) desc = new LocString("artists", ".description.alt.upgrade");
            else if (isUpgrade) desc = new LocString("artists", ".description.upgrade");
            else if (isAlt) desc = new LocString("artists", ".description.alt");

            desc.Add("Artist", Replace(author));

            tips.Add(new HoverTip(desc) { IsDebuff = true });
            //
            tips.AddRange(OverlayCredits(card));
        }

        {   // epitaph
            // TODO: do this way better
            var freddy = new LocString("artists", ".epitaph.freddy");
            var epitaphTitle = new LocString("artists", ".epitaph.freddy.title");

            var epitaph = new LocString("artists", creditsKey.WithoutUpgrade() + ".epitaph");
            var epitaphUpgraded = new LocString("artists", creditsKey.WithoutUpgrade() + "_plus" + ".epitaph");
            epitaph.Add("Freddy", freddy);
            epitaphUpgraded.Add("Freddy", freddy);
            if (isUpgrade && epitaphUpgraded.Exists()) tips.Add(new HoverTip(epitaphTitle, epitaphUpgraded));
            else if (epitaph.Exists()) tips.Add(new HoverTip(epitaphTitle, epitaph));
        }

        return tips;
    }

    static IEnumerable<IHoverTip> OverlayCredits(CardModel card)
    {
        var key = CreditsKeyFor(card);
        var overlayAuthor = new LocString("artists", key.WithoutUpgrade() + ".overlay");

        var overlayAuthorUpgraded = new LocString("artists", key.WithoutUpgrade() + "_plus" + ".overlay");
        if (card.IsUpgraded && overlayAuthorUpgraded.Exists()) overlayAuthor = overlayAuthorUpgraded;

        if (overlayAuthor.Exists())
        {
            var overlayDesc = new LocString("artists", ".description.overlay");
            overlayDesc.Add("Artist", Replace(overlayAuthor));
            yield return new HoverTip(overlayDesc) { IsDebuff = true };
        }
    }
    internal static LocString Replace(LocString str) => LocString.GetIfExists("usernames", str.GetRawText().ToLower()) ?? str;
    internal static string CreditsKeyFor(CardModel card)
    {
        var pool = PoolOf(card);
        // TODO: Find a better way to do this. read from AlternateArts somehow?
        var name = card.PortraitPath.GetBaseName().GetFile();

        return $"{pool}.{name}"; // "silent.predator"
    }
    internal static string DefaultCreditsKeyFor(CardModel card)
    {
        var pool = PoolOf(card);
        var name = card.Id.Entry.ToLowerInvariant();

        return $"{pool}.{name}"; // "silent.predator"
    }

    internal static string PoolOf(CardModel card) => card.Pool.Title.ToLowerInvariant();
}