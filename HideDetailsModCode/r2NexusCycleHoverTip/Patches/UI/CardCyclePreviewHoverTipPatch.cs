using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace BaseLib.Patches.UI;

internal sealed class CardCyclePreviewState
{
    public Control Owner = null!;

    public List<CardCyclePreviewSlot> Slots = [];
}

internal sealed class CardCyclePreviewSlot
{
    public IResolvingHoverTip Tip = null!;

    public Control? Node;

    public int LastVersion;
}

internal static class CardCyclePreviewPatchStorage
{
    public static readonly Dictionary<NHoverTipSet, CardCyclePreviewState> Active = [];

    public static readonly Stack<CardCyclePreviewState> PendingCreates = [];
}

internal static class CardCyclePreviewPatchHelpers
{
    public static List<IHoverTip> ResolveAllForCycleSet(
        IEnumerable<IHoverTip> tips)
    {
        List<IHoverTip> result = [];

        foreach (IHoverTip tip in tips)
        {
            if (tip is IResolvingHoverTip resolvingTip)
            {
                IHoverTip? resolvedTip = resolvingTip.ResolveHoverTip();

                if (resolvedTip != null)
                    result.Add(resolvedTip);

                continue;
            }

            result.Add(tip);
        }

        return result;
    }

    public static void UpdateCycleSlot(CardCyclePreviewSlot slot)
    {
        if (slot.Node == null || !GodotObject.IsInstanceValid(slot.Node))
            return;

        IHoverTip? resolvedTip = slot.Tip.ResolveHoverTip();

        if (resolvedTip is not CardHoverTip cardHoverTip)
        {
            slot.Node.Visible = false;
            return;
        }

        slot.Node.Visible = true;

        NCard cardNode = slot.Node.GetNode<NCard>((NodePath)"%Card");

        cardNode.Model = cardHoverTip.Card;
        cardNode.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
    }
}

[HarmonyPatch(
    typeof(NHoverTipSet),
    nameof(NHoverTipSet.CreateAndShow),
    [
        typeof(Control),
        typeof(IEnumerable<IHoverTip>),
        typeof(HoverTipAlignment)
    ])]
public static class NHoverTipSetCreateAndShowCardCyclePreviewPatch
{
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(
        ref IEnumerable<IHoverTip> hoverTips,
        out CardCyclePreviewState? __state)
    {
        List<IHoverTip> originalTips = hoverTips.ToList();

        List<IResolvingHoverTip> cycleTips = originalTips
            .OfType<IResolvingHoverTip>()
            .ToList();

        if (cycleTips.Count == 0)
        {
            __state = null;
            hoverTips = originalTips;
            return;
        }

        __state = new CardCyclePreviewState
        {
            Slots = cycleTips
                .Select(tip => new CardCyclePreviewSlot
                {
                    Tip = tip,
                    LastVersion = tip.ResolveVersion
                })
                .ToList()
        };

        CardCyclePreviewPatchStorage.PendingCreates.Push(__state);

        hoverTips = CardCyclePreviewPatchHelpers.ResolveAllForCycleSet(
            originalTips);
    }

    private static void Postfix(
        NHoverTipSet? __result,
        CardCyclePreviewState? __state)
    {
        if (__state != null && CardCyclePreviewPatchStorage.PendingCreates.Count > 0)
        {
            CardCyclePreviewState pending = CardCyclePreviewPatchStorage.PendingCreates.Peek();

            if (ReferenceEquals(pending, __state))
                CardCyclePreviewPatchStorage.PendingCreates.Pop();
        }

        if (__result == null || __state == null)
            return;

        __state.Owner = __result._owner;

        if (__state.Slots.Any(slot => slot.Node != null))
            CardCyclePreviewPatchStorage.Active[__result] = __state;
    }
}

[HarmonyPatch(typeof(NHoverTipCardContainer), nameof(NHoverTipCardContainer.Add))]
public static class NHoverTipCardContainerAddCardCyclePreviewPatch
{
    private static void Postfix(
        NHoverTipCardContainer __instance,
        CardHoverTip cardTip)
    {
        if (cardTip is not ResolvedCardCycleHoverTip resolvedCycleTip)
            return;

        if (CardCyclePreviewPatchStorage.PendingCreates.Count == 0)
            return;

        CardCyclePreviewState state = CardCyclePreviewPatchStorage.PendingCreates.Peek();

        CardCyclePreviewSlot? slot = state.Slots.FirstOrDefault(existingSlot =>
            ReferenceEquals(existingSlot.Tip, resolvedCycleTip.Source)
            && existingSlot.Node == null);

        if (slot == null)
            return;

        Control? createdNode = __instance
            .GetChildren()
            .OfType<Control>()
            .LastOrDefault();

        if (createdNode == null)
            return;

        slot.Node = createdNode;
        slot.LastVersion = resolvedCycleTip.Source.ResolveVersion;
    }
}

[HarmonyPatch(typeof(NHoverTipSet), nameof(NHoverTipSet._Process))]
public static class NHoverTipSetProcessCardCyclePreviewPatch
{
    private static void Postfix(NHoverTipSet __instance)
    {
        if (!CardCyclePreviewPatchStorage.Active.TryGetValue(__instance, out CardCyclePreviewState? state))
            return;

        bool changed = false;

        foreach (CardCyclePreviewSlot slot in state.Slots)
        {
            int version = slot.Tip.ResolveVersion;

            if (version == slot.LastVersion)
                continue;

            slot.LastVersion = version;

            CardCyclePreviewPatchHelpers.UpdateCycleSlot(
                slot);

            changed = true;
        }

        if (!changed)
            return;

        __instance.CorrectVerticalOverflow();
        __instance.CorrectHorizontalOverflow();
    }
}

[HarmonyPatch(typeof(NHoverTipSet), nameof(NHoverTipSet.Remove))]
public static class NHoverTipSetRemoveCardCyclePreviewPatch
{
    private static void Prefix(Control owner)
    {
        if (NHoverTipSet._activeHoverTips.TryGetValue(owner, out NHoverTipSet? set))
            CardCyclePreviewPatchStorage.Active.Remove(set);
    }
}