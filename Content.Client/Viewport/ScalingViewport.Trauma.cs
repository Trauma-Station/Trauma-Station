using Content.Trauma.Common.MouseWheel;
using Robust.Client.UserInterface;

namespace Content.Client.Viewport;

public sealed partial class ScalingViewport
{
    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);

        if (args.Handled || MathHelper.CloseToPercent(0f, args.Delta.Y))
            return;

        _entityManager.SystemOrNull<CommonMouseWheelSystem>()?.HandleMouseWheel(args.Delta);
    }
}
