using BaseLib.Config;

namespace HideDetailsMod.HideDetailsModCode;

internal class MyModConfig : SimpleModConfig
{
    static public MyModConfig Init()
    {
        if (Instance == null)
        {
            var config = new MyModConfig();
            ModConfigRegistry.Register(MainFile.ModId, config);
            return config;
        }
        MainFile.Logger.Warn("Config was already initialized!");
        return Instance;
    }

#nullable disable
    static public MyModConfig Instance => ModConfigRegistry.Get<MyModConfig>();
#nullable restore

    public static bool ShouldPatchCardExporter { get; set; }

    [ConfigSection("Credits")] public static bool ShowCreditsTooltip { get; set; } = true;

    [ConfigSection("Art")]
    // TODO: Not done yet. doesn't reset full-art 
    [ConfigHideInUI] public static bool UseCustomArt { get; set; } = true;
    public static bool UseCustomCharSelect { get; set; } = true;

#if CANARY
    [ConfigSection("Canary only!")]
    // See NetUtils
    public static bool EmulateCanaryMode { get; set; } = true;

    public static bool UseBetaShivArt { get; set; } = false;
    
    public static bool UseBetaSoulArt { get; set; } = false;
#else
    [ConfigIgnore] public static bool EmulateCanaryMode => false;
    [ConfigIgnore] public static bool UseBetaShivArt => false;
    [ConfigIgnore] public static bool UseBetaSoulArt => false;
#endif

    // Renamed to InfiniteBladesLength2 to reset configs
    [ConfigSlider(0, 5000, 10)] public static float InfiniteBladesLength2 { get; set; } = 3000;

    [ConfigHideInUI] // TODO: Not yet useful
    public static bool EnableNeurosurgeYippe { get; set; } = true;
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
