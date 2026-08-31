using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode.Scenes;

public partial class InfiniteInfiniteBlades : Control
{
    public static readonly AddedNode<NCard, InfiniteInfiniteBlades> Node = new(static cardNode =>
    {
        InfiniteInfiniteBlades overlay = new() { CardNode = cardNode };
        return overlay;
    });
    private const float Tilt1 = -0.005f;
    private const float Tilt2 = 0.007f;
    private const float BladeWidth = 3.2f;

#nullable disable
    private NCard CardNode { get; set; }
    private Line2D LeftBlade { get; set; }
    private Line2D _rightBlade { get; set; }
#nullable restore

    public override void _Ready()
    {
        Visible = false;
        ZIndex = 0;
        ZAsRelative = true;
        Position = new Vector2(45f, -180f);

        Gradient bladeGradient = new()
        {
            Offsets = [0.0f, 0.1f],
            Colors =
            [
                new Color(0.0f, 0.78f, 0.0f, 0.9f),
                Color.FromHtml("00C800")
            ]
        };

        LeftBlade = new Line2D
        {
            Width = BladeWidth,
            Gradient = bladeGradient,
            Position = new Vector2(-8f, 0f)
        };

        _rightBlade = new Line2D
        {
            Width = BladeWidth,
            Gradient = bladeGradient,
            Position = new Vector2(8.1f, 0f)
        };

        AddChild(LeftBlade);
        AddChild(_rightBlade);
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
        _rightBlade.Points = [localStart, localEnd2];
    }
}