using BaseLib.Config;

namespace HideDetailsMod.HideDetailsModCode;

internal class MyModConfig : SimpleModConfig
{
    public static bool HideEnergy { get; set; } = true;
    public static bool HideStars { get; set; } = true;

    public static bool HideType { get; set; } = true;

    public static bool HideTitle { get; set; } = true;
    public static bool HideDescription { get; set; } = true;
    public static bool HideCardRewardRarityGlow { get; set; } = true;
    // public static int ExplosionSize { get; set; } = 80;
}
    
