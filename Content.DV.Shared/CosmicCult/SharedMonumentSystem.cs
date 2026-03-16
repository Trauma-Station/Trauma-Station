using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.Examine;
using Robust.Shared.Physics.Events;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Shared._DV.CosmicCult;

public abstract class SharedMonumentSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MonumentSpawnMarkComponent, ExaminedEvent>(OnMonumentMarkExamined);
    }

    private void OnMonumentMarkExamined(Entity<MonumentSpawnMarkComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("cosmiccult-monument-approval-count", ("count", ent.Comp.ApprovingCultists.Count)));
        if (ent.Comp.ApprovingCultists.Contains(args.Examiner)) args.PushMarkup(Loc.GetString("cosmiccult-monument-approval-examine-present"));
        args.PushMarkup(Loc.GetString("cosmiccult-monument-approval-needed", ("count", ent.Comp.ApprovalsRequired - ent.Comp.ApprovingCultists.Count)));
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
}
