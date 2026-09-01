using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode.Scenes;

public partial class InfiniteInfiniteBlades : Control
{
    public static readonly AddedNode<NCard, InfiniteInfiniteBlades> Node = new(static cardNode =>
    {
        var inf = new InfiniteInfiniteBlades() { CardNode = cardNode };
        cardNode.Body.AddChildSafely(inf);
        cardNode.Body.MoveChildSafely(inf, cardNode._portraitCanvasGroup.GetIndex() + 1);

        return inf;
    });
    private const float Tilt1 = -0.006f;
    private const float Tilt2 = 0.007f;
    private const float BladeWidth = 3.2f;

#nullable disable
    private NCard CardNode { get; set; }
    private Line2D LeftBlade { get; set; }
    private Line2D RightBlade { get; set; }
#nullable restore

    public override void _Ready()
    {
        Visible = false;
        ZIndex = 0;
        ZAsRelative = true;
        Position = new Vector2(46.5f, -185f);

        Gradient bladeGradient = new()
        {
            Offsets = [0.0f, 0.1f],
            Colors =
            [
                new Color(0.0f, 0.78f, 0.0f, 0.9f),
                Color.FromHtml("#2ec70f")
            ]
        };

        LeftBlade = new Line2D
        {
            Width = BladeWidth,
            Gradient = bladeGradient,
            Position = new Vector2(-8.1f, 0f)
        };

        RightBlade = new Line2D
        {
            Width = BladeWidth,
            Gradient = bladeGradient,
            Position = new Vector2(8.1f, 0f)
        };

        AddChild(LeftBlade);
        AddChild(RightBlade);
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(CardNode))
        {
            Visible = false;
            return;
        }

        if (CardNode.Model is not InfiniteBlades || !MyModConfig.UseCustomArt)
        {
            Visible = false;
            return;
        }

        Visible = true;

        Vector2 localStart = Vector2.Zero;
        float bladeLength = MyModConfig.InfiniteBladesLength2;

        Vector2 localEnd1 = localStart + ((Vector2.Up + new Vector2(Tilt1, 0f)) * bladeLength);
        Vector2 localEnd2 = localStart + ((Vector2.Up + new Vector2(Tilt2, 0f)) * bladeLength);

        LeftBlade.Points = [localStart, localEnd1];
        RightBlade.Points = [localStart, localEnd2];
    }
}