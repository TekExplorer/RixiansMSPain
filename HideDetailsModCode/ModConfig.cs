using BaseLib.Config;

namespace HideDetailsMod.HideDetailsModCode;

internal class MyModConfig : SimpleModConfig
{
    [ConfigSection("Credits")] public static bool ShowCreditsTooltip { get; set; } = true;

    [ConfigSection("Art")]
    [ConfigSlider(0, 2000, 10)]
    public static float InfiniteBladesLength { get; set; } = 2000;

    public static bool UseBetaShivArt { get; set; } = false;

    [ConfigSection("Card UI (Cheating)")] public static bool HideEnergy { get; set; } = true;
    public static bool HideStars { get; set; } = true;
    public static bool HideType { get; set; } = true;
    public static bool HideTitle { get; set; } = true;
    public static bool HideDescription { get; set; } = true;
    public static bool HideCardRewardRarityGlow { get; set; } = true;
    public static bool HideTooltips { get; set; } = true;

    [ConfigSection("Exclusions (Cheating)")]
    public static bool ExcludeFranticEscape { get; set; } = false;
}
