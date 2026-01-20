using Content.Goobstation.Shared.Wraith.Components;
using Content.Goobstation.Shared.Wraith.Events;
using Content.Shared.Gibbing;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Mobs.Systems;

namespace Content.Goobstation.Shared.Wraith.Systems;

public sealed class RaiseSkeletonSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedRottingSystem _rotting = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RaiseSkeletonComponent, RaiseSkeletonEvent>(OnRaiseSkeleton);
    }

    private void OnRaiseSkeleton(Entity<RaiseSkeletonComponent> ent, ref RaiseSkeletonEvent args)
    {
        // check if we targeted a locker, early return and deploy skeleton if yes
        var coords = Transform(args.Target).Coordinates;
        if (TryComp<EntityStorageComponent>(args.Target, out var entStorage))
        {
            var skeleton = PredictedSpawnAtPosition(ent.Comp.SkeletonProto, coords);

            if (!_entityStorage.Insert(skeleton, args.Target, entStorage))
            {
                Del(skeleton);
                return;
            }

            args.Handled = true;
            return;
        }

        // otherwise, check if target is dead
        if (!_mobState.IsDead(args.Target))
        {
            _popup.PopupClient(Loc.GetString("wraith-raise-no-corpse"), ent.Owner, ent.Owner);
            return;
        }

        // or rotting
        if (!_rotting.IsRotten(args.Target))
        {
            _popup.PopupClient(Loc.GetString("wraith-raise-body-refuse"), ent.Owner, ent.Owner);
            return;
        }

        // since both conditions passed, deploy the skeleton and gib them
        PredictedSpawnAtPosition(ent.Comp.SkeletonProto, coords);
        _gibbing.Gib(args.Target);

        args.Handled = true;
    }
}
