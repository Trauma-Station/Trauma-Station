using Robust.Client.Graphics;

namespace Content.Trauma.Client.Weapons.Ranged;

public sealed class BulletholeVisualizerSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlayManager.AddOverlay(new BulletholeOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay<BulletholeOverlay>();
    }
}
