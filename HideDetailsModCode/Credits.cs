using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace HideDetailsMod.HideDetailsModCode;

class Credits
{
    public static IEnumerable<IHoverTip> Tooltips(CardModel card)
    {
        List<IHoverTip> tips = [];

        // tips.AddItem(new HoverTip(new LocString("credits", ".title")));

        var author = new LocString("artists", CreditsKeyFor(card));

        // TODO: BAD! FIX!
        bool isAlt = CreditsKeyFor(card) != DefaultCreditsKeyFor(card);

        // var uploader = new LocString("artists", CreditsKeyFor(card) + ".uploader");
        if (author.Exists())
        {
            var desc = new LocString("artists", ".description");
            if (isAlt) desc = new LocString("artists", ".description.alt");

            desc.Add("Artist", Replace(author));

            tips.Add(new HoverTip(desc) { IsDebuff = true });
            //
            tips.AddRange(OverlayCredits(card));
        }
        return tips;
    }

    static IEnumerable<IHoverTip> OverlayCredits(CardModel card)
    {
        var overlayAuthor = new LocString("artists", CreditsKeyFor(card) + ".overlay");

        if (overlayAuthor.Exists())
        {
            var overlayDesc = new LocString("artists", ".description.overlay");
            overlayDesc.Add("Artist", Replace(overlayAuthor));
            yield return new HoverTip(overlayDesc) { IsDebuff = true };
        }
    }
    internal static LocString Replace(LocString str) => LocString.GetIfExists("usernames", str.GetRawText()) ?? str;
    internal static string CreditsKeyFor(CardModel card)
    {
        return DefaultCreditsKeyFor(card);
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