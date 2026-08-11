using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts;

// namespace HideDetailsMod.HideDetailsModCode.AlternateArts;

public abstract class IAlternateCardArt(double priority) : IComparable<IAlternateCardArt>
{
    // public static List<Type> GetAllSubtypes(Type type)
    // {
    //     var types = AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly());
    //     List<Type> concreteTypes = types.Where(t => !t.IsAbstract && !t.IsInterface).ToList();
    //     List<Type> inNamespace = types.Where(t => (t.Namespace ?? "").StartsWith("HideDetailsMod.HideDetailsModCode.AlternateArts.Cards")).ToList();
    //     List<Type> subclasses = inNamespace.Where(t =>
    //     {

    //         return t.IsSubclassOf(type);
    //     }).ToList();
    //     return concreteTypes;
    // }
    public static List<Type> GetDirectGenericSubtypes(Type openGenericBase)
    {
        return AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly())
            .Where(t => t.IsClass && !t.IsAbstract &&
                        t.BaseType is { IsGenericType: true } &&
                        t.BaseType.GetGenericTypeDefinition() == openGenericBase)
            .ToList();
    }

    // static IEnumerable<Type> GenericSubtypes => ReflectionHelper.GetSubtypesInMods(typeof(AlternateCardArt<>)).Where(t => !t.IsAbstract && !t.IsInterface);
    // static IEnumerable<Type> GenericSubtypes => ReflectionHelper.GetSubtypesFromAssembly(typeof(AlternateCardArt<>).Assembly, typeof(AlternateCardArt<>)).Where(t => !t.IsAbstract && !t.IsInterface);
    static List<Type> GenericSubtypes { get; } = GetDirectGenericSubtypes(typeof(AlternateCardArt<>));
    static List<IAlternateCardArt> GenericArts { get; } = GenericSubtypes.Select(type =>
    {
        try
        {
            return Activator.CreateInstance(type);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn("Failed to create instance of IAlternateCardArt: " + e);
            return null;
        }
    }).OfType<IAlternateCardArt>().ToList();

    static List<IAlternateCardArt> Arts { get; } = [
        // new OverrideArt(),
        ..GenericArts,
        new BaseArt(),
    ];
    public static IEnumerable<CardImg> GetAllArtsFor(CardModel card)
    => Arts
        .SelectMany(art => art.GetAllAndUpgraded(card))
        .Distinct()
        .Where(art => art.Exists());
    public static IEnumerable<CardImg> GetArtsFor(CardModel card)
    => Arts
        .Select(art => art.Get(card))
        .Distinct()
        .OfType<CardImg>()
        .Where(img => img.Exists());

    public IEnumerable<CardImg> GetAllAndUpgraded(CardModel card)
    {
        foreach (var item in GetAll(card))
        {
            yield return item;
            if (item.Upgraded() is var u && u.Exists()) yield return u;
        }
    }
    public abstract IEnumerable<CardImg> GetAll(CardModel card);
    public abstract CardImg? Get(CardModel card);
    protected double Priority { get; } = priority;
    public class Patch
    {
        internal static void AllPortraitPaths(CardModel card, ref IEnumerable<string> result)
        {
            result = GetAllArtsFor(card).Select(img => img.PortraitPath);
        }
        internal static void PortraitPath(CardModel card, ref string result)
        {
            var arts = GetArtsFor(card);
            if (arts.FirstOrDefault() is { } img)
            {
                result = img.PortraitPath;
                if (card.IsUpgraded && img.Upgraded() is var u && u.Exists()) result = u.PortraitPath;
            }
        }
        internal static void PortraitPngPath(CardModel card, ref string result)
        {
            var arts = GetArtsFor(card);
            if (arts.FirstOrDefault() is { } img)
            {
                result = img.PortraitPngPath;
                if (card.IsUpgraded && img.Upgraded() is var u && u.Exists()) result = u.PortraitPngPath;
            }
        }
    }
    public int CompareTo(IAlternateCardArt? other)
    {
        if (other is null) return 1;
        return Priority.CompareTo(other.Priority);
    }

    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdatePortrait))]
    static internal class PortraitContext
    {
        static public NCard? UpdatingNCard;
        static void Prefix(NCard __instance)
        {
            UpdatingNCard = __instance;
        }
        static void Postfix()
        {
            UpdatingNCard = null;
        }
    }

    protected static NetModSettings ConfigFrom(Player? player) => NetModSettings.GetPlayerConfig(player?.NetId) ?? new();
    protected static NetModSettings ConfigFrom(CardModel? card) => ConfigFrom(Util.GetOwner(card));

    protected NCard Node => PortraitContext.UpdatingNCard!;
    protected NInspectCardScreen? InspectionScreen => Node.GetAncestorOfType<NInspectCardScreen>();
    protected bool IsBeingInspected => InspectionScreen != null;
    protected NCardLibrary? CardLibrary => Node.GetAncestorOfType<NCardLibrary>();
    protected NCardRewardSelectionScreen? CardRewardScreen => Node.GetAncestorOfType<NCardRewardSelectionScreen>();
    protected bool IsInCardRewardScreen => CardRewardScreen != null;
    protected bool IsInShop => Node.GetAncestorOfType<NMerchantCard>() != null;
    protected bool IsInCardLibrary => CardLibrary != null;
}
