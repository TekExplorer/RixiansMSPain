using System.Reflection;
using BaseLib.Utils;
using HarmonyLib;
using HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts;

[HarmonyPatch(typeof(NCard))]
static internal class NCardAwareCardsPatch
{
    extension(CardModel card)
    {
        public NCard? Node => AssociatedNCard[card];
        public NInspectCardScreen? InspectionScreen => card.Node?.GetAncestorOfType<NInspectCardScreen>();
        public bool IsBeingInspected => card.InspectionScreen != null;
        public NCardLibrary? CardLibrary => card.Node?.GetAncestorOfType<NCardLibrary>();
        public NCardRewardSelectionScreen? CardRewardScreen => card.Node?.GetAncestorOfType<NCardRewardSelectionScreen>();
        public bool IsInCardRewardScreen => card.CardRewardScreen != null;
        public bool IsInShop => card.Node?.GetAncestorOfType<NMerchantCard>() != null;
        public bool IsInCardLibrary => card.CardLibrary != null;
    }
    static public SpireField<CardModel, NCard?> AssociatedNCard = new(() => null);

    [HarmonyPrefix]
    [HarmonyPatch(nameof(NCard.SubscribeToModel))]
    static internal void SubscribeToModel(NCard __instance, CardModel? model)
    {
        if (model is not { } Model) return;
        AssociatedNCard[Model] = __instance;
    }
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NCard.UnsubscribeFromModel))]
    static internal void UnsubscribeFromModel(NCard __instance, CardModel? model)
    {
        if (model is not { } Model) return;
        if (AssociatedNCard[Model] == __instance) AssociatedNCard[Model] = null;
    }
}

public abstract class AlternateCardArt
{
    public static List<Type> GetDirectGenericSubtypes(Type openGenericBase)
    {
        var assemblies = Traverse.Create(MainFile.Mod).Property<List<Assembly>>("assemblies").Value;

        IEnumerable<Type> types;
        if (ModManager.State == ModManagerState.None)
        {
            types = ReflectionHelper.GetSubtypesFromAssembly(Assembly.GetExecutingAssembly(), typeof(AlternateCardArt));
        }
        else
        {
            types = ReflectionHelper.GetSubtypesInMods<AlternateCardArt>();
        }
        return types
            .Where(t => t.IsClass && !t.IsAbstract &&
                        t.BaseType is { IsGenericType: true } &&
                        t.BaseType.GetGenericTypeDefinition() == openGenericBase)
            .ToList();
    }

    // static IEnumerable<Type> GenericSubtypes => ReflectionHelper.GetSubtypesInMods(typeof(AlternateCardArt<>)).Where(t => !t.IsAbstract && !t.IsInterface);
    // static IEnumerable<Type> GenericSubtypes => ReflectionHelper.GetSubtypesFromAssembly(typeof(AlternateCardArt<>).Assembly, typeof(AlternateCardArt<>)).Where(t => !t.IsAbstract && !t.IsInterface);
    static List<Type> GenericSubtypes { get; } = GetDirectGenericSubtypes(typeof(AlternateCardArt<>));
    static List<AlternateCardArt> GenericArts { get; } = GenericSubtypes.Select(type =>
    {
        try
        {
            return Activator.CreateInstance(type);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Failed to create instance of {type}: {e}");
            return null;
        }
    }).OfType<AlternateCardArt>().ToList();

    static List<AlternateCardArt> _Arts { get; } = [
        // new OverrideArt(),
        ..GenericArts,
        new BaseArt(),
    ];
    static List<AlternateCardArt> Arts => MyModConfig.UseSimpleMode ? [new MadScienceArt(), new BaseArt()] : _Arts;
    static bool IsRestricted(CardModel card) => card.Pool switch
    {
        DefectCardPool when !MainFile.DefectSetActive => true,
        IroncladCardPool when !MainFile.IroncladSetActive => true,
        _ => false,
    };
    static IEnumerable<CardImg> GetAllArtsFor(CardModel card)
    => Arts
        .SelectMany(art => art.GetAllAndUpgraded(card))
        .Distinct()
        .Where(art => art.Exists);
    static IEnumerable<(CardImg? Base, CardImg? Upgraded)> GetArtsFor(CardModel card)
    => Arts
        .Select(art =>
        {
            try
            {
                return art.GetSplit(card);
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"Error in GetSplit for {card.Id.Entry}:\n{e}");
                return null;
            }
        })
        .Distinct()
        .OfType<(CardImg? Base, CardImg? Upgraded)>();

    IEnumerable<CardImg> GetAllAndUpgraded(CardModel card)
    {
        foreach (var item in GetAll(card))
        {
            yield return item;
            if (item.UpgradedIfExists() is { } u) yield return u;
        }
    }
    public abstract IEnumerable<CardImg> GetAll(CardModel card);
    public abstract CardImg? Get(CardModel card);
    public virtual (CardImg? Base, CardImg? Upgraded)? GetSplit(CardModel card)
    {
        var img = Get(card);
        var upgraded = img?.UpgradedIfExists();
        if (img is null && upgraded is null) return null;
        return (img, upgraded ?? img);
    }

    public class Patch
    {
        internal static void AllPortraitPaths(CardModel card, ref IEnumerable<string> result)
        {
            if (IsRestricted(card)) return;
            result = GetAllArtsFor(card).Select(img => img.PortraitPath);
        }
        internal static void PortraitPath(CardModel card, ref string result)
        {
            if (IsRestricted(card)) return;
            var arts = GetArtsFor(card);
            foreach ((var Base, var Upgraded) in arts)
            {
                if (card.IsUpgraded)
                {
                    if (Upgraded == null || !Upgraded.Exists) continue;
                    result = Upgraded.PortraitPath;
                    return;
                }
                else
                {
                    if (Base == null || !Base.Exists) continue;
                    result = Base.PortraitPath;
                    return;
                }
            }
        }
        internal static void PortraitPngPath(CardModel card, ref string result)
        {
            if (IsRestricted(card)) return;
            var arts = GetArtsFor(card);
            foreach ((var Base, var Upgraded) in arts)
            {
                if (card.IsUpgraded)
                {
                    if (Upgraded == null || !Upgraded.Exists) continue;
                    result = Upgraded.PortraitPngPath;
                }
                else
                {
                    if (Base == null || !Base.Exists) continue;
                    result = Base.PortraitPngPath;
                }
            }
        }
    }

    // [HarmonyPatch(typeof(NCard), nameof(NCard.UpdatePortrait))]
    static internal class PortraitContext
    {
        [HarmonyTargetMethods]
        static internal IEnumerable<MethodInfo> Methods()
        {
            yield return typeof(NCard).Method("UpdatePortrait") ?? typeof(NCard).Method("Reload");
        }
        static public NCard? UpdatingNCard;
        static internal void Prefix(NCard __instance)
        {
            UpdatingNCard = __instance;
        }
        static internal void Postfix()
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

public abstract class AlternateCardArt<T> : AlternateCardArt where T : CardModel
{
    protected AlternateCardArt()
    {
        _lazyImages = new Lazy<IEnumerable<CardImg>>(() =>
        {
            var t = Traverse.Create(this);
            IEnumerable<Traverse> properties = t.Properties().Select(prop => t.Property(prop));
            IEnumerable<Traverse> fields = t.Fields().Select(prop => t.Field(prop));
            return properties.Concat(fields)
                .Where(prop => prop.GetValueType() == typeof(CardImg))
                .Select(prop => prop.GetValue<CardImg?>())
                .OfType<CardImg>();

            // Type subclassType = GetType();
            // return subclassType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            //     .Where(p => p.PropertyType == typeof(CardImg) && p.DeclaringType == subclassType && p.CanRead)
            //     .Select(p => Traverse.Create(this).Property(p.Name).GetValue<CardImg>())
            //     .Where(val => val != null)
            //     .ToArray();
        });
    }
    private readonly Lazy<IEnumerable<CardImg>> _lazyImages;
    public virtual IEnumerable<CardImg> GetAll(T card) => _lazyImages.Value;
    public override IEnumerable<CardImg> GetAll(CardModel card)
    {
        if (card is T typed) return GetAll(typed);
        return [];
    }

    public abstract CardImg? Get(T card);
    protected virtual bool ShowIfCanonical => false;
    public override CardImg? Get(CardModel card)
    {
        if (card.IsCanonical && !ShowIfCanonical) return null;
        if (card is T typed) return Get(typed);
        return null;
    }

    public virtual (CardImg? Base, CardImg? Upgraded)? GetSplit(T card)
    {
        if (card.IsCanonical && !ShowIfCanonical) return null;
        return base.GetSplit(card);
    }

    public override (CardImg? Base, CardImg? Upgraded)? GetSplit(CardModel card)
    {
        if (card.IsCanonical && !ShowIfCanonical) return null;
        if (card is T typed) return GetSplit(typed);
        return base.GetSplit(card);
    }
}
