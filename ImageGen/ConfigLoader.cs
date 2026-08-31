using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ImageGen;

public static class ConfigLoader
{
    public static (List<SingleAtlasProfile> Profiles, string ConfigPath) LoadProfiles(string explicitPath)
    {
        string path = !string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath)
            ? Path.GetFullPath(explicitPath)
            : new[] {
                "atlas_config.json",
                "atlas_groups.json",
                Path.Combine("ImageGen", "atlas_config.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "atlas_config.json")
            }.FirstOrDefault(File.Exists);

        if (path == null) return (new List<SingleAtlasProfile> { new() }, null);

        try
        {
            string json = File.ReadAllText(path);
            var root = JsonSerializer.Deserialize<MultiAtlasConfigFile>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (root?.Configs?.Count > 0) return (root.Configs, path);

            if (root != null)
            {
                return (new List<SingleAtlasProfile> {
                    new() {
                        Name = "default",
                        InputRoot = root.InputRoot,
                        OutputRoot = root.OutputRoot,
                        AtlasName = root.AtlasName ?? "card_atlas",
                        TargetWidth = root.TargetWidth,
                        TargetHeight = root.TargetHeight,
                        Padding = root.Padding,
                        MaxTextureSize = root.MaxTextureSize,
                        WarnAboveSize = root.WarnAboveSize,
                        DownscaleProportionalOversized = root.DownscaleProportionalOversized,
                        Force = root.Force,
                        CanvasPadding = root.CanvasPadding ?? new(),
                        Header = root.Header ?? new(),
                        DummyVariants = root.DummyVariants ?? new(),
                        Groups = root.Groups ?? new()
                    }
                }, path);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Config parse error: {ex.Message}");
        }

        return (new List<SingleAtlasProfile> { new() }, path);
    }

    public static string ResolveGroup(string relSubDir, SingleAtlasProfile profile)
    {
        string clean = relSubDir.Replace("\\", "/").Trim('/').ToLowerInvariant();
        string defaultGroup = clean.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "root";

        if (profile?.Groups == null) return defaultGroup;

        foreach (var (groupName, patterns) in profile.Groups)
        {
            if (patterns == null) continue;
            foreach (var p in patterns)
            {
                string cp = p.Replace("\\", "/").Trim('/').ToLowerInvariant();
                if (clean == cp || clean.StartsWith(cp + "/") || defaultGroup == cp) return groupName.ToLowerInvariant();
            }
        }

        return defaultGroup;
    }
}