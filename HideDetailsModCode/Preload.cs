using System.Collections.Concurrent;
using System.Diagnostics;
using Godot;

namespace HideDetailsMod;

/// <summary>
/// Pre-warms backing atlas textures and proxy .tres resources in the background.
/// Zero-configuration: call Preload.Start() and Preload.Wait() with no parameters.
/// </summary>
public static class Preload
{
    private static readonly ConcurrentBag<Resource> _pinnedResources = [];
    private static Task? _activePreloadTask;
    private static readonly object _taskLock = new();

    public const string DefaultModAtlasDir = "res://HideDetailsMod/images/atlases/";

    /// <summary>
    /// Kicks off background preloading using all default paths and auto-discovery.
    /// Does not block. Call Wait() at the end of your initializer.
    /// </summary>
    public static void Start()
    {
        lock (_taskLock)
        {
            if (_activePreloadTask != null && !_activePreloadTask.IsCompleted)
            {
                return;
            }

            _activePreloadTask = Task.Run(WarmUpSync);
        }
    }

    /// <summary>
    /// Blocks the calling thread until background preloading completes.
    /// Safe to call even if Start() was not called or is already finished.
    /// </summary>
    public static bool Wait(int timeoutMs = -1)
    {
        Task? task;
        lock (_taskLock)
        {
            task = _activePreloadTask;
        }

        if (task == null || task.IsCompleted)
        {
            return true;
        }

        try
        {
            if (timeoutMs < 0)
            {
                task.GetAwaiter().GetResult();
                return true;
            }

            return task.Wait(timeoutMs);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[HideDetailsMod] Exception while waiting for preload: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Returns true if background preloading is currently active.
    /// </summary>
    public static bool IsRunning
    {
        get
        {
            lock (_taskLock)
            {
                return _activePreloadTask != null && !_activePreloadTask.IsCompleted;
            }
        }
    }

    /// <summary>
    /// Synchronously executes discovery, VRAM upload, and sprite caching.
    /// </summary>
    public static void WarmUpSync()
    {
        var sw = Stopwatch.StartNew();

        GD.Print("[HideDetailsMod] 🚀 Starting asset preloading...");

        // 1. Auto-discover and upload all backing atlas PNG sheets
        int atlasCount = PreloadAllAtlasSheets(DefaultModAtlasDir);

        // 2. Auto-discover all *.sprites/ folders inside the atlas directory
        var spriteDirs = DiscoverSpriteDirectories(DefaultModAtlasDir);
        int spriteCount = 0;

        foreach (string dirPath in spriteDirs)
        {
            spriteCount += PreloadDirectoryRecursive(dirPath);
        }

        sw.Stop();
        GD.Print($"[HideDetailsMod] ✨ Preload complete in {sw.ElapsedMilliseconds} ms ({atlasCount} atlas sheets, {spriteCount} .tres sprites across {spriteDirs.Count} directories cached).");
    }

    private static List<string> DiscoverSpriteDirectories(string atlasBaseDir)
    {
        var found = new List<string>();
        if (!DirAccess.DirExistsAbsolute(atlasBaseDir)) return found;

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

    private static int PreloadAllAtlasSheets(string atlasBaseDir)
    {
        if (!DirAccess.DirExistsAbsolute(atlasBaseDir)) return 0;

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