using System.Reflection;
using System.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace HideDetailsMod.HideDetailsModCode;

public static class StringExtensions
{
    /// <summary>
    /// Indents every line of a string block by a specified number of spaces.
    /// </summary>
    public static string ToIndentedString(this string input, int spaces)
    {
        if (string.IsNullOrEmpty(input)) return input;

        string indent = new(' ', spaces);

        // Prepend indent to the first line, then inject it after every line ending
        return indent + input.ReplaceLineEndings("\n" + indent);
    }
}

internal class MSPainNetConfigCmd : AbstractConsoleCmd
{
    public override string CmdName => "mspainnetconfigs";

    public override string Args => "";

    public override string Description => "Shows what other MSPain users are using";

    public override bool IsNetworked => false;

    public override bool DebugOnly => false;//!MainFile.IsCanary;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        var players = GetPlayers();
        if (players == null)
        {
            if (LocalContext.NetId is { } id) return new(true, Write(id, GetPlayerName(id), new()));
            else return new(true, You());
        }
        List<string> contents = [];
        foreach (var (NetId, Username) in players)
        {
            contents.Add(Write(NetId, Username));
        }
        return new(true, string.Join('\n', contents));
    }

    private static string You()
    {
        return "You:\n" + new NetModSettings().ToString().ToIndentedString(4);
    }

    static string Write(ulong NetId, string Username, NetModSettings? config = null)
    {
        if (NetId == 1) return You();
        var builder = new StringBuilder();
        builder.AppendLine($"{NetId} ({Username})");
        config ??= NetModSettings.GetPlayerConfig(NetId);
        if (config is { } settings) builder.AppendLine(settings.ToString().ToIndentedString(4));
        else builder.AppendLine("Does not have the mod".ToIndentedString(4));
        return builder.ToString();
    }
    public static IEnumerable<(ulong NetId, string Username)>? GetPlayers() =>
        RunManager.Instance.State?.Players.Select(p => (p.NetId, GetPlayerName(p.NetId)));
    public static string GetPlayerName(ulong NetId) => PlatformUtil.GetPlayerNameRaw(RunManager.Instance.NetService.Platform, NetId);
}

public readonly struct NetModSettings
{
    // 1. ADD NEW TOGGLES HERE
    public bool Canary => EnabledFlags.Contains("C");
    public bool BetaShiv => Canary && EnabledFlags.Contains("Shiv");
    public bool BetaSoul => EnabledFlags.Contains("Soul");

    // Constructor that builds automatically from the local config settings
    public NetModSettings()
    {
        if (MainFile.IsCanary) EnabledFlags.Add("C");
        if (MyModConfig.UseBetaShivArt) EnabledFlags.Add("Shiv");
        if (MyModConfig.UseBetaSoulArt) EnabledFlags.Add("Soul");
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{nameof(NetModSettings)} {{");

        // Dynamically fetch all public instance properties
        var properties = typeof(NetModSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            // Filter out the collection property
            if (prop.Name == nameof(EnabledFlags))
                continue;

            var value = prop.GetValue(this);

            // Format lower-case boolean strings for clarity
            string formattedValue = value is bool b ? b.ToString().ToLower() : value?.ToString() ?? "null";

            sb.AppendLine($"  {prop.Name} = {formattedValue},");
        }

        sb.Append('}');
        return sb.ToString();
    }

    // Container for holding the synchronized string flags
    public HashSet<string> EnabledFlags { get; } = [];

    // Constructor to build from a raw token set (used for remote players)
    internal NetModSettings(IEnumerable<string> enabledFlags)
    {
        EnabledFlags.UnionWith(enabledFlags);
    }


    // Automatically bundles active configs into your mod list string
    public static string BuildPackedString()
    {
        var localSettings = new NetModSettings();
        return localSettings.EnabledFlags.Count > 0
            ? ":" + string.Join(":", localSettings.EnabledFlags)
            : "";
    }

    public static string? GetPlayerModString(ulong? NetId)
    {
        if (NetId is not { } id) return null;
        if (!NetModSettingsPatch.GameInfos.TryGetValue(id, out var msg)) return null;

        var modStr = msg.versionInfo.otherMods?.FirstOrDefault(m => m.StartsWith(MainFile.ModId + "-"));
        if (string.IsNullOrEmpty(modStr)) return null;
        return modStr;
    }
    public static NetModSettings? GetPlayerConfig(Player? player) => GetPlayerConfig(player?.NetId);

    public static NetModSettings? GetPlayerConfig(ulong? NetId)
    {
        if (NetId == null) return null;
        if (NetId is not { } id) return null;
        var modStr = GetPlayerModString(id); // TODO: weird cast. check it.
        if (string.IsNullOrEmpty(modStr)) return null;

        // Automatically slices everything after the mod name into string tokens
        var tokens = modStr.Split(':').Skip(1).ToHashSet();
        return new NetModSettings(tokens);
    }
}

[HarmonyPatch(typeof(PeerVersionInfo), nameof(PeerVersionInfo.LocalDefault))]
static class NetModSettingsPatch
{
    static void Postfix(List<string>? ___otherMods)
    {
        var mods = ___otherMods;
        if (mods == null) return;

        var index = mods.FindIndex(m => m.StartsWith(MainFile.ModId + "-"));
        if (index != -1)
        {
            var packed = NetModSettings.BuildPackedString();
            mods[index] += packed;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.NetService), MethodType.Setter)]
    static void SetupNetService(INetGameService value)
    {
        value.RegisterMessageHandler<InitialGameInfoMessage>((msg, id) => GameInfos[id] = msg);
        value.Disconnected += _ => GameInfos.Clear();
    }

    internal static readonly Dictionary<ulong, InitialGameInfoMessage> GameInfos = [];
}

