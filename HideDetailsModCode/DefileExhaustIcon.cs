using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace HideDetailsMod.HideDetailsModCode;

[GlobalClass]
public partial class DefileExhaustIcon : Control
{
    static public readonly AddedNode<NExhaustPileButton, DefileExhaustIcon> Node = new("res://HideDetailsMod/images/defile_exhaust_icon.tscn", (button, rect) => rect.Button = button);

    public NExhaustPileButton? Button;
    public TextureRect? icon;
    public override void _Ready()
    {
        icon = GetNodeOrNull<TextureRect>("Icon");
        Button?.MoveChildSafely(this, Button._icon.GetIndex() + 1);
    }

    public override void _Process(double delta)
    {
        if (Button == null) return;
        // every frame
        var player = Button._localPlayer;
        if (player == null) return;
        CardPile? cardPile = CardPile.Get(PileType.Hand, player);
        if (cardPile == null) return;
        if (cardPile.Cards.Any(c => c is Defile))
        { ShowDefileExhaustIcon(); }
        else
        { HideDefileExhaustIcon(); }
    }

    internal void ShowDefileExhaustIcon()
    {
        if (Button == null) return;

        if (Button._icon.Visible) Button._icon.Visible = false;
        if (!Visible) Visible = true;

        if (!Button.Visible) Button.AnimIn();
    }
    internal void HideDefileExhaustIcon()
    {
        if (Button == null) return;

        // if (Button._currentCount == 0 && Button.Visible) Button.AnimOut();

        if (!Button._icon.Visible) Button._icon.Visible = true;
        if (Visible) Visible = false;
    }
}
