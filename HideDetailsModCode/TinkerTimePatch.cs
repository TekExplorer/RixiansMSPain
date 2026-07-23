using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;

namespace HideDetailsMod.HideDetailsModCode;

[HarmonyPatch(typeof(TinkerTime), nameof(TinkerTime.ChooseCardType))]
public static class TinkerTimePatch
{
    static readonly string Curious = "event/mad_science_power_curious";
    static readonly string Expertise = "event/mad_science_power_expertise";
    static readonly string Improvement = "event/mad_science_power_improvement";
    static readonly string[] powerArts = [Curious, Expertise, Improvement];
    static readonly SpireField<MadScience, TinkerTime.RiderEffect?> VisualRider = new(() => null);
    static public readonly ICardImgFactory AltArt = new CardImgFactory2<MadScience>([.. powerArts], static card =>
    {
        var rider = VisualRider[card] ?? card.TinkerTimeRider;
        // var index = CurrentTemporalIndex(3);

        if (rider == TinkerTime.RiderEffect.None && card.Type == CardType.Power)
        {
            // TODO: should cycle through the 3 options
            // return powerArts[index];
            MainFile.Logger.Warn("MadScience shouldn't be missing a Rider when displayed as a power!");
        }

        return rider switch
        {
            TinkerTime.RiderEffect.None => card.Type switch
            {
                CardType.None => null, // uh...
                CardType.Attack => null, // only one art
                CardType.Skill => null, // only one art
                // TODO: request "base" version
                CardType.Power => null, // powerArts[index],
                _ => null, // idk how you got weird versions of the card...
            },
            // default location for the rest
            TinkerTime.RiderEffect.Sapping => null,
            TinkerTime.RiderEffect.Violence => null,
            TinkerTime.RiderEffect.Choking => null,
            TinkerTime.RiderEffect.Energized => null,
            TinkerTime.RiderEffect.Wisdom => null,
            TinkerTime.RiderEffect.Chaos => null,
            TinkerTime.RiderEffect.Expertise => Expertise,
            TinkerTime.RiderEffect.Curious => Curious,
            TinkerTime.RiderEffect.Improvement => Improvement,
            _ => null,
        };
    });

    [HarmonyPatch]
    static class EventOptionsLocPatch
    {
        const string attackKey = "TINKER_TIME.pages.CHOOSE_CARD_TYPE.options.ATTACK";
        const string skillKey = "TINKER_TIME.pages.CHOOSE_CARD_TYPE.options.SKILL";
        const string powerKey = "TINKER_TIME.pages.CHOOSE_CARD_TYPE.options.POWER";

        [HarmonyPrefix, HarmonyPatch(typeof(EventModel), nameof(EventModel.GetOptionTitle))]
        public static bool GetOptionTitle(EventModel __instance, string key, LocString? __result)
        {
            if (__instance is not TinkerTime) return true;
            // return false;
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(EventModel), nameof(EventModel.GetOptionDescription))]
        public static bool GetOptionDescription(EventModel __instance, string key, LocString? __result)
        {
            if (__instance is not TinkerTime) return true;
            // TODO:EventChatter
            return true;
        }
    }
    [HarmonyPatch]
    static class TinkerTimeCyclingHoverTipPatch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            // 1. Locate the exact instance method info we want to intercept
            MethodInfo originalTarget = AccessTools.Method(typeof(TinkerTime), nameof(TinkerTime.GetCardTypeHoverTip));

            // 2. Locate our type-safe replacement method info
            MethodInfo replacement = AccessTools.Method(typeof(TinkerTimePatch), nameof(TinkerTimeHoverTipProvider));
            // var _ = new CodeMatcher();
            bool found = false;
            for (int i = 0; i < codes.Count; i++)
            {
                // Note: The IL text says 'call', but Harmony usually parses it matching the target's Metadata. 
                // Checking both ensures reliability regardless of optimization level.
                if ((codes[i].opcode == OpCodes.Call || codes[i].opcode == OpCodes.Callvirt) &&
                    codes[i].operand is MethodInfo mi && mi == originalTarget)
                {
                    // Convert the opcode to a standard static call and swap the target
                    codes[i].opcode = OpCodes.Call;
                    codes[i].operand = replacement;
                    found = true;
                }
            }

            if (!found)
            { MainFile.Logger.Warn("Warning: Transpiler could not find TinkerTime.GetCardTypeHoverTip calls!"); }

            return codes.AsEnumerable();
        }

        /// <summary>
        /// Fully type-safe provider.
        /// Static methods replacing instance methods must take the 'this' instance as the first argument.
        /// </summary>
        internal static IHoverTip TinkerTimeHoverTipProvider(TinkerTime tinkerTimeInstance, CardType cardType)
        {
            // You can now safely use autocomplete and type-checking here!
            switch (cardType)
            {
                case CardType.Attack:
                    break;
                case CardType.Skill:
                    break;
                case CardType.Power:
                    // TODO: Return your custom tip here or modify the output
                    var hover = ProduceTinkerTimeHover(tinkerTimeInstance, cardType);
                    if (hover != null) return hover;
                    break;
            }

            // Fallback: Safe, type-safe call back to the original game logic if needed
            return tinkerTimeInstance.GetCardTypeHoverTip(cardType);
        }
        static IHoverTip? ProduceTinkerTimeHover(TinkerTime tinkerTime, CardType cardType)
        {
            if (cardType != CardType.Power) return null;
            var owner = tinkerTime.Owner;
            if (owner == null) return null;

            MadScience NewCard(TinkerTime.RiderEffect VisualRiderEffect)
            {
                MadScience madScience = owner.RunState.CreateCard<MadScience>(owner);
                madScience.TinkerTimeType = cardType;
                madScience.TinkerTimeRider = TinkerTime.RiderEffect.None;
                VisualRider[madScience] = VisualRiderEffect;
                return madScience;
            }

            return CardCyclePreview.FromCards([
                NewCard(TinkerTime.RiderEffect.Expertise),
            NewCard(TinkerTime.RiderEffect.Curious),
            NewCard(TinkerTime.RiderEffect.Improvement)
            ], new()
            {
                RemoveDuplicateTypes = false,
                // TimePerCard = TimeSpan.FromSeconds(0.85),
            });
        }
    }
}
