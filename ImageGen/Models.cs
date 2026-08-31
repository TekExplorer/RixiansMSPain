using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;

namespace ImageGen;

public enum CardBucket { Main, Oversized }
public enum MainTier { Exact = 0, CloseRatio = 1, Undersized = 2 }
public enum OversizedShape { Proportional, TooWide, TooTall }

public enum MismatchedPaddingMode
{
    ScaleToFit,
    Unscaled,
    None
}

public class CanvasPadding
{
    public int Left { get; set; } = 0;
    public int Top { get; set; } = 0;
    public int Right { get; set; } = 0;
    public int Bottom { get; set; } = 0;
    public bool ScaleWithResolution { get; set; } = true;
    public string MismatchedRatioMode { get; set; } = "ScaleToFit";
    public bool ShowOutline { get; set; } = true;
    public string OutlineColor { get; set; } = "#FF00FF";

    public int TotalW => Left + Right;
    public int TotalH => Top + Bottom;

    public MismatchedPaddingMode GetMismatchedMode()
    {
        if (Enum.TryParse<MismatchedPaddingMode>(MismatchedRatioMode, true, out var mode))
        {
            return mode;
        }
        return MismatchedPaddingMode.ScaleToFit;
    }

    public System.Drawing.Color GetOutlineColor() => ColorUtils.ParseHex(OutlineColor, System.Drawing.Color.FromArgb(255, 0, 255));

    public ComputedPadding ComputeFor(int origW, int origH, int targetW, int targetH, bool isRatioMatch)
    {
        if (TotalW == 0 && TotalH == 0)
        {
            return new ComputedPadding { Left = 0, Top = 0, Right = 0, Bottom = 0 };
        }

        if (!ScaleWithResolution)
        {
            return new ComputedPadding { Left = Left, Top = Top, Right = Right, Bottom = Bottom };
        }

        double scaleX = (double)origW / targetW;
        double scaleY = (double)origH / targetH;

        if (isRatioMatch)
        {
            return new ComputedPadding
            {
                Left = (int)Math.Round(Left * scaleX),
                Top = (int)Math.Round(Top * scaleY),
                Right = (int)Math.Round(Right * scaleX),
                Bottom = (int)Math.Round(Bottom * scaleY)
            };
        }

        var mode = GetMismatchedMode();
        switch (mode)
        {
            case MismatchedPaddingMode.None:
                return new ComputedPadding { Left = 0, Top = 0, Right = 0, Bottom = 0 };

            case MismatchedPaddingMode.Unscaled:
                return new ComputedPadding { Left = Left, Top = Top, Right = Right, Bottom = Bottom };

            case MismatchedPaddingMode.ScaleToFit:
            default:
                double fitScale = Math.Min(scaleX, scaleY);
                return new ComputedPadding
                {
                    Left = (int)Math.Round(Left * fitScale),
                    Top = (int)Math.Round(Top * fitScale),
                    Right = (int)Math.Round(Right * fitScale),
                    Bottom = (int)Math.Round(Bottom * fitScale)
                };
        }
    }
}

public struct ComputedPadding
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public int TotalW => Left + Right;
    public int TotalH => Top + Bottom;
}

public class DummyVariant
{
    public string Name { get; set; } = "dummy";
    public int Left { get; set; } = 0;
    public int Top { get; set; } = 0;
    public int Right { get; set; } = 0;
    public int Bottom { get; set; } = 0;

    public CanvasPadding ToPadding() => new()
    {
        Left = Left,
        Top = Top,
        Right = Right,
        Bottom = Bottom,
        ScaleWithResolution = false
    };
}

public static class ColorUtils
{
    public static System.Drawing.Color ParseHex(string hex, System.Drawing.Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        string clean = hex.Trim().TrimStart('#');
        if (clean.Length == 6 && int.TryParse(clean, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
        {
            return System.Drawing.Color.FromArgb(255, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
        }
        if (clean.Length == 8 && uint.TryParse(clean, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint argb))
        {
            return System.Drawing.Color.FromArgb((int)(argb >> 24) & 0xFF, (int)(argb >> 16) & 0xFF, (int)(argb >> 8) & 0xFF, (int)argb & 0xFF);
        }
        return fallback;
    }
}

public class HeaderConfig
{
    public bool Enabled { get; set; } = true;
    public int Height { get; set; } = 26;
    public string FontFamily { get; set; } = "Consolas";
    public float FontSize { get; set; } = 10f;
    public bool Bold { get; set; } = true;
    public bool IncludeDimensions { get; set; } = true;
    public bool IncludeAspectRatio { get; set; } = true;
    public string Color { get; set; } = "#DCDCDC";
    public string WarningColor { get; set; } = "#FF5050";
    public string CorrectRatioColor { get; set; } = "#64D2FF";

    public System.Drawing.Color GetNormalColor() => ColorUtils.ParseHex(Color, System.Drawing.Color.FromArgb(220, 220, 220));
    public System.Drawing.Color GetWarningColor() => ColorUtils.ParseHex(WarningColor, System.Drawing.Color.FromArgb(255, 80, 80));
    public System.Drawing.Color GetCorrectRatioColor() => ColorUtils.ParseHex(CorrectRatioColor, System.Drawing.Color.FromArgb(100, 210, 255));
}

public class SingleAtlasProfile
{
    public string Name { get; set; } = "default";
    public bool Enabled { get; set; } = true;
    public string InputRoot { get; set; }
    public string OutputRoot { get; set; }
    public string AtlasName { get; set; } = "card_atlas";
    public int? TargetWidth { get; set; } = 606;
    public int? TargetHeight { get; set; } = 852;
    public int? Padding { get; set; } = 8;
    public int? MaxTextureSize { get; set; } = 16384;
    public int? WarnAboveSize { get; set; } = 4096;
    public bool? DownscaleProportionalOversized { get; set; } = true;
    public bool? Force { get; set; } = false;
    public CanvasPadding CanvasPadding { get; set; } = new();
    public HeaderConfig Header { get; set; } = new();
    public List<DummyVariant> DummyVariants { get; set; } = new();
    public Dictionary<string, List<string>> Groups { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class MultiAtlasConfigFile
{
    public List<SingleAtlasProfile> Configs { get; set; }
    public string InputRoot { get; set; }
    public string OutputRoot { get; set; }
    public string AtlasName { get; set; }
    public int? TargetWidth { get; set; }
    public int? TargetHeight { get; set; }
    public int? Padding { get; set; }
    public int? MaxTextureSize { get; set; }
    public int? WarnAboveSize { get; set; }
    public bool? DownscaleProportionalOversized { get; set; }
    public bool? Force { get; set; }
    public bool? Enabled { get; set; }
    public CanvasPadding CanvasPadding { get; set; }
    public HeaderConfig Header { get; set; }
    public List<DummyVariant> DummyVariants { get; set; }
    public Dictionary<string, List<string>> Groups { get; set; }
}

public class CardItem
{
    public string Name;
    public string RelSubDir;
    public string RelFilePath;
    public string GroupKey;
    public string ImageHash;
    public CardItem CanonicalItem;
    public int OrigW, OrigH, CardW, CardH;
    public int TotalCellW, TotalCellH;
    public int CellX, CellY, ArtX, ArtY;
    public ComputedPadding AppliedPadding;
    public string AtlasPngRes;
    public bool HasCorrectRatio;
    public bool WasDownscaled;
    public CardBucket Bucket;
    public MainTier Tier;
    public OversizedShape ShapeCategory;
    public Bitmap Bmp;
}