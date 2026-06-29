using BaseLib.Config;

namespace HideDetailsMod.HideDetailsModCode;

internal class MyModConfig : SimpleModConfig
{
    [ConfigSection("Card UI")] public static bool HideEnergy { get; set; } = true;
    public static bool HideStars { get; set; } = true;
    public static bool HideType { get; set; } = true;
    public static bool HideTitle { get; set; } = true;
    public static bool HideDescription { get; set; } = true;
    public static bool HideCardRewardRarityGlow { get; set; } = true;
    public static bool HideTooltips { get; set; } = true;

    // TODO: Until issues are fixed
    // [ConfigHideInUI]
    public static bool ShowCreditsTooltip { get; set; } = true;

    [ConfigSection("Exclusions")] public static bool ExcludeFranticEscape { get; set; } = false;
    [ConfigSection("Alt Art")] public static bool UseBetaShivArt { get; set; } = false;
}