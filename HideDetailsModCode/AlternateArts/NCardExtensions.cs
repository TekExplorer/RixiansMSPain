using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;

static class NCardExtensions
{
    internal class ColorBox(Color color) { public Color color = color; }
    static readonly NotNullSpireField<NCard, ColorBox> NCardSparklesColor = new(card => new(card._sparkles.Modulate));

    extension(NCard node)
    {
        public void HideRarityGlow()
        {
            node._rareGlow?.Visible = false;
            node._uncommonGlow?.Visible = false;
        }
        public void RemoveRarityGlow()
        {
            node.KillRarityGlow();

            if (node._rareGlow is { } rareGlow)
            {
                node.RemoveChildSafely(rareGlow);
                rareGlow.QueueFree();
                node._rareGlow = null;
            }

            if (node._uncommonGlow is { } uncommonGlow)
            {
                node.RemoveChildSafely(uncommonGlow);
                uncommonGlow.QueueFree();
                node._uncommonGlow = null;
            }
        }

        public NCardUncommonGlow? AssertUncommonGlow()
        {
            if (node._uncommonGlow is { } existing)
            {
                existing.Visible = true;
                return existing;
            }
            var glow = node._uncommonGlow = NCardUncommonGlow.Create();
            node.MoveBodyChildToBackSafely(glow);
            return glow;
        }
        public NCardRareGlow? AssertRareGlow()
        {
            if (node._rareGlow is { } existing)
            {
                existing.Visible = true;
                return existing;
            }
            var glow = node._rareGlow = NCardRareGlow.Create();
            node.MoveBodyChildToBackSafely(glow);
            return glow;
        }
        private void MoveBodyChildToBackSafely(Node? glow)
        {
            if (glow != null && GodotObject.IsInstanceValid(node.Body))
            {
                node.Body.AddChildSafely(glow);
                node.Body.MoveChildSafely(glow, 1);
            }
        }
        public void ResetSparklesColor()
        {
            node._sparkles.Modulate = NCardSparklesColor[node].color;
        }
        public void ResetSparkles()
        {
            node._sparkles.Visible = false;
            node.ResetSparklesColor();
        }

    }

    static internal NotNullSpireField<NCard, NCardRotation> RotatedBy = new(() => new());
    internal class NCardRotation { public float Value { get; set; } = 0; }
    extension(NCard node)
    {
        public void RotateBy(float degrees)
        {
            node.RotationDegrees += degrees;
            RotatedBy[node].Value += degrees;
        }
        public void ResetRotation()
        {
            node.RotationDegrees -= RotatedBy[node].Value;
            RotatedBy[node].Value = 0;
        }
        public void SetRotationTo(float degrees)
        {
            node.RotateBy(degrees - RotatedBy[node].Value);
        }
    }
}
