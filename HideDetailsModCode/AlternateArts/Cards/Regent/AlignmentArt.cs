using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards;

class AlignmentArt : AlternateCardArt<Alignment>
{
    internal static float AlignmentRotationDegrees => -15f;

    public override CardImg? Get(Alignment card) => null;

    public override void AfterNCardUpdateVisuals(Alignment card, NCard node, PileType pileType, CardPreviewMode previewMode)
    {
        node.RotateBy(AlignmentRotationDegrees);
    }

    public override void AfterNCardModelChanged(CardModel? previousModel, CardModel? newModel, NCard node)
    {
        if (previousModel is Alignment && newModel is not Alignment) node.ResetRotation();
    }
}
