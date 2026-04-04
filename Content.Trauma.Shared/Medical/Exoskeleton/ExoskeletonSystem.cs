// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Access.Systems;
using Content.Shared.Body;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Standing;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Medical.Exoskeleton;

/// <summary>
/// Prevents the entity from being injected with syringes altogether.
/// </summary>
public sealed class ExoskeletonSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private EntityQuery<InjectorComponent> _injectorQuery;

    public override void Initialize()
    {
        base.Initialize();

        _injectorQuery = GetEntityQuery<InjectorComponent>();

        SubscribeLocalEvent<BodyComponent, TargetBeforeInjectEvent>(OnBeforeInject);
    }

    private void OnBeforeInject(Entity<BodyComponent> ent, ref TargetBeforeInjectEvent args)
    {
        if (args.Cancelled
        || IsHypospray(args.UsedInjector) // Hyposprays use hypoport system instead
        || !HasComp<ExoskeletonComponent>(args.TargetGettingInjected))
            return;

        args.OverrideMessage = Loc.GetString("exoskeleton-inject-fail", ("target", Identity.Entity(args.TargetGettingInjected, EntityManager)));
        args.Cancel();
    }

    private bool IsHypospray(EntityUid uid) // Copypasted from HypoportSystem because uhh umm
    {
        var comp = _injectorQuery.Comp(uid);
        if (!_proto.Resolve(comp.ActiveModeProtoId, out var mode))
            return false; // invalid injector but not my problem

        // instant injection into mobs means hypospray
        return mode.DelayPerVolume == TimeSpan.Zero && mode.MobTime == TimeSpan.Zero;
    }
}
