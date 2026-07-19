using System;

namespace BaseLib.Utils;

/// <summary>
/// Options for card cycle preview hover tips.
/// </summary>
public sealed record CardCyclePreviewOptions
{
    public static CardCyclePreviewOptions Default { get; } = new();

    /// <summary>
    /// How long each card is shown before the preview advances.
    /// </summary>
    public TimeSpan TimePerCard { get; init; } = TimeSpan.FromSeconds(0.85);

    /// <summary>
    /// Prevents accidentally previewing a huge card pool.
    /// </summary>
    public int MaxCards { get; init; } = 32;

    /// <summary>
    /// Removes duplicate card model types from the cycle.
    /// </summary>
    public bool RemoveDuplicateTypes { get; init; } = true;

    /// <summary>
    /// Whether the previewed cards should be shown upgraded.
    /// </summary>
    public bool Upgrade { get; init; } = false;
}