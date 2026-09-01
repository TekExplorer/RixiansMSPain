using System.Collections.Concurrent;
using System.Diagnostics;
using Godot;

namespace HideDetailsMod.HideDetailsModCode;

/// <summary>
/// Pre-warms backing atlas textures and proxy .tres resources in the background
/// for HideDetailsMod to eliminate runtime frame drops and GPU upload hitches.
/// </summary>
public static class Preload
{
    private static readonly ConcurrentBag<Resource> _pinnedResources = [];

    public const string DefaultModAtlasDir = "res://HideDetailsMod/images/atlases/";

    public static readonly string[] DefaultModSpriteDirs =
    {
        "res://HideDetailsMod/images/atlases/card_atlas.sprites/"
    };

    /// <summary>
    /// Executes full preloading asynchronously on a background task.
    /// Accepts multiple sprite directories or auto-detects all `.sprites/` folders if omitted.
    /// </summary>
    public static Task WarmUpAsync(
        string atlasBaseDir = DefaultModAtlasDir,
        IEnumerable<string>? spriteDirs = null)
    {
        return Task.Run(() => WarmUpSync(atlasBaseDir, spriteDirs));
    }

    /// <summary>
    /// Overload for params string[] convenience.
    /// </summary>
    public static Task WarmUpAsync(string atlasBaseDir, params string[] spriteDirs)
    {
        return Task.Run(() => WarmUpSync(atlasBaseDir, spriteDirs));
    }

    /// <summary>
    /// Executes full preloading synchronously.
    /// </summary>
    public static void WarmUpSync(
        string atlasBaseDir = DefaultModAtlasDir,
        IEnumerable<string>? spriteDirs = null)
    {
        var sw = Stopwatch.StartNew();
        int atlasCount = 0;
        int spriteCount = 0;

        GD.Print("[HideDetailsMod] 🚀 Starting asset preloading...");

        // 1. Discover and upload all backing atlas PNG sheets in the atlas folder to VRAM
        atlasCount = PreloadAllAtlasSheets(atlasBaseDir);

        // 2. Resolve sprite directories (explicitly provided, default, or auto-discovered)
        var targetDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (spriteDirs != null && spriteDirs.Any())
        {
            foreach (var d in spriteDirs)
            {
                if (!string.IsNullOrWhiteSpace(d)) targetDirs.Add(d);
            }
        }
        else
        {
            // Auto-discover all *.sprites/ folders inside the atlas base directory
            foreach (var autoDir in DiscoverSpriteDirectories(atlasBaseDir))
            {
                targetDirs.Add(autoDir);
            }

            // Fallback to default if nothing discovered
            if (targetDirs.Count == 0)
            {
                foreach (var d in DefaultModSpriteDirs) targetDirs.Add(d);
            }
        }

        // 3. Recursively warm up all individual .tres proxy sprites into Godot's cache
        foreach (string dirPath in targetDirs)
        {
            if (DirAccess.DirExistsAbsolute(dirPath))
            {
                spriteCount += PreloadDirectoryRecursive(dirPath);
            }
        }

        sw.Stop();
        GD.Print($"[HideDetailsMod] ✨ Preload complete in {sw.ElapsedMilliseconds} ms ({atlasCount} atlas sheets, {spriteCount} .tres sprites across {targetDirs.Count} directories cached).");
    }

    /// <summary>
    /// Auto-discovers any directories ending with `.sprites` inside the atlas root.
    /// </summary>
    private static List<string> DiscoverSpriteDirectories(string atlasBaseDir)
    {
        var found = new List<string>();
        if (!DirAccess.DirExistsAbsolute(atlasBaseDir))
        {
            return found;
        }

        using var dir = DirAccess.Open(atlasBaseDir);
        if (dir == null) return found;

        dir.ListDirBegin();
        string entry = dir.GetNext();

        while (!string.IsNullOrEmpty(entry))
        {
            if (dir.CurrentIsDir() && entry.EndsWith(".sprites", StringComparison.OrdinalIgnoreCase))
            {
                found.Add($"{atlasBaseDir.TrimEnd('/')}/{entry}/");
            }

            entry = dir.GetNext();
        }

        return found;
    }

    /// <summary>
    /// Scans the root atlas directory for any PNG sheets and uploads them to VRAM.
    /// </summary>
    private static int PreloadAllAtlasSheets(string atlasBaseDir)
    {
        if (!DirAccess.DirExistsAbsolute(atlasBaseDir))
        {
            return 0;
        }

        using var dir = DirAccess.Open(atlasBaseDir);
        if (dir == null) return 0;

        int loadedCount = 0;
        dir.ListDirBegin();
        string entry = dir.GetNext();

        while (!string.IsNullOrEmpty(entry))
        {
            if (!dir.CurrentIsDir() && entry.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                string fullPath = $"{atlasBaseDir.TrimEnd('/')}/{entry}";
                if (PreloadTexture(fullPath))
                {
                    loadedCount++;
                }
            }

            entry = dir.GetNext();
        }

        return loadedCount;
    }

    /// <summary>
    /// Preloads and pins a specific texture in memory/VRAM.
    /// </summary>
    public static bool PreloadTexture(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !ResourceLoader.Exists(path))
        {
            return false;
        }

        try
        {
            var tex = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
            if (tex != null)
            {
                _pinnedResources.Add(tex);
                return true;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[HideDetailsMod] Failed to preload texture '{path}': {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Recursively traverses a directory, caching all .tres files with CacheMode.Reuse.
    /// </summary>
    private static int PreloadDirectoryRecursive(string dirPath)
    {
        if (string.IsNullOrWhiteSpace(dirPath) || !DirAccess.DirExistsAbsolute(dirPath))
        {
            return 0;
        }

        using var dir = DirAccess.Open(dirPath);
        if (dir == null) return 0;

        int loadedCount = 0;
        dir.ListDirBegin();
        string entry = dir.GetNext();

        while (!string.IsNullOrEmpty(entry))
        {
            if (entry == "." || entry == ".." || entry == ".godot")
            {
                entry = dir.GetNext();
                continue;
            }

            string fullPath = $"{dirPath.TrimEnd('/')}/{entry}";

            if (dir.CurrentIsDir())
            {
                loadedCount += PreloadDirectoryRecursive(fullPath);
            }
            else if (entry.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var res = ResourceLoader.Load<Resource>(fullPath, null, ResourceLoader.CacheMode.Reuse);
                    if (res != null)
                    {
                        _pinnedResources.Add(res);
                        loadedCount++;
                    }
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[HideDetailsMod] Failed to preload resource '{fullPath}': {ex.Message}");
                }
            }

            entry = dir.GetNext();
        }

        return loadedCount;
    }

    public static void Clear()
    {
        _pinnedResources.Clear();
    }
}