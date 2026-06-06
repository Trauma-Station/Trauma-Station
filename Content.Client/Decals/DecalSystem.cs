using Content.Client.Decals.Overlays;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using static Content.Shared.Decals.DecalGridComponent;

namespace Content.Client.Decals;

// Trauma - completely rewrote decals to be entity based
public sealed partial class DecalSystem : SharedDecalSystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private SpriteSystem _sprites = default!;

    private DecalOverlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new DecalOverlay(_sprites, EntityManager, PrototypeManager);
        _overlayManager.AddOverlay(_overlay);
    }

    public void ToggleOverlay()
    {
        if (_overlay == null)
            return;

        if (_overlayManager.HasOverlay<DecalOverlay>())
        {
            _overlayManager.RemoveOverlay(_overlay);
        }
        else
        {
            _overlayManager.AddOverlay(_overlay);
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_overlay == null)
            return;

        _overlayManager.RemoveOverlay(_overlay);
    }
}
