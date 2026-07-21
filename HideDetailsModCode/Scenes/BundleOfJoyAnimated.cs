// using BaseLib.Utils;
// using Godot;
// using MegaCrit.Sts2.Core.Models.Cards;
// using MegaCrit.Sts2.Core.Nodes.Cards;

// namespace HideDetailsMod.HideDetailsModCode.Scenes;

// partial class BundleOfJoyAnimated : Control
// {
//     static internal AddedNode<NCard, BundleOfJoyAnimated> _ = new("res://HideDetailsMod/scenes/cards/bundle_of_joy.tscn", (card, animation) => animation.card = card);
//     // res://HideDetailsMod/scenes/cards/bundle_of_joy.tscn
//     NCard? card;
//     AnimatedSprite2D? animation;
//     public override void _Ready()
//     {
//         animation = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
//     }

//     public override void _Process(double delta)
//     {
//         Visible = card?.Model is BundleOfJoy;
//     }
// }
