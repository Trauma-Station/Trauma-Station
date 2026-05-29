using Content.Shared.Buckle;

namespace Content.Server.Traits.Assorted;

public sealed partial class BuckleOnMapInitSystem : EntitySystem
{
    [Dependency] private SharedBuckleSystem _buckleSystem = default!;
    [Dependency] private SharedTransformSystem _transform = default!; // Goobstation

    public override void Initialize()
    {
        SubscribeLocalEvent<BuckleOnMapInitComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, BuckleOnMapInitComponent component, MapInitEvent args)
    {
        var buckle = Spawn(component.Prototype, _transform.GetMapCoordinates(uid)); // Goob edit
        _buckleSystem.TryBuckle(uid, uid, buckle);
    }
}
