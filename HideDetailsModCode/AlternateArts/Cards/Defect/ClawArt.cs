using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

class ClawArt : AlternateCardArt<Claw>
{
    static CardImg[] All { get; } = [
        new("defect/claw_01"),
        new("defect/claw_02"),
        new("defect/claw_03"),
        new("defect/claw_04"),
        new("defect/claw_05"),
        new("defect/claw_06"),
        new("defect/claw_07"),
        new("defect/claw_08"),
        new("defect/claw_09"),
        new("defect/claw_10"),
        new("defect/claw_11"),
        new("defect/claw_12"),
        new("defect/claw_13"),
        new("defect/claw_14"),
        new("defect/claw_15"),
        new("defect/claw_16"),
        new("defect/claw_17"),
    ];
    static int Amount => All.Length;
    static CardImg? Get(int index)
    {
        if (index < 1 || index > Amount) return null;
        return All[index];
    }
    public override IEnumerable<CardImg> GetAll(Claw card) => All;
    public override CardImg? Get(Claw card) => Get(NumUpgrades[card]);

    static SpireField<Claw, int> NumUpgrades { get; } = new SpireField<Claw, int>(() => 0).CopyOnClone();
    [HarmonyPatch(typeof(Claw), nameof(Claw.BuffFromClawPlay))]
    static class ClawPatch
    {
        static void Prefix(Claw __instance)
        {
            NumUpgrades[__instance]++;
        }
    }
}