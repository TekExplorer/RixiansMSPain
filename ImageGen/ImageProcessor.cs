using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ImageGen;

public readonly struct NormalizedPadding
{
    public readonly double LeftPct;
    public readonly double TopPct;
    public readonly double RightPct;
    public readonly double BottomPct;

    public NormalizedPadding(CanvasPadding pad, int targetW, int targetH)
    {
        LeftPct = targetW > 0 ? (double)pad.Left / targetW : 0;
        TopPct = targetH > 0 ? (double)pad.Top / targetH : 0;
        RightPct = targetW > 0 ? (double)pad.Right / targetW : 0;
        BottomPct = targetH > 0 ? (double)pad.Bottom / targetH : 0;
    }

    public (int Left, int Top, int Right, int Bottom) ToPixels(int width, int height)
    {
        return (
            (int)Math.Round(LeftPct * width),
            (int)Math.Round(TopPct * height),
            (int)Math.Round(RightPct * width),
            (int)Math.Round(BottomPct * height)
        );
    }
}

public static class ImageProcessor
{
    public static (Bitmap PaddedBitmap, int OrigW, int OrigH, bool WasDownscaled, ComputedPadding AppliedPadding, bool IsStandardCell) ProcessCard(
        Bitmap rawBmp,
        int targetW,
        int targetH,
        NormalizedPadding normPad,
        bool downscaleOversized)
    {
        int origW = rawBmp.Width;
        int origH = rawBmp.Height;

        double targetRatio = (double)targetW / targetH;
        double cardRatio = (double)origW / origH;
        bool isProportional = Math.Abs(cardRatio - targetRatio) < 0.035;
        bool isLargerThanTarget = origW > targetW || origH > targetH;
        bool isDoubleOrMore = origW >= (targetW * 2) || origH >= (targetH * 2);

        bool shouldDownscale = downscaleOversized && (isDoubleOrMore || isLargerThanTarget);

        int activeArtW;
        int activeArtH;
        bool isStandardCell = false;

        if (isProportional && (shouldDownscale || (origW == targetW && origH == targetH)))
        {
            activeArtW = targetW;
            activeArtH = targetH;
            isStandardCell = true;
        }
        else if (shouldDownscale)
        {
            // Best-effort aspect fit inside targetW x targetH bounds
            double scale = Math.Min((double)targetW / origW, (double)targetH / origH);
            activeArtW = Math.Max(1, (int)Math.Round(origW * scale));
            activeArtH = Math.Max(1, (int)Math.Round(origH * scale));
        }
        else
        {
            activeArtW = origW;
            activeArtH = origH;
            if (origW == targetW && origH == targetH) isStandardCell = true;
        }

        Bitmap activeArt;
        if (activeArtW != origW || activeArtH != origH)
        {
            activeArt = new Bitmap(activeArtW, activeArtH, PixelFormat.Format32bppArgb);
            activeArt.SetResolution(96, 96);
            using (var g = Graphics.FromImage(activeArt))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(rawBmp, new Rectangle(0, 0, activeArtW, activeArtH), 0, 0, origW, origH, GraphicsUnit.Pixel);
            }
        }
        else
        {
            activeArt = rawBmp;
        }

        var (padL, padT, padR, padB) = normPad.ToPixels(activeArtW, activeArtH);

        int totalCanvasW = activeArtW + padL + padR;
        int totalCanvasH = activeArtH + padT + padB;

        var finalPaddedBmp = new Bitmap(totalCanvasW, totalCanvasH, PixelFormat.Format32bppArgb);
        finalPaddedBmp.SetResolution(96, 96);

        using (var g = Graphics.FromImage(finalPaddedBmp))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(
                activeArt,
                new Rectangle(padL, padT, activeArtW, activeArtH),
                0, 0, activeArtW, activeArtH,
                GraphicsUnit.Pixel
            );
        }

        if (activeArt != rawBmp)
        {
            activeArt.Dispose();
        }

        var appliedPadding = new ComputedPadding
        {
            Left = padL,
            Top = padT,
            Right = padR,
            Bottom = padB
        };

        return (finalPaddedBmp, origW, origH, (activeArtW != origW || activeArtH != origH), appliedPadding, isStandardCell);
    }
}