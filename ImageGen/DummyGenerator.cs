using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace ImageGen;

public static class DummyGenerator
{
    public static CardItem CreateDummyCard(DummyVariant variant, int targetW, int targetH, int headerH)
    {
        var cPad = variant.ToPadding();
        int dW = targetW + cPad.TotalW;
        int dH = targetH + cPad.TotalH;
        int artX = cPad.Left;
        int artY = cPad.Top;
        int artCenterX = artX + (targetW / 2);
        int artCenterY = artY + (targetH / 2);

        var bmp = new Bitmap(dW, dH, PixelFormat.Format32bppArgb);

        // 1. Render Non-Linear Heat Gradient Buffer
        var bmpData = bmp.LockBits(new Rectangle(0, 0, dW, dH), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        unsafe
        {
            byte* ptr = (byte*)bmpData.Scan0;
            for (int y = 0; y < dH; y++)
            {
                byte* row = ptr + (y * bmpData.Stride);
                for (int x = 0; x < dW; x++)
                {
                    double normX = (x >= artX && x < artX + targetW)
                        ? Math.Min(x - artX, (artX + targetW - 1) - x) / (targetW / 2.0)
                        : 0.0;
                    double normY = (y >= artY && y < artY + targetH)
                        ? Math.Min(y - artY, (artY + targetH - 1) - y) / (targetH / 2.0)
                        : 0.0;

                    normX = Math.Max(0.0, Math.Min(1.0, normX));
                    normY = Math.Max(0.0, Math.Min(1.0, normY));

                    double heat = 1.0 - Math.Pow(Math.Min(normX, normY), 0.35);

                    byte r, g, b;
                    if (x < artX || y < artY || x >= artX + targetW || y >= artY + targetH)
                    {
                        r = 35; g = 10; b = 35; // Distinct dark violet for outer padding
                    }
                    else
                    {
                        if (heat > 0.70)
                        {
                            double t = (heat - 0.70) / 0.30;
                            r = (byte)(200 + (55 * t));
                            g = (byte)(20 + (80 * (1.0 - t)));
                            b = (byte)(20 * (1.0 - t));
                        }
                        else if (heat > 0.40)
                        {
                            double t = (heat - 0.40) / 0.30;
                            r = (byte)(30 + (170 * t));
                            g = (byte)(110 + (40 * t));
                            b = (byte)(130 * (1.0 - t));
                        }
                        else
                        {
                            double t = heat / 0.40;
                            r = (byte)(15 + (15 * t));
                            g = (byte)(22 + (88 * t));
                            b = (byte)(35 + (95 * t));
                        }
                    }

                    row[x * 4 + 0] = b;
                    row[x * 4 + 1] = g;
                    row[x * 4 + 2] = r;
                    row[x * 4 + 3] = 255;
                }
            }
        }
        bmp.UnlockBits(bmpData);

        // 2. Render Calibration Overlay
        using (var g = Graphics.FromImage(bmp))
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            using var labelFont = new Font("Consolas", 7.5f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var headerFont = new Font("Consolas", 10.5f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var subHeaderFont = new Font("Consolas", 9f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var centerFont = new Font("Consolas", 10f, FontStyle.Bold, GraphicsUnit.Pixel);

            using var textBrushDim = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
            using var textBrushYellow = new SolidBrush(Color.FromArgb(255, 230, 80));
            using var textBrushCyan = new SolidBrush(Color.FromArgb(100, 220, 255));
            using var textBrushWhite = new SolidBrush(Color.White);
            using var textBrushGreen = new SolidBrush(Color.FromArgb(0, 255, 140));
            using var ringPen = new Pen(Color.FromArgb(90, 255, 255, 255), 1) { DashStyle = DashStyle.Dash };
            using var subGridPen = new Pen(Color.FromArgb(35, 255, 255, 255), 1);

            // A. Iso-Percentage Boundary Rings (Every 5%)
            for (int pct = 95; pct >= 10; pct -= 5)
            {
                double scale = pct / 100.0;
                int rw = (int)(targetW * scale);
                int rh = (int)(targetH * scale);
                int rx = artX + (targetW - rw) / 2;
                int ry = artY + (targetH - rh) / 2;
                g.DrawRectangle(ringPen, rx, ry, rw, rh);
                if (pct % 10 == 0 || pct >= 85) g.DrawString($"{pct}%", labelFont, textBrushDim, rx + 4, ry + 2);
            }

            // B. Normalized 10% Grid Lines & Intersections
            for (int px = 0; px <= 100; px += 10)
            {
                int x = artX + (int)Math.Round(targetW * (px / 100.0));
                g.DrawLine(subGridPen, x, artY, x, artY + targetH);
                for (int py = 0; py <= 100; py += 10)
                {
                    int y = artY + (int)Math.Round(targetH * (py / 100.0));
                    if (px == 0) g.DrawLine(subGridPen, artX, y, artX + targetW, y);
                    if (px % 20 == 0 && py % 20 == 0 && !(px == 50 && py == 50))
                    {
                        g.DrawString($"{px}%,{py}%\n+{x - artX},+{y - artY}", labelFont, textBrushDim, x + 3, y + 3);
                    }
                }
            }

            // C. Center Crosshairs (50%, 50%)
            using var centerCrossPen = new Pen(Color.FromArgb(255, 230, 80), 2);
            g.DrawLine(centerCrossPen, artX, artCenterY, artX + targetW, artCenterY);
            g.DrawLine(centerCrossPen, artCenterX, artY, artCenterX, artY + targetH);
            g.DrawEllipse(centerCrossPen, artCenterX - 14, artCenterY - 14, 28, 28);
            g.FillEllipse(textBrushYellow, artCenterX - 3, artCenterY - 3, 6, 6);

            // D. Top-Left Origin (0%, 0%) Marker
            using var originPen = new Pen(Color.FromArgb(0, 255, 140), 2.5f);
            g.DrawLine(originPen, artX, artY, artX + 40, artY);
            g.DrawLine(originPen, artX, artY, artX, artY + 40);
            g.DrawEllipse(originPen, artX - 6, artY - 6, 12, 12);
            g.FillEllipse(textBrushGreen, artX - 2, artY - 2, 4, 4);

            // E. HIGH-VISIBILITY INFO BADGE (Placed safely above center)
            int badgeW = 230;
            int badgeH = 72;
            int badgeX = artCenterX - (badgeW / 2);
            int badgeY = artCenterY - 125; // Sits above center crosshair

            using var badgeBackBrush = new SolidBrush(Color.FromArgb(245, 15, 18, 24));
            using var badgeOuterPen = new Pen(Color.FromArgb(255, 230, 80), 2);
            using var badgeInnerPen = new Pen(Color.FromArgb(120, 100, 220, 255), 1);

            g.FillRectangle(badgeBackBrush, badgeX, badgeY, badgeW, badgeH);
            g.DrawRectangle(badgeOuterPen, badgeX, badgeY, badgeW, badgeH);
            g.DrawRectangle(badgeInnerPen, badgeX + 3, badgeY + 3, badgeW - 6, badgeH - 6);

            // Badge text content
            using var strFormatCenter = new StringFormat { Alignment = StringAlignment.Center };
            g.DrawString($"★ {variant.Name.ToUpper()} ★", headerFont, textBrushYellow, badgeX + (badgeW / 2), badgeY + 8, strFormatCenter);
            g.DrawString($"PAD: L:{cPad.Left}px | T:{cPad.Top}px | R:{cPad.Right}px | B:{cPad.Bottom}px", subHeaderFont, textBrushCyan, badgeX + (badgeW / 2), badgeY + 26, strFormatCenter);
            g.DrawString($"Active Art: {targetW}x{targetH} px", labelFont, textBrushWhite, badgeX + (badgeW / 2), badgeY + 43, strFormatCenter);
            g.DrawString($"Canvas Total: {dW}x{dH} px (Center: {targetW / 2}, {targetH / 2})", labelFont, textBrushDim, badgeX + (badgeW / 2), badgeY + 55, strFormatCenter);

            // F. Target Frame Outline & Outer Canvas Outline
            using var targetBoxPen = new Pen(Color.FromArgb(255, 255, 255), 2);
            g.DrawRectangle(targetBoxPen, artX, artY, targetW - 1, targetH - 1);

            if (cPad.TotalW > 0 || cPad.TotalH > 0)
            {
                using var padPen = new Pen(cPad.GetOutlineColor(), 2);
                g.DrawRectangle(padPen, 1, 1, dW - 2, dH - 2);
            }
        }

        return new CardItem
        {
            Name = variant.Name,
            RelSubDir = "",
            RelFilePath = $"{variant.Name}.png",
            GroupKey = "dummy",
            OrigW = targetW,
            OrigH = targetH,
            CardW = dW,
            CardH = dH,
            TotalCellW = dW,
            TotalCellH = dH + headerH,
            HasCorrectRatio = true,
            Bucket = CardBucket.Main,
            Tier = MainTier.Exact,
            Bmp = bmp
        };
    }
}