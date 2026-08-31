using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ImageGen;

public class AtlasPacker
{
    private readonly SingleAtlasProfile _profile;
    private readonly string _baseDir;

    private const long GODOT_MAX_PIXELS = 268435456L;

    public AtlasPacker(SingleAtlasProfile profile, string baseDir)
    {
        _profile = profile;
        _baseDir = baseDir;
    }

    private static Dictionary<string, string> LoadCache(string cacheFilePath)
    {
        if (!File.Exists(cacheFilePath)) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            string json = File.ReadAllText(cacheFilePath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static void SaveCache(string cacheFilePath, Dictionary<string, string> cache)
    {
        try
        {
            string json = JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(cacheFilePath, json);
        }
        catch { }
    }

    public bool Execute()
    {
        string inRoot = _profile.InputRoot;
        string outRoot = _profile.OutputRoot;
        string atlasName = _profile.AtlasName ?? "card_atlas";
        int targetW = _profile.TargetWidth ?? 606;
        int targetH = _profile.TargetHeight ?? 852;
        int pad = _profile.Padding ?? 8;
        int maxTextureSize = Math.Min(_profile.MaxTextureSize ?? 16384, 16384);
        int warnAboveSize = _profile.WarnAboveSize ?? 4096;
        bool downscaleProp = _profile.DownscaleProportionalOversized ?? true;
        var cPad = _profile.CanvasPadding ?? new();
        var hdr = _profile.Header ?? new();
        int headerH = hdr.Enabled ? hdr.Height : 0;
        bool force = _profile.Force ?? false;
        bool isDummyGenerator = _profile.DummyVariants != null && _profile.DummyVariants.Count > 0;

        if (!isDummyGenerator && string.IsNullOrWhiteSpace(inRoot))
        {
            PrintColored($"[ERROR] Missing inputRoot in profile: {_profile.Name}", ConsoleColor.Red);
            return false;
        }

        if (string.IsNullOrWhiteSpace(outRoot))
        {
            PrintColored($"[ERROR] Missing outputRoot in profile: {_profile.Name}", ConsoleColor.Red);
            return false;
        }

        if (!string.IsNullOrWhiteSpace(inRoot) && !Path.IsPathRooted(inRoot)) inRoot = Path.GetFullPath(Path.Combine(_baseDir, inRoot));
        if (!Path.IsPathRooted(outRoot)) outRoot = Path.GetFullPath(Path.Combine(_baseDir, outRoot));

        Directory.CreateDirectory(outRoot);
        double targetRatio = (double)targetW / targetH;
        double targetInvRatio = (double)targetH / targetW;
        long standardArea = (long)targetW * targetH;

        PrintColored("\n========================================================", ConsoleColor.DarkCyan);
        PrintColored($"   🎨 Atlas Build Profile: [{_profile.Name.ToUpper()}]", ConsoleColor.Cyan);
        PrintColored("========================================================", ConsoleColor.DarkCyan);
        if (!isDummyGenerator)
        {
            PrintColored("Source    : ", ConsoleColor.DarkGray, false); PrintColored(inRoot ?? "(Procedural Dummies)", ConsoleColor.White);
        }
        PrintColored("Target    : ", ConsoleColor.DarkGray, false); PrintColored(outRoot, ConsoleColor.White);
        PrintColored("Standard  : ", ConsoleColor.DarkGray, false);
        PrintColored($"{targetW}x{targetH} px (Aspect Ratio 1:{targetInvRatio:0.00})", ConsoleColor.Yellow);
        PrintColored("Downscale : ", ConsoleColor.DarkGray, false);
        PrintColored(downscaleProp ? "Enabled (Oversized valid-ratio art scales down to fit slots)" : "Disabled", ConsoleColor.Cyan);

        if (cPad.TotalW > 0 || cPad.TotalH > 0)
        {
            PrintColored("Base Pad  : ", ConsoleColor.DarkGray, false);
            PrintColored($"+L:{cPad.Left}, +T:{cPad.Top}, +R:{cPad.Right}, +B:{cPad.Bottom} px (Scaled: {cPad.ScaleWithResolution}, Mismatched: {cPad.MismatchedRatioMode})", ConsoleColor.Magenta);
        }

        string sharedCacheFile = Path.Combine(outRoot, ".atlas_cache.json");
        var cacheDb = LoadCache(sharedCacheFile);

        var allCards = new List<CardItem>();
        var duplicateCards = new List<CardItem>();

        if (isDummyGenerator)
        {
            PrintColored($"\n🧪 Generating {_profile.DummyVariants.Count} procedural dummy card variants...", ConsoleColor.Cyan);
            foreach (var variant in _profile.DummyVariants)
            {
                var dummyCard = DummyGenerator.CreateDummyCard(variant, targetW, targetH, headerH);
                allCards.Add(dummyCard);
            }
        }
        else
        {
            if (!Directory.Exists(inRoot))
            {
                PrintColored($"[ERROR] Input directory not found: {inRoot}", ConsoleColor.Red);
                return false;
            }

            string[] exts = { ".png", ".jpg", ".jpeg", ".webp", ".bmp" };
            var files = Directory.GetFiles(inRoot, "*.*", SearchOption.AllDirectories)
                .Where(f => exts.Contains(Path.GetExtension(f).ToLower()))
                .OrderBy(f => f)
                .ToArray();

            if (files.Length == 0)
            {
                PrintColored("\n⚠️ No images found in source path.", ConsoleColor.Yellow);
                return true;
            }

            PrintColored($"\n🔍 Scanning, hashing, and classifying {files.Length} cards...", ConsoleColor.Cyan);
            var uniqueMap = new Dictionary<string, CardItem>();
            using var imgSha = SHA256.Create();

            foreach (var f in files)
            {
                string parent = Path.GetDirectoryName(f);
                string relSub = parent.Length > inRoot.Length ? parent.Substring(inRoot.Length).TrimStart('\\', '/') : "";
                string relFile = f.Substring(inRoot.Length).TrimStart('\\', '/').Replace("\\", "/");

                byte[] bytes = File.ReadAllBytes(f);
                string fileHash = BitConverter.ToString(imgSha.ComputeHash(bytes)).Replace("-", "").ToLower();

                if (uniqueMap.TryGetValue(fileHash, out var canon))
                {
                    var duplicate = new CardItem
                    {
                        Name = Path.GetFileNameWithoutExtension(f),
                        RelSubDir = relSub,
                        RelFilePath = relFile,
                        GroupKey = ConfigLoader.ResolveGroup(relSub, _profile),
                        ImageHash = fileHash,
                        CanonicalItem = canon,
                        OrigW = canon.OrigW,
                        OrigH = canon.OrigH,
                        CardW = canon.CardW,
                        CardH = canon.CardH,
                        TotalCellW = canon.TotalCellW,
                        TotalCellH = canon.TotalCellH,
                        AppliedPadding = canon.AppliedPadding,
                        Bucket = canon.Bucket,
                        Tier = canon.Tier,
                        ShapeCategory = canon.ShapeCategory,
                        HasCorrectRatio = canon.HasCorrectRatio,
                        WasDownscaled = canon.WasDownscaled
                    };
                    allCards.Add(duplicate);
                    duplicateCards.Add(duplicate);
                    continue;
                }

                using var raw = new Bitmap(new MemoryStream(bytes));
                double ratio = (double)raw.Width / raw.Height;
                bool isRatioMatch = Math.Abs(ratio - targetRatio) < 0.015;
                bool isLargerThanTarget = (long)raw.Width * raw.Height > standardArea;

                bool shouldDownscale = downscaleProp && isRatioMatch && isLargerThanTarget;

                Bitmap bmpToUse;
                int finalArtW, finalArtH;
                ComputedPadding appliedPad;

                if (shouldDownscale)
                {
                    // Downscale proportional art directly to standard target size
                    finalArtW = targetW;
                    finalArtH = targetH;
                    appliedPad = new ComputedPadding { Left = cPad.Left, Top = cPad.Top, Right = cPad.Right, Bottom = cPad.Bottom };

                    int paddedW = finalArtW + appliedPad.TotalW;
                    int paddedH = finalArtH + appliedPad.TotalH;
                    bmpToUse = new Bitmap(paddedW, paddedH, PixelFormat.Format32bppArgb);

                    using (var g = Graphics.FromImage(bmpToUse))
                    {
                        g.Clear(Color.Transparent);
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(raw, appliedPad.Left, appliedPad.Top, finalArtW, finalArtH);
                    }
                }
                else
                {
                    appliedPad = cPad.ComputeFor(raw.Width, raw.Height, targetW, targetH, isRatioMatch);
                    finalArtW = raw.Width;
                    finalArtH = raw.Height;

                    int paddedW = finalArtW + appliedPad.TotalW;
                    int paddedH = finalArtH + appliedPad.TotalH;
                    bmpToUse = new Bitmap(paddedW, paddedH, PixelFormat.Format32bppArgb);

                    using (var g = Graphics.FromImage(bmpToUse))
                    {
                        g.Clear(Color.Transparent);
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.DrawImage(raw, appliedPad.Left, appliedPad.Top, raw.Width, raw.Height);
                    }
                }

                var item = new CardItem
                {
                    Name = Path.GetFileNameWithoutExtension(f),
                    RelSubDir = relSub,
                    RelFilePath = relFile,
                    GroupKey = ConfigLoader.ResolveGroup(relSub, _profile),
                    ImageHash = fileHash,
                    OrigW = raw.Width,
                    OrigH = raw.Height,
                    CardW = bmpToUse.Width,
                    CardH = bmpToUse.Height,
                    AppliedPadding = appliedPad,
                    HasCorrectRatio = isRatioMatch,
                    WasDownscaled = shouldDownscale,
                    Bmp = bmpToUse
                };

                // Classify into Main vs Oversized Buckets
                if (shouldDownscale || (raw.Width == targetW && raw.Height == targetH))
                {
                    item.Bucket = CardBucket.Main;
                    item.Tier = MainTier.Exact;
                }
                else if (!isLargerThanTarget)
                {
                    item.Bucket = CardBucket.Main;
                    item.Tier = isRatioMatch ? MainTier.CloseRatio : MainTier.Undersized;
                }
                else if (isRatioMatch && !downscaleProp)
                {
                    item.Bucket = CardBucket.Main;
                    item.Tier = MainTier.CloseRatio;
                }
                else
                {
                    // Ratio-mismatched oversized art is strictly sent to the Oversized atlas
                    item.Bucket = CardBucket.Oversized;
                    item.ShapeCategory = ratio > targetRatio ? OversizedShape.TooWide : OversizedShape.TooTall;
                }

                if (item.Bucket == CardBucket.Main)
                {
                    item.TotalCellW = targetW + cPad.TotalW;
                    item.TotalCellH = targetH + cPad.TotalH + headerH;
                }
                else
                {
                    int extW = item.CardW;
                    int extH = (int)Math.Ceiling(item.CardW / targetRatio);
                    if (extH < item.CardH)
                    {
                        extH = item.CardH;
                        extW = (int)Math.Ceiling(item.CardH * targetRatio);
                    }
                    item.TotalCellW = Math.Max(item.CardW, extW);
                    item.TotalCellH = Math.Max(item.CardH, extH) + headerH;
                }

                uniqueMap[fileHash] = item;
                allCards.Add(item);
            }
        }

        var uniqueCards = allCards.Where(x => x.CanonicalItem == null).ToList();
        string godotBase = ResolveGodotRes(outRoot);
        var expectedTres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var expectedPngs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sizeViolations = new List<string>();

        PrintColored("\n⚙️  Building Atlas Sheets (Near-Square Aspect Ratio):", ConsoleColor.Cyan);

        // 1. Render Main Grids (Fixed Standard Cell Sizing to Prevent Waste)
        foreach (var grp in uniqueCards.Where(x => x.Bucket == CardBucket.Main).GroupBy(x => x.GroupKey))
        {
            var allGroupItems = grp.OrderBy(x => (int)x.Tier).ThenBy(x => x.Name).ToList();
            int cellW = targetW + cPad.TotalW;
            int cellH = targetH + cPad.TotalH + headerH;

            int maxColsPossible = Math.Max(1, (maxTextureSize - pad) / (cellW + pad));
            int maxRowsPossible = Math.Max(1, (maxTextureSize - pad) / (cellH + pad));
            int maxCardsPerSheet = maxColsPossible * maxRowsPossible;

            var pages = new List<List<CardItem>>();
            for (int i = 0; i < allGroupItems.Count; i += maxCardsPerSheet)
            {
                pages.Add(allGroupItems.Skip(i).Take(maxCardsPerSheet).ToList());
            }

            for (int pageIdx = 0; pageIdx < pages.Count; pageIdx++)
            {
                var items = pages[pageIdx];
                string pageSuffix = pages.Count > 1 ? $"_{pageIdx}" : "";
                string pngName = isDummyGenerator ? $"{atlasName}{pageSuffix}.png" : $"{atlasName}_{grp.Key}{pageSuffix}.png";
                string pngPath = Path.Combine(outRoot, pngName);
                string pngRes = $"{godotBase.TrimEnd('/')}/{pngName}";
                expectedPngs.Add(pngPath);

                // Optimal square layout: sqrt(N * H / W)
                double idealColRatio = Math.Sqrt((double)items.Count * cellH / cellW);
                int cols = (int)Math.Round(idealColRatio);
                cols = Math.Clamp(cols, 1, maxColsPossible);

                int rows = (int)Math.Ceiling((double)items.Count / cols);
                while (rows > maxRowsPossible && cols < maxColsPossible)
                {
                    cols++;
                    rows = (int)Math.Ceiling((double)items.Count / cols);
                }

                int sheetW = cols * (cellW + pad) + pad;
                int sheetH = rows * (cellH + pad) + pad;

                string sheetHashKey = $"{_profile.Name}:{pngName}";
                string computedSheetHash = ComputeAtlasSheetHash(items, sheetW, sheetH, cPad, hdr, isDummyGenerator);

                bool needsRebuild = force || !File.Exists(pngPath) || !cacheDb.TryGetValue(sheetHashKey, out var savedHash) || savedHash != computedSheetHash;

                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    it.CellX = pad + (i % cols) * (cellW + pad);
                    it.CellY = pad + (i / cols) * (cellH + pad);
                    it.ArtX = it.CellX;
                    it.ArtY = it.CellY + headerH;
                    it.AtlasPngRes = pngRes;
                }

                ValidateSheetSize(pngName, sheetW, sheetH, maxTextureSize, warnAboveSize, sizeViolations);

                if (!needsRebuild)
                {
                    PrintColored($"   ├─ ⚡ Up to date: [{grp.Key}{pageSuffix}] -> {pngName} ({items.Count} items)", ConsoleColor.DarkGray);
                    foreach (var it in items)
                    {
                        it.Bmp?.Dispose();
                    }
                    continue;
                }

                PrintColored($"   ├─ 🔨 Rendering [{grp.Key}{pageSuffix}]: ", ConsoleColor.Green, false);
                PrintColored($"{items.Count} items ({cols}x{rows} grid, compact {sheetW}x{sheetH} px)", ConsoleColor.White);

                using (var bmp = new Bitmap(sheetW, sheetH, PixelFormat.Format32bppArgb))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.Transparent);
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.PixelOffsetMode = PixelOffsetMode.Half;
                        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                        using var font = new Font(hdr.FontFamily, hdr.FontSize, hdr.Bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
                        using var normalBrush = new SolidBrush(hdr.GetNormalColor());
                        using var warnBrush = new SolidBrush(hdr.GetWarningColor());
                        using var correctBrush = new SolidBrush(hdr.GetCorrectRatioColor());
                        using var redPen = new Pen(hdr.GetWarningColor(), 2) { DashStyle = DashStyle.Dash };
                        using var cyanPen = new Pen(hdr.GetCorrectRatioColor(), 2) { DashStyle = DashStyle.Dash };
                        using var padPen = new Pen(cPad.GetOutlineColor(), 2) { DashStyle = DashStyle.Solid };
                        using var gridPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1);

                        for (int i = 0; i < items.Count; i++)
                        {
                            var it = items[i];
                            g.DrawRectangle(gridPen, it.CellX, it.CellY, cellW, cellH);

                            if (hdr.Enabled && headerH > 0)
                            {
                                var textRect = new RectangleF(it.CellX + 2, it.CellY, cellW - 4, headerH);
                                string text = BuildHeaderText(it, hdr);
                                Brush b = it.Tier == MainTier.Exact ? normalBrush : (it.HasCorrectRatio ? correctBrush : warnBrush);
                                g.DrawString(text, font, b, textRect, new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });
                            }

                            if (!isDummyGenerator && it.Tier != MainTier.Exact)
                            {
                                g.DrawRectangle(it.HasCorrectRatio ? cyanPen : redPen, it.ArtX + it.AppliedPadding.Left, it.ArtY + it.AppliedPadding.Top, targetW - 1, targetH - 1);
                            }

                            if (!isDummyGenerator && cPad.ShowOutline && (it.AppliedPadding.TotalW > 0 || it.AppliedPadding.TotalH > 0))
                            {
                                g.DrawRectangle(padPen, it.ArtX, it.ArtY, it.CardW - 1, it.CardH - 1);
                            }

                            g.DrawImage(it.Bmp, it.ArtX, it.ArtY, it.CardW, it.CardH);
                            it.Bmp.Dispose();
                        }
                    }
                    SavePng(bmp, pngPath);
                }

                cacheDb[sheetHashKey] = computedSheetHash;
            }
        }

        // 2. Render Shared Oversized Atlas (Shelf Bin-Packing with Page Spilling)
        var oversizedItems = uniqueCards.Where(x => x.Bucket == CardBucket.Oversized).ToList();
        if (oversizedItems.Count > 0)
        {
            var shapeGroups = new List<(string Title, List<CardItem> Cards)> {
                ("Proportional (Valid Ratio 1:1.41)", oversizedItems.Where(x => x.ShapeCategory == OversizedShape.Proportional).OrderByDescending(x => (long)x.CardW * x.CardH).ToList()),
                ("Too Wide / Fat (Needs Top/Bottom Extension)", oversizedItems.Where(x => x.ShapeCategory == OversizedShape.TooWide).OrderByDescending(x => (long)x.CardW * x.CardH).ToList()),
                ("Too Tall / Skinny (Needs Left/Right Extension)", oversizedItems.Where(x => x.ShapeCategory == OversizedShape.TooTall).OrderByDescending(x => (long)x.CardW * x.CardH).ToList())
            };

            var oversizedPages = PartitionOversizedPages(shapeGroups, maxTextureSize, pad, headerH);

            for (int pageIdx = 0; pageIdx < oversizedPages.Count; pageIdx++)
            {
                var page = oversizedPages[pageIdx];
                string pageSuffix = oversizedPages.Count > 1 ? $"_{pageIdx}" : "";
                string atlasFileName = $"{atlasName}_oversized{pageSuffix}.png";
                string pngOut = Path.Combine(outRoot, atlasFileName);
                string atlasPngRes = $"{godotBase.TrimEnd('/')}/{atlasFileName}";
                expectedPngs.Add(pngOut);

                foreach (var it in page.Items)
                {
                    it.AtlasPngRes = atlasPngRes;
                }

                string sheetHashKey = $"{_profile.Name}:{atlasFileName}";
                string computedSheetHash = ComputeAtlasSheetHash(page.Items, page.SheetW, page.SheetH, cPad, hdr, false);
                bool needsRebuild = force || !File.Exists(pngOut) || !cacheDb.TryGetValue(sheetHashKey, out var savedHash) || savedHash != computedSheetHash;

                ValidateSheetSize(atlasFileName, page.SheetW, page.SheetH, maxTextureSize, warnAboveSize, sizeViolations);

                if (!needsRebuild)
                {
                    PrintColored($"   ├─ ⚡ Up to date: [Oversized{pageSuffix}] -> {atlasFileName} ({page.Items.Count} cards)", ConsoleColor.DarkGray);
                    foreach (var it in page.Items)
                    {
                        it.Bmp?.Dispose();
                    }
                }
                else
                {
                    PrintColored($"   ├─ 🔨 Rendering Oversized Atlas [{pageIdx}]: ", ConsoleColor.Magenta, false);
                    PrintColored($"{page.Items.Count} cards -> {page.SheetW}x{page.SheetH} px", ConsoleColor.White);

                    using (var masterBmp = new Bitmap(page.SheetW, page.SheetH, PixelFormat.Format32bppArgb))
                    {
                        using (var g = Graphics.FromImage(masterBmp))
                        {
                            g.Clear(Color.Transparent);
                            g.InterpolationMode = InterpolationMode.NearestNeighbor;
                            g.PixelOffsetMode = PixelOffsetMode.Half;
                            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                            using var sectionFont = new Font("Consolas", 12f, FontStyle.Bold, GraphicsUnit.Pixel);
                            using var headerFont = new Font(hdr.FontFamily, hdr.FontSize, hdr.Bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
                            using var sectionBrush = new SolidBrush(Color.FromArgb(255, 200, 80));
                            using var textBrushWarning = new SolidBrush(hdr.GetWarningColor());
                            using var textBrushCorrectRatio = new SolidBrush(hdr.GetCorrectRatioColor());
                            using var cyanGuidePen = new Pen(hdr.GetCorrectRatioColor(), 2) { DashStyle = DashStyle.Dash };
                            using var yellowAddPen = new Pen(Color.FromArgb(255, 215, 0), 2) { DashStyle = DashStyle.Dot };
                            using var cellBorderPen = new Pen(Color.FromArgb(35, 255, 255, 255), 1);
                            using var stringFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };

                            foreach (var sl in page.SectionLabels)
                            {
                                g.DrawString($"=== {sl.Title.ToUpper()} ===", sectionFont, sectionBrush, pad, sl.Y);
                            }

                            foreach (var it in page.Items)
                            {
                                double invRatio = (double)it.CardH / it.CardW;
                                string ratioText = $"1:{invRatio:0.00}";
                                Brush headerBrush = it.HasCorrectRatio ? textBrushCorrectRatio : textBrushWarning;

                                if (headerH > 0)
                                {
                                    var textRect = new RectangleF(it.CellX + 2, it.CellY, it.TotalCellW - 4, headerH);
                                    g.DrawString($"[OVERSIZED] {it.RelFilePath} ({it.OrigW}x{it.OrigH} | {ratioText})", headerFont, headerBrush, textRect, stringFormat);
                                }

                                g.DrawRectangle(cellBorderPen, it.CellX, it.CellY, it.TotalCellW, it.TotalCellH);

                                if (it.HasCorrectRatio)
                                {
                                    g.DrawRectangle(cyanGuidePen, it.ArtX, it.ArtY, it.CardW - 1, it.CardH - 1);
                                }
                                else
                                {
                                    int extW = it.CardW;
                                    int extH = (int)Math.Ceiling(it.CardW / targetRatio);
                                    if (extH < it.CardH)
                                    {
                                        extH = it.CardH;
                                        extW = (int)Math.Ceiling(it.CardH * targetRatio);
                                    }
                                    int addX = it.ArtX - (extW - it.CardW) / 2;
                                    int addY = it.ArtY - (extH - it.CardH) / 2;
                                    g.DrawRectangle(yellowAddPen, addX, addY, extW - 1, extH - 1);
                                }

                                g.DrawImage(it.Bmp, it.ArtX, it.ArtY, it.CardW, it.CardH);
                                it.Bmp.Dispose();
                            }
                        }
                        SavePng(masterBmp, pngOut);
                    }

                    cacheDb[sheetHashKey] = computedSheetHash;
                }
            }
        }

        // 3. Write individual .tres Files
        foreach (var it in allCards)
        {
            var src = it.CanonicalItem ?? it;
            string targetDir = string.IsNullOrEmpty(it.RelSubDir) ? outRoot : Path.Combine(outRoot, it.RelSubDir);
            Directory.CreateDirectory(targetDir);
            string tresPath = Path.Combine(targetDir, $"{it.Name}.tres");
            expectedTres.Add(tresPath);

            string tres = $"[gd_resource type=\"AtlasTexture\" load_steps=2 format=3]\n\n[ext_resource type=\"Texture2D\" path=\"{src.AtlasPngRes}\" id=\"1_atlas\"]\n\n[resource]\natlas = ExtResource(\"1_atlas\")\nregion = Rect2({src.ArtX}, {src.ArtY}, {src.CardW}, {src.CardH})\n";

            if (!File.Exists(tresPath) || File.ReadAllText(tresPath) != tres)
            {
                File.WriteAllText(tresPath, tres);
            }
        }

        // Clean orphaned files
        int pruned = 0;
        foreach (var t in Directory.GetFiles(outRoot, "*.tres", SearchOption.AllDirectories))
        {
            if (!expectedTres.Contains(t))
            {
                try { File.Delete(t); pruned++; } catch { }
            }
        }
        foreach (var p in Directory.GetFiles(outRoot, $"{atlasName}*.png", SearchOption.TopDirectoryOnly))
        {
            if (!expectedPngs.Contains(p))
            {
                try { File.Delete(p); } catch { }
            }
        }

        SaveCache(sharedCacheFile, cacheDb);

        // Diagnostics & Optimization Report
        var closeRatios = uniqueCards.Where(x => x.Tier == MainTier.CloseRatio).ToList();
        var undersized = uniqueCards.Where(x => x.Tier == MainTier.Undersized).ToList();
        var oversized = uniqueCards.Where(x => x.Bucket == CardBucket.Oversized).ToList();
        var downscaled = uniqueCards.Where(x => x.WasDownscaled).ToList();

        if (duplicateCards.Count > 0 || closeRatios.Count > 0 || undersized.Count > 0 || oversized.Count > 0 || sizeViolations.Count > 0 || downscaled.Count > 0)
        {
            PrintColored("\n========================================================", ConsoleColor.Yellow);
            PrintColored("⚠️  Atlas Diagnostics & Optimization Report", ConsoleColor.Yellow);
            PrintColored("========================================================", ConsoleColor.Yellow);

            if (downscaled.Count > 0)
            {
                PrintColored($"\n🔍 High-Res Cards Downscaled to Standard {targetW}x{targetH} ({downscaled.Count} items):", ConsoleColor.Cyan);
                foreach (var d in downscaled)
                {
                    PrintColored("   ├─ ", ConsoleColor.DarkGray, false);
                    PrintColored(d.RelFilePath, ConsoleColor.White, false);
                    PrintColored($" [{d.OrigW}x{d.OrigH}] -> [{targetW}x{targetH}]", ConsoleColor.Green);
                }
            }

            if (sizeViolations.Count > 0)
            {
                PrintColored("\n🚨 GODOT TEXTURE SIZE WARNINGS:", ConsoleColor.Red);
                foreach (var v in sizeViolations)
                {
                    PrintColored("   ├─ " + v, ConsoleColor.Red);
                }
            }

            if (duplicateCards.Count > 0)
            {
                PrintColored($"\n🔗 Deduplicated Card Aliases ({duplicateCards.Count} aliases linked to existing sprites):", ConsoleColor.Cyan);
                foreach (var d in duplicateCards)
                {
                    PrintColored("   ├─ ", ConsoleColor.DarkGray, false);
                    PrintColored(d.RelFilePath, ConsoleColor.White, false);
                    PrintColored(" -> aliases ", ConsoleColor.DarkGray, false);
                    PrintColored(d.CanonicalItem.RelFilePath, ConsoleColor.Cyan);
                }
            }

            if (closeRatios.Count > 0)
            {
                PrintColored($"\n📐 Close Ratio Art (Middle Tier of Main Grids - Unscaled, Raw Pixels):", ConsoleColor.Cyan);
                foreach (var r in closeRatios)
                {
                    double inv = (double)r.OrigH / r.OrigW;
                    PrintColored("   ├─ ", ConsoleColor.DarkGray, false);
                    PrintColored(r.RelFilePath, ConsoleColor.White, false);
                    PrintColored($" [{r.OrigW}x{r.OrigH} (1:{inv:0.00})] - Cyan {targetW}x{targetH} Frame", ConsoleColor.Gray);
                }
            }

            if (undersized.Count > 0)
            {
                PrintColored($"\n📐 Undersized Art (Bottom Tier of Main Grids - Pinned to Top-Left):", ConsoleColor.Yellow);
                foreach (var inv in undersized)
                {
                    double actualInv = (double)inv.OrigH / inv.OrigW;
                    PrintColored("   ├─ ", ConsoleColor.DarkGray, false);
                    PrintColored(inv.RelFilePath, ConsoleColor.White, false);
                    PrintColored($" [{inv.OrigW}x{inv.OrigH} (1:{actualInv:0.00})] - Red {targetW}x{targetH} Frame", ConsoleColor.Red);
                }
            }

            if (oversized.Count > 0)
            {
                PrintColored($"\n📦 Oversized Images -> [{atlasName}_oversized.png]:", ConsoleColor.Magenta);
                PrintColored("   (Guides: Yellow = Outer Canvas Addition to reach 1:1.41)", ConsoleColor.DarkGray);
                foreach (var inv in oversized)
                {
                    double actualInv = (double)inv.OrigH / inv.OrigW;
                    ConsoleColor guideColor = inv.HasCorrectRatio ? ConsoleColor.Cyan : ConsoleColor.Red;
                    string shapeDesc = inv.ShapeCategory == OversizedShape.TooWide ? "Too Wide (Fat)" : (inv.ShapeCategory == OversizedShape.TooTall ? "Too Tall (Skinny)" : "Proportional");
                    PrintColored("   ├─ ", ConsoleColor.DarkGray, false);
                    PrintColored(inv.RelFilePath, ConsoleColor.White, false);
                    PrintColored($" [{inv.OrigW}x{inv.OrigH} (1:{actualInv:0.00})] - {shapeDesc}", guideColor);
                }
            }
        }

        PrintColored("\n========================================================", ConsoleColor.DarkCyan);
        PrintColored($"✨ Profile [{_profile.Name}] Complete!", ConsoleColor.Green);
        PrintColored("   ├─ 🖼️ Atlas Sheets : ", ConsoleColor.DarkGray, false); PrintColored(expectedPngs.Count + " total in atlas", ConsoleColor.White);
        PrintColored("   ├─ 📄 Total .tres  : ", ConsoleColor.DarkGray, false); PrintColored(allCards.Count + " generated", ConsoleColor.White);
        PrintColored("   ├─ 🧩 Unique Sprites: ", ConsoleColor.DarkGray, false); PrintColored(uniqueCards.Count + " packed", ConsoleColor.Cyan);
        if (duplicateCards.Count > 0)
        {
            PrintColored("   ├─ 🔗 Deduplicated : ", ConsoleColor.DarkGray, false); PrintColored(duplicateCards.Count + " shared rects", ConsoleColor.Green);
        }
        if (sizeViolations.Count > 0)
        {
            PrintColored($"   ├─ ⚠️ Size Warnings: ", ConsoleColor.DarkGray, false); PrintColored($"{sizeViolations.Count} textures exceed recommended limits!", ConsoleColor.Yellow);
        }
        if (pruned > 0)
        {
            PrintColored("   ├─ 🧹 Pruned       : ", ConsoleColor.DarkGray, false); PrintColored(pruned + " orphaned .tres files", ConsoleColor.Yellow);
        }
        PrintColored("========================================================\n", ConsoleColor.DarkCyan);

        return true;
    }

    private class OversizedPage
    {
        public List<CardItem> Items = new();
        public List<(string Title, int Y)> SectionLabels = new();
        public int SheetW;
        public int SheetH;
    }

    private static List<OversizedPage> PartitionOversizedPages(List<(string Title, List<CardItem> Cards)> shapeGroups, int maxTextureSize, int pad, int headerH)
    {
        var pages = new List<OversizedPage>();
        var currentPage = new OversizedPage();
        int curY = pad;
        int maxOccupiedW = 0;

        foreach (var sg in shapeGroups)
        {
            if (sg.Cards.Count == 0) continue;

            if (curY + headerH + pad > maxTextureSize && currentPage.Items.Count > 0)
            {
                currentPage.SheetW = Math.Max(maxOccupiedW, 1);
                currentPage.SheetH = curY + pad;
                pages.Add(currentPage);

                currentPage = new OversizedPage();
                curY = pad;
                maxOccupiedW = 0;
            }

            currentPage.SectionLabels.Add((sg.Title, curY));
            curY += headerH + pad;

            int curX = pad;
            int rowH = 0;

            foreach (var it in sg.Cards)
            {
                if ((curX + it.TotalCellW + pad) > maxTextureSize && curX > pad)
                {
                    curX = pad;
                    curY += rowH + pad;
                    rowH = 0;
                }

                if (curY + it.TotalCellH + pad > maxTextureSize && currentPage.Items.Count > 0)
                {
                    currentPage.SheetW = Math.Max(maxOccupiedW, 1);
                    currentPage.SheetH = curY + pad;
                    pages.Add(currentPage);

                    currentPage = new OversizedPage();
                    curY = pad;
                    maxOccupiedW = 0;
                    curX = pad;
                    rowH = 0;

                    currentPage.SectionLabels.Add((sg.Title + " (Cont.)", curY));
                    curY += headerH + pad;
                }

                it.CellX = curX;
                it.CellY = curY;

                int contentAreaH = it.TotalCellH - headerH;
                it.ArtX = curX + (it.TotalCellW - it.CardW) / 2;
                it.ArtY = curY + headerH + (contentAreaH - it.CardH) / 2;

                curX += it.TotalCellW + pad;
                if (curX > maxOccupiedW) maxOccupiedW = curX;
                if (it.TotalCellH > rowH) rowH = it.TotalCellH;
                currentPage.Items.Add(it);
            }

            curY += rowH + pad * 2;
        }

        if (currentPage.Items.Count > 0)
        {
            currentPage.SheetW = Math.Max(maxOccupiedW, 1);
            currentPage.SheetH = curY + pad;
            pages.Add(currentPage);
        }

        return pages;
    }

    private static string ComputeAtlasSheetHash(List<CardItem> items, int sheetW, int sheetH, CanvasPadding cPad, HeaderConfig hdr, bool isDummy)
    {
        using var sha = SHA256.Create();
        using var ms = new MemoryStream();

        byte[] meta = Encoding.UTF8.GetBytes($"sheet:{sheetW}x{sheetH}:pad:{cPad.Left},{cPad.Top},{cPad.Right},{cPad.Bottom},{cPad.ScaleWithResolution},{cPad.MismatchedRatioMode}:hdr:{hdr.Enabled},{hdr.Height},{hdr.FontSize}:dummy:{isDummy};");
        ms.Write(meta, 0, meta.Length);

        foreach (var it in items)
        {
            byte[] itemMeta = Encoding.UTF8.GetBytes($"{it.Name}:{it.ImageHash}:{it.CardW}x{it.CardH}:{it.AppliedPadding.Left},{it.AppliedPadding.Top}:{it.WasDownscaled};");
            ms.Write(itemMeta, 0, itemMeta.Length);
        }

        ms.Position = 0;
        return BitConverter.ToString(sha.ComputeHash(ms)).Replace("-", "").ToLower();
    }

    private static void ValidateSheetSize(string sheetName, int w, int h, int hardLimit, int warnLimit, List<string> violations)
    {
        long totalPixels = (long)w * h;
        if (totalPixels > GODOT_MAX_PIXELS)
        {
            violations.Add($"CRITICAL: [{sheetName}] ({w}x{h} = {totalPixels:N0} px) EXCEEDS Godot's max pixel limit (268,435,456 px). Texture will fail to load or allocate!");
        }

        if (w > hardLimit || h > hardLimit)
        {
            violations.Add($"CRITICAL: [{sheetName}] ({w}x{h} px) EXCEEDS Godot's max texture dimension ({hardLimit}px). Texture will fail to load or crash on many GPUs!");
        }
        else if (w > warnLimit || h > warnLimit)
        {
            violations.Add($"WARNING: [{sheetName}] ({w}x{h} px) exceeds mobile/compatibility limit ({warnLimit}px). May cause high VRAM usage or fail on low-end hardware.");
        }
    }

    private static string BuildHeaderText(CardItem it, HeaderConfig hdr)
    {
        var sb = new StringBuilder();
        if (it.WasDownscaled) sb.Append("[DOWNSCALED] ");
        else if (it.Tier == MainTier.Undersized) sb.Append("[UNDERSIZED] ");
        else if (it.Tier == MainTier.CloseRatio) sb.Append("[CLOSE RATIO] ");
        sb.Append(it.RelFilePath);

        var extras = new List<string>();
        if (hdr.IncludeDimensions) extras.Add($"{it.OrigW}x{it.OrigH}");
        if (hdr.IncludeAspectRatio)
        {
            double inv = (double)it.OrigH / it.OrigW;
            extras.Add($"1:{inv:0.00}");
        }

        if (extras.Count > 0) sb.Append($" ({string.Join(" | ", extras)})");
        return sb.ToString();
    }

    private static void SavePng(Bitmap bmp, string path)
    {
        using var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
        fs.Seek(0, SeekOrigin.Begin);
        bmp.Save(fs, ImageFormat.Png);
        fs.SetLength(fs.Position);
    }

    private static string ResolveGodotRes(string outPath)
    {
        string norm = outPath.Replace("\\", "/");
        int idx = norm.IndexOf("/res:/", StringComparison.OrdinalIgnoreCase);
        if (idx != -1) return "res://" + norm.Substring(idx + 6).TrimStart('/');
        string cur = Directory.GetCurrentDirectory().Replace("\\", "/");
        return norm.StartsWith(cur, StringComparison.OrdinalIgnoreCase) ? "res://" + norm.Substring(cur.Length).TrimStart('/') : $"res://{Path.GetFileName(outPath)}";
    }

    private static void PrintColored(string text, ConsoleColor color, bool newLine = true)
    {
        Console.ForegroundColor = color;
        if (newLine) Console.WriteLine(text);
        else Console.Write(text);
        Console.ResetColor();
    }
}