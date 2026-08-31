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

namespace ImageGen;

public class AtlasPacker
{
    private readonly SingleAtlasProfile _profile;
    private readonly string _baseDir;

    public AtlasPacker(SingleAtlasProfile profile, string baseDir)
    {
        _profile = profile;
        _baseDir = baseDir;
    }

    public bool Execute()
    {
        string inRoot = _profile.InputRoot;
        string outRoot = _profile.OutputRoot;
        string atlasName = _profile.AtlasName ?? "card_atlas";
        int targetW = _profile.TargetWidth ?? 606;
        int targetH = _profile.TargetHeight ?? 852;
        int pad = _profile.Padding ?? 8;
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
        if (!isDummyGenerator) PrintColored("Source    : ", ConsoleColor.DarkGray, false); PrintColored(inRoot ?? "(Procedural Dummies)", ConsoleColor.White);
        PrintColored("Target    : ", ConsoleColor.DarkGray, false); PrintColored(outRoot, ConsoleColor.White);
        PrintColored("Standard  : ", ConsoleColor.DarkGray, false);
        PrintColored($"{targetW}x{targetH} px (Aspect Ratio 1:{targetInvRatio:0.00})", ConsoleColor.Yellow);
        if (cPad.TotalW > 0 || cPad.TotalH > 0)
        {
            PrintColored("Base Pad  : ", ConsoleColor.DarkGray, false);
            PrintColored($"+L:{cPad.Left}, +T:{cPad.Top}, +R:{cPad.Right}, +B:{cPad.Bottom} px (Scaled: {cPad.ScaleWithResolution}, Mismatched: {cPad.MismatchedRatioMode})", ConsoleColor.Magenta);
        }

        var allCards = new List<CardItem>();

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

            string cacheFile = Path.Combine(outRoot, $"cache_{_profile.Name}.sha256");
            string hash;
            using (var sha = SHA256.Create())
            {
                using var ms = new MemoryStream();
                foreach (var f in files)
                {
                    byte[] nb = Encoding.UTF8.GetBytes(f + ":");
                    ms.Write(nb, 0, nb.Length);
                    byte[] fb = File.ReadAllBytes(f);
                    ms.Write(fb, 0, fb.Length);
                }
                ms.Write(Encoding.UTF8.GetBytes($"pad:{cPad.Left},{cPad.Top},{cPad.Right},{cPad.Bottom},{cPad.ScaleWithResolution},{cPad.MismatchedRatioMode}:hdr:{hdr.Enabled},{hdr.Height}"));
                ms.Position = 0;
                hash = BitConverter.ToString(sha.ComputeHash(ms)).Replace("-", "").ToLower();
            }

            var existingTres = Directory.GetFiles(outRoot, "*.tres", SearchOption.AllDirectories);
            var existingPngs = Directory.GetFiles(outRoot, $"{atlasName}*.png", SearchOption.TopDirectoryOnly);

            if (!force && File.Exists(cacheFile) && existingPngs.Length > 0 && existingTres.Length > 0)
            {
                if (File.ReadAllText(cacheFile).Trim() == hash)
                {
                    PrintColored($"\n⚡ Up to date: Profile '{_profile.Name}' has no modifications ({files.Length} cards).", ConsoleColor.Green);
                    return true;
                }
            }

            PrintColored($"\n🔍 Scanning, hashing, scaling padding, and classifying {files.Length} cards...", ConsoleColor.Cyan);
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
                    allCards.Add(new CardItem
                    {
                        Name = Path.GetFileNameWithoutExtension(f),
                        RelSubDir = relSub,
                        RelFilePath = relFile,
                        GroupKey = ConfigLoader.ResolveGroup(relSub, _profile),
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
                        HasCorrectRatio = canon.HasCorrectRatio
                    });
                    continue;
                }

                using var raw = new Bitmap(new MemoryStream(bytes));
                double ratio = (double)raw.Width / raw.Height;
                bool isRatioMatch = Math.Abs(ratio - targetRatio) < 0.015;

                // Dynamically calculate proportional padding based on resolution & ratio
                var appliedPad = cPad.ComputeFor(raw.Width, raw.Height, targetW, targetH, isRatioMatch);

                int paddedW = raw.Width + appliedPad.TotalW;
                int paddedH = raw.Height + appliedPad.TotalH;

                var item = new CardItem
                {
                    Name = Path.GetFileNameWithoutExtension(f),
                    RelSubDir = relSub,
                    RelFilePath = relFile,
                    GroupKey = ConfigLoader.ResolveGroup(relSub, _profile),
                    OrigW = raw.Width,
                    OrigH = raw.Height,
                    CardW = paddedW,
                    CardH = paddedH,
                    AppliedPadding = appliedPad,
                    HasCorrectRatio = isRatioMatch
                };

                item.Bmp = new Bitmap(paddedW, paddedH, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(item.Bmp))
                {
                    g.Clear(Color.Transparent);
                    g.DrawImage(raw, appliedPad.Left, appliedPad.Top, raw.Width, raw.Height);
                }

                if (raw.Width == targetW && raw.Height == targetH)
                {
                    item.Bucket = CardBucket.Main;
                    item.Tier = MainTier.Exact;
                }
                else if ((long)raw.Width * raw.Height < standardArea)
                {
                    item.Bucket = CardBucket.Main;
                    item.Tier = isRatioMatch ? MainTier.CloseRatio : MainTier.Undersized;
                }
                else if (isRatioMatch)
                {
                    item.Bucket = CardBucket.Main;
                    item.Tier = MainTier.CloseRatio;
                }
                else
                {
                    item.Bucket = CardBucket.Oversized;
                    item.ShapeCategory = ratio > targetRatio ? OversizedShape.TooWide : OversizedShape.TooTall;
                }

                if (item.Bucket == CardBucket.Main)
                {
                    item.TotalCellW = Math.Max(targetW + appliedPad.TotalW, item.CardW);
                    item.TotalCellH = Math.Max(targetH + appliedPad.TotalH, item.CardH) + headerH;
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

        PrintColored("\n⚙️  Building Atlas Sheets:", ConsoleColor.Cyan);

        // 1. Render Main Grids
        foreach (var grp in uniqueCards.Where(x => x.Bucket == CardBucket.Main).GroupBy(x => x.GroupKey))
        {
            var items = grp.OrderBy(x => (int)x.Tier).ThenBy(x => x.Name).ToList();
            string pngName = isDummyGenerator ? $"{atlasName}.png" : $"{atlasName}_{grp.Key}.png";
            string pngPath = Path.Combine(outRoot, pngName);
            string pngRes = $"{godotBase.TrimEnd('/')}/{pngName}";
            expectedPngs.Add(pngPath);

            int maxCardW = items.Max(x => x.TotalCellW);
            int maxCardH = items.Max(x => x.TotalCellH);

            int cols = Math.Max(1, Math.Min(items.Count, 8192 / (maxCardW + pad)));
            int rows = (int)Math.Ceiling((double)items.Count / cols);

            int sheetW = cols * (maxCardW + pad) + pad;
            int sheetH = rows * (maxCardH + pad) + pad;

            PrintColored($"   ├─ Atlas Sheet [{grp.Key}]: ", ConsoleColor.Green, false);
            PrintColored($"{items.Count} items ({cols}x{rows} grid) -> {sheetW}x{sheetH} px", ConsoleColor.White);

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
                        it.CellX = pad + (i % cols) * (maxCardW + pad);
                        it.CellY = pad + (i / cols) * (maxCardH + pad);
                        it.ArtX = it.CellX;
                        it.ArtY = it.CellY + headerH;
                        it.AtlasPngRes = pngRes;

                        g.DrawRectangle(gridPen, it.CellX, it.CellY, it.TotalCellW, it.TotalCellH);

                        if (hdr.Enabled && headerH > 0)
                        {
                            var textRect = new RectangleF(it.CellX + 2, it.CellY, it.TotalCellW - 4, headerH);
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
        }

        // 2. Render Shared Oversized Atlas
        var oversizedItems = uniqueCards.Where(x => x.Bucket == CardBucket.Oversized).ToList();
        if (oversizedItems.Count > 0)
        {
            string atlasFileName = $"{atlasName}_oversized.png";
            string pngOut = Path.Combine(outRoot, atlasFileName);
            string atlasPngRes = $"{godotBase.TrimEnd('/')}/{atlasFileName}";
            expectedPngs.Add(pngOut);

            var shapeGroups = new List<(string Title, List<CardItem> Cards)> {
                ("Proportional (Valid Ratio 1:1.41)", oversizedItems.Where(x => x.ShapeCategory == OversizedShape.Proportional).OrderByDescending(x => (long)x.CardW * x.CardH).ToList()),
                ("Too Wide / Fat (Needs Top/Bottom Extension)", oversizedItems.Where(x => x.ShapeCategory == OversizedShape.TooWide).OrderByDescending(x => (long)x.CardW * x.CardH).ToList()),
                ("Too Tall / Skinny (Needs Left/Right Extension)", oversizedItems.Where(x => x.ShapeCategory == OversizedShape.TooTall).OrderByDescending(x => (long)x.CardW * x.CardH).ToList())
            };

            int maxSheetWidth = 8192;
            int curY = pad;
            int maxOccupiedW = 0;
            var sectionLabels = new List<(string Title, int Y)>();

            foreach (var sg in shapeGroups)
            {
                if (sg.Cards.Count == 0) continue;

                sectionLabels.Add((sg.Title, curY));
                curY += headerH + pad;

                int curX = pad;
                int rowH = 0;

                foreach (var it in sg.Cards)
                {
                    if ((curX + it.TotalCellW + pad) > maxSheetWidth && curX > pad)
                    {
                        curX = pad;
                        curY += rowH + pad;
                        rowH = 0;
                    }

                    it.CellX = curX;
                    it.CellY = curY;

                    int contentAreaH = it.TotalCellH - headerH;
                    it.ArtX = curX + (it.TotalCellW - it.CardW) / 2;
                    it.ArtY = curY + headerH + (contentAreaH - it.CardH) / 2;
                    it.AtlasPngRes = atlasPngRes;

                    curX += it.TotalCellW + pad;
                    if (curX > maxOccupiedW) maxOccupiedW = curX;
                    if (it.TotalCellH > rowH) rowH = it.TotalCellH;
                }

                curY += rowH + pad * 2;
            }

            int sheetW = Math.Max(maxOccupiedW, 1);
            int sheetH = curY + pad;

            PrintColored($"   ├─ Shared Oversized Atlas: ", ConsoleColor.Magenta, false);
            PrintColored($"{oversizedItems.Count} cards -> {sheetW}x{sheetH} px", ConsoleColor.White);

            using (var masterBmp = new Bitmap(sheetW, sheetH, PixelFormat.Format32bppArgb))
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

                    foreach (var sl in sectionLabels)
                    {
                        g.DrawString($"=== {sl.Title.ToUpper()} ===", sectionFont, sectionBrush, pad, sl.Y);
                    }

                    foreach (var it in oversizedItems)
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
            File.WriteAllText(tresPath, tres);
        }

        PrintColored($"\n✨ Profile [{_profile.Name}] Complete: {allCards.Count} .tres created.", ConsoleColor.Green);
        return true;
    }

    private static string BuildHeaderText(CardItem it, HeaderConfig hdr)
    {
        var sb = new StringBuilder();
        if (it.Tier == MainTier.Undersized) sb.Append("[UNDERSIZED] ");
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