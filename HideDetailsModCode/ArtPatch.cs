using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HideDetailsMod.HideDetailsModCode;

public partial class AlternateArts
{
    [HarmonyPatch]
    public class ArtPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CardModel), nameof(CardModel.AllPortraitPaths), MethodType.Getter)]
        [HarmonyPatch(typeof(MadScience), nameof(CardModel.AllPortraitPaths), MethodType.Getter)]
        public static void AllPortraitPaths(CardModel? __instance, ref IEnumerable<string>? __result)
        {
            var card = __instance;
            if (card == null || __result == null) return;
            if (!MyModConfig.UseCustomArt) return;
            try
            {
                List<string> result = [.. __result];

                var found = GetAltsFor(card);

                result.AddRange(found.SelectMany(alt => alt.All).Select(Img => Img.PortraitPath));

                var upgraded = CardImg.Upgraded(card);
                if (upgraded.Exists()) result.Add(upgraded.PortraitPath);
                __result = result;
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"Error in AllPortraitPaths: {e}");
            }
        }

        static CardImg ImgFor(CardModel card)
        {
            var factories = GetAltsFor(card);
            foreach (var factory in factories)
            {
                var img = factory.Get(card);
                if (img == null) continue;
                if (card.IsUpgraded && img.Upgraded().Exists()) img = img.Upgraded();
                return img;
            }
            var Img = new CardImg(card);
            if (card.IsUpgraded && Img.Upgraded().Exists()) Img = Img.Upgraded();
            return Img;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CardModel), nameof(CardModel.PortraitPath), MethodType.Getter)]
        [HarmonyPatch(typeof(MadScience), nameof(CardModel.PortraitPath), MethodType.Getter)]
        public static void PortraitPath(CardModel? __instance, ref string __result)
        {
            if (!MyModConfig.UseCustomArt) return;
            if (__instance == null) return;
            try
            {
                var Img = ImgFor(__instance);
                if (Img != null) __result = Img.PortraitPath;
            }
            catch (Exception e)
            { MainFile.Logger.Error($"Error in PortraitPath: {e}"); }
        }

        // PortraitPngPath
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CardModel), "PortraitPngPath", MethodType.Getter)]
        // [HarmonyPatch(typeof(MadScience), "PortraitPngPath", MethodType.Getter)] // doesn't exist lol
        public static void PortraitPngPath(CardModel? __instance, ref string __result)
        {
            if (!MyModConfig.UseCustomArt) return;
            if (__instance == null) return;
            try
            {
                var Img = ImgFor(__instance);
                if (Img != null) __result = Img.PortraitPngPath;
            }
            catch (Exception e)
            { MainFile.Logger.Error($"Error in PortraitPngPath: {e}"); }
        }
    }
}