using MegaCrit.Sts2.Core.HoverTips;

namespace BaseLib.Utils;

public interface IResolvingHoverTip : IHoverTip
{
    IHoverTip? ResolveHoverTip();

    int ResolveVersion { get; }
}