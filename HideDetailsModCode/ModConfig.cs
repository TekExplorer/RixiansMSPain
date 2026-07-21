using BaseLib.Config;

namespace HideDetailsMod.HideDetailsModCode;

internal class MyModConfig : SimpleModConfig
{
    [ConfigSection("Credits")] public static bool ShowCreditsTooltip { get; set; } = true;

    [ConfigSection("Art")]
    //
    [ConfigHideInUI]
    public static bool UseCustomArt { get; set; } = true;

    // Renamed to InfiniteBladesLength2 to reset configs
    [ConfigSlider(0, 5000, 10)] public static float InfiniteBladesLength2 { get; set; } = 3000;

    public static bool UseBetaShivArt { get; set; } = false;
    public static bool ClashAsGrandFinale { get; set; } = true;

    [ConfigSection("Card UI (Cheating)")]
    public static bool HideTitle { get; set; } = true;
    public static bool HideTitleBanner { get; set; } = true;
    public static bool MakeEverythingAncient { get; set; } = false;
    public static bool HideCardRewardRarityGlow { get; set; } = true;
    public static bool HideDescription { get; set; } = true;
    public static bool HideEnergy { get; set; } = true;
    public static bool HideStars { get; set; } = true;
    public static bool HideType { get; set; } = true;
    public static bool HideTooltips { get; set; } = true;

    [ConfigSection("Exclusions (Cheating)")]
    public static bool ExcludeFranticEscape { get; set; } = false;
}
