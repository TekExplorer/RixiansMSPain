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

        var normPad = new NormalizedPadding(cPad, targetW, targetH);

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
        PrintColored("Padding % : ", ConsoleColor.DarkGray, false);
        PrintColored($"L:{normPad.LeftPct:P2}, T:{normPad.TopPct:P2}, R:{normPad.RightPct:P2}, B:{normPad.BottomPct:P2}", ConsoleColor.Magenta);

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

            PrintColored($"\n🔍 Processing and packing {files.Length} cards...", ConsoleColor.Cyan);
            var uniqueMap = new Dictionary<string, CardItem>();

            foreach (var f in files)
            {
                string parent = Path.GetDirectoryName(f);
                string relSub = parent.Length > inRoot.Length ? parent.Substring(inRoot.Length).TrimStart('\\', '/') : "";
                string relFile = f.Substring(inRoot.Length).TrimStart('\\', '/').Replace("\\", "/");

                var (rawBmp, fileHash) = ImageLoader.LoadNormalized(f);

                if (uniqueMap.TryGetValue(fileHash, out var canon))
                {
                    rawBmp.Dispose();
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

                var (paddedBmp, origW, origH, wasDownscaled, appliedPad, isStandardCell) = ImageProcessor.ProcessCard(
                    rawBmp, targetW, targetH, normPad, downscaleProp
                );
                rawBmp.Dispose();

                var item = new CardItem
                {
                    Name = Path.GetFileNameWithoutExtension(f),
                    RelSubDir = relSub,
                    RelFilePath = relFile,
                    GroupKey = ConfigLoader.ResolveGroup(relSub, _profile),
                    ImageHash = fileHash,
                    OrigW = origW,
                    OrigH = origH,
                    CardW = paddedBmp.Width,
                    CardH = paddedBmp.Height,
                    TotalCellW = paddedBmp.Width,
                    TotalCellH = paddedBmp.Height + headerH,
                    AppliedPadding = appliedPad,
                    HasCorrectRatio = isStandardCell,
                    WasDownscaled = wasDownscaled,
                    Bucket = isStandardCell ? CardBucket.Main : CardBucket.Oversized,
                    Tier = isStandardCell ? MainTier.Exact : MainTier.CloseRatio,
                    Bmp = paddedBmp
                };

                uniqueMap[fileHash] = item;
                allCards.Add(item);
            }
        }

        var uniqueCards = allCards.Where(x => x.CanonicalItem == null).ToList();
        string godotBase = ResolveGodotRes(outRoot);
        var expectedTres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var expectedPngs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sizeViolations = new List<string>();

        PrintColored("\n⚙️  Building Atlas Sheets (Standard First, Mismatched in Extra Space):", ConsoleColor.Cyan);

        int stdCellW = targetW + cPad.Left + cPad.Right;
        int stdCellH = targetH + cPad.Top + cPad.Bottom + headerH;

        foreach (var grp in uniqueCards.GroupBy(x => x.GroupKey))
        {
            // Sort standard cards first, followed by downscaled mismatched ones
            var allGroupItems = grp.OrderBy(x => x.Bucket == CardBucket.Main ? 0 : 1)
                                   .ThenBy(x => x.Name)
                                   .ToList();

            var pages = LayoutPackedPages(allGroupItems, stdCellW, stdCellH, maxTextureSize, pad, headerH);

            for (int pageIdx = 0; pageIdx < pages.Count; pageIdx++)
            {
                var page = pages[pageIdx];
                string pageSuffix = pages.Count > 1 ? $"_{pageIdx}" : "";
                string pngName = isDummyGenerator ? $"{atlasName}{pageSuffix}.png" : $"{atlasName}_{grp.Key}{pageSuffix}.png";
                string pngPath = Path.Combine(outRoot, pngName);
                string pngRes = $"{godotBase.TrimEnd('/')}/{pngName}";
                expectedPngs.Add(pngPath);

                string sheetHashKey = $"{_profile.Name}:{pngName}";
                string computedSheetHash = ComputeAtlasSheetHash(page.Items, page.SheetW, page.SheetH, cPad, hdr, isDummyGenerator);

                bool needsRebuild = force || !File.Exists(pngPath) || !cacheDb.TryGetValue(sheetHashKey, out var savedHash) || savedHash != computedSheetHash;

                foreach (var it in page.Items)
                {
                    it.AtlasPngRes = pngRes;
                }

                ValidateSheetSize(pngName, page.SheetW, page.SheetH, maxTextureSize, warnAboveSize, sizeViolations);

                if (!needsRebuild)
                {
                    PrintColored($"   ├─ ⚡ Up to date: [{grp.Key}{pageSuffix}] -> {pngName} ({page.Items.Count} items)", ConsoleColor.DarkGray);
                    foreach (var it in page.Items)
                    {
                        it.Bmp?.Dispose();
                    }
                    continue;
                }

                PrintColored($"   ├─ 🔨 Rendering [{grp.Key}{pageSuffix}]: ", ConsoleColor.Green, false);
                PrintColored($"{page.Items.Count} items -> {page.SheetW}x{page.SheetH} px", ConsoleColor.White);

                using (var bmp = new Bitmap(page.SheetW, page.SheetH, PixelFormat.Format32bppArgb))
                {
                    bmp.SetResolution(96, 96);
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.Transparent);
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.PixelOffsetMode = PixelOffsetMode.Half;
                        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                        using var font = new Font(hdr.FontFamily, hdr.FontSize, hdr.Bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
                        using var normalBrush = new SolidBrush(hdr.GetNormalColor());
                        using var gridPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1);

                        for (int i = 0; i < page.Items.Count; i++)
                        {
                            var it = page.Items[i];
                            g.DrawRectangle(gridPen, it.CellX, it.CellY, it.TotalCellW, it.TotalCellH);

                            if (hdr.Enabled && headerH > 0)
                            {
                                var textRect = new RectangleF(it.CellX + 2, it.CellY, it.TotalCellW - 4, headerH);
                                string text = BuildHeaderText(it, hdr);
                                g.DrawString(text, font, normalBrush, textRect, new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });
                            }

                            g.DrawImage(it.Bmp, new Rectangle(it.ArtX, it.ArtY, it.CardW, it.CardH), 0, 0, it.CardW, it.CardH, GraphicsUnit.Pixel);
                            it.Bmp.Dispose();
                        }
                    }
                    SavePng(bmp, pngPath);
                }

                cacheDb[sheetHashKey] = computedSheetHash;
            }
        }

        // Write individual .tres Files
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

    private class PackedPage
    {
        public List<CardItem> Items = new();
        public int SheetW;
        public int SheetH;
    }

    private static List<PackedPage> LayoutPackedPages(List<CardItem> items, int stdCellW, int stdCellH, int maxTextureSize, int pad, int headerH)
    {
        var pages = new List<PackedPage>();
        var currentPage = new PackedPage();

        // Calculate near-square layout columns for the standard items
        int standardCount = items.Count(x => x.Bucket == CardBucket.Main);
        double idealColRatio = Math.Sqrt((double)Math.Max(standardCount, items.Count) * stdCellH / stdCellW);
        int cols = Math.Clamp((int)Math.Round(idealColRatio), 1, Math.Max(1, (maxTextureSize - pad) / (stdCellW + pad)));

        int maxRowWidth = cols * (stdCellW + pad) + pad;
        maxRowWidth = Math.Min(maxRowWidth, maxTextureSize);

        int curX = pad;
        int curY = pad;
        int rowH = 0;
        int maxOccupiedW = 0;

        foreach (var it in items)
        {
            // If card overflows line, wrap to next row
            if (curX + it.TotalCellW + pad > maxRowWidth && curX > pad)
            {
                curX = pad;
                curY += rowH + pad;
                rowH = 0;
            }

            // If row overflows page height, start new page
            if (curY + it.TotalCellH + pad > maxTextureSize && currentPage.Items.Count > 0)
            {
                currentPage.SheetW = Math.Max(maxOccupiedW, 1);
                currentPage.SheetH = curY + pad;
                pages.Add(currentPage);

                currentPage = new PackedPage();
                curX = pad;
                curY = pad;
                rowH = 0;
                maxOccupiedW = 0;
            }

            it.CellX = curX;
            it.CellY = curY;
            it.ArtX = curX;
            it.ArtY = curY + headerH;

            curX += it.TotalCellW + pad;
            if (curX > maxOccupiedW) maxOccupiedW = curX;
            if (it.TotalCellH > rowH) rowH = it.TotalCellH;

            currentPage.Items.Add(it);
        }

        if (currentPage.Items.Count > 0)
        {
            currentPage.SheetW = Math.Max(maxOccupiedW, 1);
            currentPage.SheetH = curY + rowH + pad;
            pages.Add(currentPage);
        }

        return pages;
    }

    private static string ComputeAtlasSheetHash(List<CardItem> items, int sheetW, int sheetH, CanvasPadding cPad, HeaderConfig hdr, bool isDummy)
    {
        using var sha = SHA256.Create();
        using var ms = new MemoryStream();

        byte[] meta = Encoding.UTF8.GetBytes($"sheet:{sheetW}x{sheetH}:pad:{cPad.Left},{cPad.Top},{cPad.Right},{cPad.Bottom}:hdr:{hdr.Enabled},{hdr.Height},{hdr.FontSize}:dummy:{isDummy};");
        ms.Write(meta, 0, meta.Length);

        foreach (var it in items)
        {
            byte[] itemMeta = Encoding.UTF8.GetBytes($"{it.Name}:{it.ImageHash}:{it.CardW}x{it.CardH}:{it.WasDownscaled};");
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
        sb.Append(it.RelFilePath);
        if (hdr.IncludeDimensions) sb.Append($" ({it.OrigW}x{it.OrigH})");
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