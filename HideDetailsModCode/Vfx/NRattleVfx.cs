using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Random;

namespace HideDetailsMod.HideDetailsModCode.Vfx;
// NRattleVfx.cs
public partial class NRattleVfx : NCreatureModifierVfx
{
    public float _shakeIntensity = 14f;
    public float _rotationIntensity = 0.08f;

    public static NRattleVfx? Create(NCreatureVisuals visuals, float duration = 0.5f, DurationMode mode = DurationMode.Timed)
    {
        return Create<NRattleVfx>(visuals, duration, mode, vfx =>
        {
            vfx._transType = Tween.TransitionType.Linear;
        });
    }

    protected override void ApplyProgress(float t)
    {
        var rng = Rng.Chaotic;
        float currentShake = _shakeIntensity * t;
        CurrentPositionOffset = new Vector2(
            rng.NextFloat(-currentShake, currentShake),
            rng.NextFloat(-currentShake * 0.5f, currentShake * 0.5f)
        );
        CurrentRotationOffset = rng.NextFloat(-_rotationIntensity * t, _rotationIntensity * t);
    }
}