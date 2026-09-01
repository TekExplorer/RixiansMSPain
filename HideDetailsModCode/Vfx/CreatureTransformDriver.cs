using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace HideDetailsMod.HideDetailsModCode.Vfx;

public partial class CreatureTransformDriver : Node
{
#nullable disable
    private Node2D _body;
#nullable restore
    private Vector2 _basePosition;
    private Vector2 _baseScale;
    private float _baseRotation;

    private readonly List<NCreatureModifierVfx> _activeModifiers = [];

    public static CreatureTransformDriver? GetOrCreate(NCreatureVisuals visuals)
    {
        if (!GodotObject.IsInstanceValid(visuals)) return null;

        Node2D body = visuals.GetCurrentBody();
        if (!GodotObject.IsInstanceValid(body)) return null;

        CreatureTransformDriver? driver = body.GetNodeOrNull<CreatureTransformDriver>("TransformDriver");
        if (driver != null) return driver;

        driver = new CreatureTransformDriver
        {
            Name = "TransformDriver",
            _body = body,
            _basePosition = body.Position,
            _baseScale = body.Scale,
            _baseRotation = body.Rotation
        };

        body.AddChildSafely(driver);
        return driver;
    }

    public void Register(NCreatureModifierVfx modifier)
    {
        if (!GodotObject.IsInstanceValid(_body)) return;

        if (!_activeModifiers.Contains(modifier))
        {
            _activeModifiers.Add(modifier);
        }
    }

    public void Unregister(NCreatureModifierVfx modifier)
    {
        _activeModifiers.Remove(modifier);

        if (!GodotObject.IsInstanceValid(_body)) return;

        if (_activeModifiers.Count == 0)
        {
            ResetToBase();
        }
        else
        {
            UpdateCompositeTransform();
        }
    }

    public void UpdateCompositeTransform()
    {
        if (!GodotObject.IsInstanceValid(_body)) return;

        Vector2 compositeScale = _baseScale;
        Vector2 compositePosOffset = Vector2.Zero;
        float compositeRotOffset = 0f;

        // Multiply all scales, sum all offsets
        foreach (NCreatureModifierVfx mod in _activeModifiers)
        {
            if (!GodotObject.IsInstanceValid(mod)) continue;

            compositeScale.X *= mod.CurrentScaleMultiplier.X;
            compositeScale.Y *= mod.CurrentScaleMultiplier.Y;
            compositePosOffset += mod.CurrentPositionOffset;
            compositeRotOffset += mod.CurrentRotationOffset;
        }

        _body.Position = _basePosition + compositePosOffset;
        _body.Scale = compositeScale;
        _body.Rotation = _baseRotation + compositeRotOffset;
    }

    public void ResetToBase()
    {
        if (!GodotObject.IsInstanceValid(_body)) return;
        _body.Position = _basePosition;
        _body.Scale = _baseScale;
        _body.Rotation = _baseRotation;
    }

    /// <summary>
    /// Clears active modifiers on this creature, optionally matching a filter condition.
    /// </summary>
    public void ClearModifiers(Func<NCreatureModifierVfx, bool>? filter = null, bool animateRevert = true)
    {
        var snapshot = new List<NCreatureModifierVfx>(_activeModifiers);
        foreach (NCreatureModifierVfx mod in snapshot)
        {
            // If the driver node itself or the creature body is being freed, break out safely
            if (!GodotObject.IsInstanceValid(this) || !GodotObject.IsInstanceValid(_body)) break;
            if (!GodotObject.IsInstanceValid(mod)) continue;

            if (filter == null || filter(mod))
            {
                if (animateRevert)
                {
                    mod.Revert();
                }
                else
                {
                    mod.ForceKill();
                }
            }
        }
    }
}
