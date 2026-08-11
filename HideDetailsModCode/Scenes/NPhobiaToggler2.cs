using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;

namespace HideDetailsMod.HideDetailsModCode.Scenes;

/// <summary>
/// Will start/stop the animation from an AnimationPlayer depending on if Phobia Mode is on
/// Used in instances where the animation can make people uncomfortable (ie the Infection card overlay)
/// Copied from <see cref="MegaCrit.Sts2.Core.Nodes.Animation.NPhobiaAnimationToggler"/>
/// </summary>
partial class NPhobiaToggler2 : Node
{
    [Export]
    public AnimationPlayer? _animationPlayer;

    public override void _Ready()
    {
        base._Ready();
        UpdatePhobiaMode();
    }

    public override void _EnterTree()
    {
        NGame.Instance?.Connect(NGame.SignalName.PhobiaModeToggled, Callable.From(UpdatePhobiaMode));
    }

    public override void _ExitTree()
    {
        NGame.Instance?.Disconnect(NGame.SignalName.PhobiaModeToggled, Callable.From(UpdatePhobiaMode));
    }

    public void UpdatePhobiaMode()
    {
        if (_animationPlayer != null)
        {
            _animationPlayer.Active = !SaveManager.Instance.PrefsSave.PhobiaMode;
        }
    }
}
