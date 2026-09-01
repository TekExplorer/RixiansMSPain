using MegaCrit.Sts2.Core.Models;

using System.Reflection;
using BaseLib.Utils;
using HarmonyLib;
using HideDetailsMod.HideDetailsModCode.AlternateArts;
using MegaCrit.Sts2.Core.Modding;

namespace HideDetailsMod.HideDetailsModCode.Patches;
// TODO: put in proper location
public static class HarmonyPatchHelpers
{
    /// <summary>
    /// Finds all implementations and overrides of the given MethodInfo across specified assemblies.
    /// </summary>
    /// <param name="baseMethod">The MethodInfo representing the target method signature and declaring type.</param>
    /// <param name="assemblies">Optional list of assemblies to search in. Defaults to all loaded non-dynamic assemblies.</param>
    /// <returns>An enumerable of MethodBase targets for [HarmonyTargetMethods].</returns>
    public static IEnumerable<MethodBase> GetMethodImplementations(
        MethodInfo baseMethod,
        IEnumerable<Assembly>? assemblies = null)
    {
        ArgumentNullException.ThrowIfNull(baseMethod);

        Type declaringType = baseMethod.DeclaringType
            ?? throw new ArgumentException("Base method must have a valid DeclaringType.", nameof(baseMethod));

        string methodName = baseMethod.Name;
        Type[] paramTypes = baseMethod.GetParameters().Select(p => p.ParameterType).ToArray();
        Type[]? genericArgs = baseMethod.IsGenericMethod ? baseMethod.GetGenericArguments() : null;

        // assemblies ??= AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic);
        // No need for searching unneeded places
        assemblies ??= ModManager.Mods.SelectMany(mod =>
        {
            // Beta Main Compatibility
            // TODO: remove later
            List<Assembly> Assemblies = [];
            var Old = typeof(Mod).Property("assembly");
            var New = typeof(Mod).Property("assemblies");
            if (New is not null && New.GetValue(mod) is IEnumerable<Assembly> assemblies) Assemblies.AddRange(assemblies);
            if (Old is not null && Old.GetValue(mod) is Assembly asm) Assemblies.Add(asm);
            return Assemblies;
        }).Prepend(typeof(ModManager).Assembly);
        foreach (var assembly in assemblies)
        {
            IEnumerable<Type> types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.OfType<Type>();
            }

            foreach (var type in types)
            {
                // Must be a subtype, implement the interface, or be the declaring type itself
                if (!declaringType.IsAssignableFrom(type))
                    continue;

                // Look for declared method matching the exact signature on this subtype
                MethodInfo method = AccessTools.DeclaredMethod(type, methodName, paramTypes, genericArgs);

                // Make sure the method exists on this class specifically and isn't abstract
                if (method != null && !method.IsAbstract)
                {
                    yield return method;
                }
            }
        }
    }
}
//TODO: optimize image overrides
[HarmonyPatch]
public static class ArtPatch
{
    [HarmonyPatch]
    public static class AllPortraitPaths
    {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            // Simply pass the target base method directly
            MethodInfo baseMethod = AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.AllPortraitPaths));
            return HarmonyPatchHelpers.GetMethodImplementations(baseMethod);
        }
        [HarmonyPostfix]
        internal static void PostFix(CardModel __instance, ref IEnumerable<string> __result)
        {
            if (!MyModConfig.UseCustomArt) return;
            try
            {
                AlternateCardArt.Patch.AllPortraitPaths(__instance, ref __result);
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"Error in AllPortraitPaths: {e}");
            }
        }
    }


    static private SpireField<CardModel, (CardImg? Base, CardImg? Upgraded)> OverrideImg = new SpireField<CardModel, (CardImg? Base, CardImg? Upgraded)>(() => (null, null)).CopyOnClone();
    static public void SetOverrideImage(this CardModel card, (CardImg? Base, CardImg? Upgraded) Override) => OverrideImg.Set(card, Override);
    static public (CardImg? Base, CardImg? Upgraded) GetOverrideImage(this CardModel card) => OverrideImg.Get(card);
    [HarmonyPatch]
    public static class PortraitPath
    {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            // Simply pass the target base method directly
            MethodInfo baseMethod = AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.PortraitPath));
            return HarmonyPatchHelpers.GetMethodImplementations(baseMethod);
        }


        [HarmonyPostfix]
        internal static void PostFix(CardModel __instance, ref string __result)
        {
            if (!MyModConfig.UseCustomArt) return;
            try
            {

                var (OverrideBase, OverrideUpgrade) = __instance.GetOverrideImage();
                var Override = __instance.IsUpgraded ? OverrideUpgrade : OverrideBase;
                if (Override != null && Override.Exists)
                {
                    __result = Override.PortraitPath;
                    return;
                }

                AlternateCardArt.Patch.PortraitPath(__instance, ref __result);
            }
            catch (Exception e)
            { MainFile.Logger.Error($"Error in PortraitPath: {e}"); }
        }

    }
    [HarmonyPatch]
    public static class PortraitPngPath
    {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            // Simply pass the target base method directly
            MethodInfo baseMethod = AccessTools.PropertyGetter(typeof(CardModel), "PortraitPngPath");
            return HarmonyPatchHelpers.GetMethodImplementations(baseMethod);
        }
        [HarmonyPostfix]
        internal static void PostFix(CardModel __instance, ref string __result)
        {
            if (!MyModConfig.UseCustomArt) return;
            if (__instance == null) return;
            try
            {
                var (OverrideBase, OverrideUpgrade) = __instance.GetOverrideImage();
                var Override = __instance.IsUpgraded ? OverrideUpgrade : OverrideBase;
                if (Override != null && Override.Exists)
                {
                    __result = Override.PortraitPngPath;
                    return;
                }

                AlternateCardArt.Patch.PortraitPngPath(__instance, ref __result);
            }
            catch (Exception e)
            { MainFile.Logger.Error($"Error in PortraitPngPath: {e}"); }
        }
    }
}
