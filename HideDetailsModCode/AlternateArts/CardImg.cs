using Godot;
using MegaCrit.Sts2.Core.Models;

namespace HideDetailsMod.HideDetailsModCode;

static class StrExtension
{
    internal static string GetTextAfter(this string source, string marker)
    {
        // Check for null or empty inputs to prevent runtime crashes
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(marker))
        {
            return string.Empty;
        }

        // Find the starting index of the marker
        int index = source.IndexOf(marker);

        // If marker is not found, return empty (or return source based on your needs)
        if (index == -1)
        {
            return string.Empty;
        }

        // Cut the string starting exactly after the marker
        return source.Substring(index + marker.Length);
    }
}
public record CardImg(string Path)
{
    static public CardImg? Of(string fullPath)
    {
        if (!fullPath.Contains("HideDetailsMod")) return null;
        var text = "HideDetailsMod/images/atlases/card_atlas.sprites/";
        if (fullPath.Contains(text))
        {
            var result = fullPath.GetTextAfter(text);
            return new(result.GetBaseName());
        }
        text = "res://HideDetailsMod/artist_assets/";
        if (fullPath.Contains(text))
        {
            var result = fullPath.GetTextAfter(text);
            return new(result.GetBaseName());
        }
        return null;
    }

    public CardImg(CardModel card) : this($"{card.Pool.Title.ToLowerInvariant()}/{card.Id.Entry.ToLowerInvariant()}") { }
    public static CardImg Upgraded(CardModel card) => new CardImg(card).Upgraded();
    public string PortraitPath => $"res://HideDetailsMod/images/atlases/card_atlas.sprites/{Path}.tres";
    public string PortraitPngPath => $"res://HideDetailsMod/artist_assets/{Path}.png";
    // public string PortraitPngPath { get; } = ImageHelperExtensions.GetModImagePath($"{path}.png");
    internal bool Exists() => ResourceLoader.Exists(PortraitPath);
    public bool IsUpgraded => Path.EndsWith("_plus");
    public CardImg Upgraded() => IsUpgraded ? this : new(Path + "_plus");
    public CardImg Downgraded() => IsUpgraded ? new(Path[..Path.LastIndexOf("_plus")]) : this;
    public bool IsBeta => Path.Contains("/beta/");
    public CardImg Beta() => IsBeta ? this : new(Path.Replace("/", "/beta/"));

    public CardImg NonBeta() => !IsBeta ? this : new(Path.Replace("/beta/", "/"));
}