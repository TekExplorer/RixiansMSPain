using System;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode;

public static class InfiniteInfiniteBlades
{
    public static readonly AddedNode<NCard, Control> Node = new(static (cardNode) =>
    {
        // Use a simple Control container to hold both blades
        Control container = new()
        {
            Visible = false,
            ZIndex = 0,
            ZAsRelative = true,
            // Base anchor position for the lightsaber hilts
            // Position = new Vector2(45, -200)
            Position = new Vector2(45, -180)
        };

        // const float maxBladeLength = 100.0f;;
        const float tilt1 = -0.005f; // Left blade tilts left
        const float tilt2 = 0.007f; // Right blade tilts right

        const float width = 3.2f;
        // Color color = new(0.0f, 0.9f, 0.1f, 0.9f); // Vibrant Lightsaber Green
        Color color = Color.FromHtml("00C800");

        // 1. Create a shared gradient config
        Gradient bladeGradient = new()
        {
            // Define two interpolation points (0.0 is the bottom start, 1.0 is the top end)
            Offsets = [0.0f, 0.1f],

            // Set the colors:
            Colors =
            [
                new Color(0.0f, 0.78f, 0.0f, 0.9f), // 0%  -> Completely Transparent Green
                Color.FromHtml("00C800") // 15% -> Full Solid Art Green (stays solid to the top)
            ]
        };

        // Create Left Blade
        Line2D leftBlade = new()
        {
            Width = width,
            Gradient = bladeGradient,
            Position = new Vector2(-8f, 0) // Shifted slightly left
        };

        // Create Right Blade
        Line2D rightBlade = new()
        {
            Width = width,
            Gradient = bladeGradient,
            Position = new Vector2(8.1f, 0) // Shifted slightly right
        };

        container.AddChild(leftBlade);
        container.AddChild(rightBlade);

        void updateDelegate()
        {
            if (!GodotObject.IsInstanceValid(cardNode) || !GodotObject.IsInstanceValid(container)) return;

            if (cardNode.Model is not InfiniteBlades || !MyModConfig.UseCustomArt)
            {
                container.Visible = false;
                return;
            }

            container.Visible = true;

            // Draw both lines pointing straight up relative to the card's position/rotation
            Vector2 localStart = Vector2.Zero;
            Vector2 localEnd1 = localStart + ((Vector2.Up + new Vector2(tilt1, 0)) * MyModConfig.InfiniteBladesLength2);
            Vector2 localEnd2 = localStart + ((Vector2.Up + new Vector2(tilt2, 0)) * MyModConfig.InfiniteBladesLength2);

            leftBlade.Points = [localStart, localEnd1];
            rightBlade.Points = [localStart, localEnd2];
        }
        container.External_Process(updateDelegate);

        return container;
    });

    // TODO: move to util
    static public void External_Process(this Node node, Action updateDelegate)
    {
        node.TreeEntered += () =>
            {
                if (!GodotObject.IsInstanceValid(node)) return;
                node.GetTree().ProcessFrame += updateDelegate;
            };

        node.TreeExiting += () =>
            {
                if (GodotObject.IsInstanceValid(node) && node.GetTree() != null)
                { node.GetTree().ProcessFrame -= updateDelegate; }
            };
    }
}
