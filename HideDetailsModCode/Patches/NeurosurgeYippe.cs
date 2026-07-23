using BaseLib.Patches.Content;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Random;

namespace HideDetailsMod.HideDetailsModCode.Patches;

[HarmonyPatch(typeof(Neurosurge), "OnPlay")]
class NeurosurgeYippe
{
    // HideDetailsMod/audio/{filename}
    // .wav or .ogg only
    static string[] Assets => [
        // "yipee.wav"
    ];
    static public void Prefix(CardPlay cardPlay)
    {
        if (!MyModConfig.EnableNeurosurgeYippe) return;
        if (Assets.Length == 0) return;
        var chosen = Assets.TakeRandom(1, new Rng(cardPlay.Card.Owner.PlayerRng.Seed));
        // TODO: mess with pitchVariation
        MainFile.Audio.PlaySfx(chosen.First() /*,pitchVariation: .3f*/);
    }
}