using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.Actions;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Shared._DV.CosmicCult;

public abstract class SharedMonumentSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedCosmicCultSystem _cosmicCult = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MonumentCollisionComponent, PreventCollideEvent>(OnPreventCollide);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<MonumentTransformingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.EndTime)
                continue;
            _appearance.SetData(uid, MonumentVisuals.Transforming, false);
            RemComp<MonumentTransformingComponent>(uid);
        }
    }

    /// <summary>
    /// Ensures that Cultists can't walk through The Monument and allows non-cultists to walk through the space.
    /// </summary>
    private void OnPreventCollide(EntityUid uid, MonumentCollisionComponent comp, ref PreventCollideEvent args)
    {
        if (!_cosmicCult.EntitySeesCult(args.OtherEntity) && !comp.HasCollision)
            args.Cancelled = true;
    }
}
