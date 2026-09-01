using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.TestSupport;

namespace HideDetailsMod.HideDetailsModCode.Vfx;

public abstract partial class NCreatureModifierVfx : Node
{
    public enum DurationMode
    {
        Timed,
        UntilRevert,
        Permanent
    }

#nullable disable
    public NCreatureVisuals _creatureVisuals;
#nullable restore
    public DurationMode _mode = DurationMode.Timed;
    public float _duration = 0.4f;

    public Tween.EaseType _easeType = Tween.EaseType.Out;
    public Tween.TransitionType _transType = Tween.TransitionType.Quad;

    public Vector2 CurrentScaleMultiplier { get; set; } = Vector2.One;
    public Vector2 CurrentPositionOffset { get; set; } = Vector2.Zero;
    public float CurrentRotationOffset { get; set; } = 0f;

    protected CreatureTransformDriver? _driver;
    protected Tween? _tween;

    protected TaskCompletionSource<bool> _manualRevertTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected TaskCompletionSource<bool> _applyCompletedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected TaskCompletionSource<bool> _vfxCompletedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public CancellationToken _cancelToken;
    public CancellationTokenSource VfxCancellationToken { get; } = new();

    public Task ApplyTask => _applyCompletedTcs.Task;
    public Task VfxTask => _vfxCompletedTcs.Task;

    public static TVfx? Create<TVfx>(
        NCreatureVisuals creatureVisuals,
        float duration = 0.4f,
        DurationMode mode = DurationMode.Timed,
        Action<TVfx>? configure = null) where TVfx : NCreatureModifierVfx, new()
    {
        if (TestMode.IsOn || !GodotObject.IsInstanceValid(creatureVisuals))
        {
            return null;
        }

        TVfx vfx = new()
        {
            _creatureVisuals = creatureVisuals,
            _duration = duration,
            _mode = mode
        };

        configure?.Invoke(vfx);

        // Attach directly under the visuals node (does not touch body hierarchy)
        creatureVisuals.AddChildSafely(vfx);
        return vfx;
    }

    public override void _Ready()
    {
        _cancelToken = VfxCancellationToken.Token;
        _driver = CreatureTransformDriver.GetOrCreate(_creatureVisuals);
        _driver?.Register(this);

        TaskHelper.RunSafely(PlayVfx());
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        VfxCancellationToken.Cancel();
        _applyCompletedTcs.TrySetCanceled();
        _manualRevertTcs.TrySetCanceled();
        _vfxCompletedTcs.TrySetResult(true);

        _tween?.Kill();
        _driver?.Unregister(this);
    }

    private async Task PlayVfx()
    {
        try
        {
            if (_cancelToken.IsCancellationRequested) return;

            // 1. Forward deformation
            await PlayTweenSequence(0f, 1f, _duration * 0.5f);
            _applyCompletedTcs.TrySetResult(true);

            // 2. Lifecycle
            switch (_mode)
            {
                case DurationMode.Timed:
                    if (_cancelToken.IsCancellationRequested) return;
                    await PlayTweenSequence(1f, 0f, _duration * 0.5f);
                    break;

                case DurationMode.UntilRevert:
                    using (_cancelToken.Register(() => _manualRevertTcs.TrySetCanceled()))
                    {
                        await _manualRevertTcs.Task;
                    }
                    if (_cancelToken.IsCancellationRequested) return;
                    await PlayTweenSequence(1f, 0f, _duration * 0.5f);
                    break;

                case DurationMode.Permanent:
                    break;
            }
        }
        catch (Exception) when (_cancelToken.IsCancellationRequested) { }
        finally
        {
            _driver?.Unregister(this);
            _applyCompletedTcs.TrySetResult(true);
            _vfxCompletedTcs.TrySetResult(true);

            if (GodotObject.IsInstanceValid(this))
            {
                this.QueueFreeSafely();
            }
        }
    }

    protected abstract void ApplyProgress(float t);

    private async Task PlayTweenSequence(float from, float to, float time)
    {
        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenMethod(Callable.From<float>(progress =>
        {
            ApplyProgress(progress);
            _driver?.UpdateCompositeTransform();
        }), from, to, time).SetEase(_easeType).SetTrans(_transType);

        await _tween.AwaitFinished(this);
    }

    public void Revert()
    {
        _manualRevertTcs.TrySetResult(true);
    }

    public async Task RevertAsync()
    {
        Revert();
        await VfxTask;
    }

    public void ForceKill()
    {
        _tween?.Kill();
        _driver?.Unregister(this);
        this.QueueFreeSafely();
    }

    /// <summary>
    /// Convenience helper to clear all or filtered modifiers on a creature.
    /// </summary>
    public static void ClearAll(NCreatureVisuals visuals, Func<NCreatureModifierVfx, bool>? filter = null, bool animateRevert = true)
    {
        CreatureTransformDriver? driver = CreatureTransformDriver.GetOrCreate(visuals);
        driver?.ClearModifiers(filter, animateRevert);
    }
}