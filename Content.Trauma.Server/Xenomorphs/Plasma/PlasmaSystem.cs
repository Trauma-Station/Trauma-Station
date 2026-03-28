// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Xenomorphs.Plasma;
using Content.Trauma.Shared.Xenomorphs.Plasma.Components;
using Content.Trauma.Shared.Xenomorphs.Stealth;
using Content.Trauma.Shared.Xenomorphs.Xenomorph;
using Content.Shared.Placeable;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Xenomorphs.Plasma;

public sealed class PlasmaSystem : SharedPlasmaSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly PhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlasmaGainModifierComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<PlasmaGainModifierComponent, ItemRemovedEvent>(OnItemRemoved);
    }

    private void OnItemPlaced(EntityUid uid, PlasmaGainModifierComponent component, ItemPlacedEvent args)
    {
        if (!TryComp<XenomorphComponent>(args.OtherEntity, out var xenomorph) || xenomorph.OnWeed)
            return;

        xenomorph.OnWeed = true;
    }

    private void OnItemRemoved(EntityUid uid, PlasmaGainModifierComponent component, ItemRemovedEvent args)
    {
        if (!TryComp<XenomorphComponent>(args.OtherEntity, out var xenomorph) || !xenomorph.OnWeed)
            return;

        foreach (var contact in _physics.GetContactingEntities(args.OtherEntity))
        {
            if (contact == uid || !HasComp<PlasmaGainModifierComponent>(contact))
                continue;

            return;
        }

        xenomorph.OnWeed = false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<PlasmaVesselComponent>();
        while (query.MoveNext(out var uid, out var plasmaVessel))
        {
            if (time < plasmaVessel.NextPointsAt)
                continue;

            plasmaVessel.NextPointsAt = time + TimeSpan.FromSeconds(1);

            var plasma = plasmaVessel.PlasmaPerSecondOffWeed;
            if (TryComp<XenomorphComponent>(uid, out var xenomorph) && xenomorph.OnWeed)
                plasma = plasmaVessel.PlasmaPerSecondOnWeed;

            if (TryComp<StealthOnWalkComponent>(uid, out var stealthOnWalk) && stealthOnWalk.Stealth)
                plasma -= stealthOnWalk.PlasmaCost;

            ChangePlasmaAmount(uid, plasma, plasmaVessel);
        }
    }
}
