// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Vampires.Gargantua;

public sealed partial class GargantuaChargingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GargantuaChargingComponent, StartCollideEvent>(OnCollide);

        SubscribeLocalEvent<GargantuaChargingComponent, ComponentStartup>(OnStartup);

        SubscribeLocalEvent<GargantuaChargingComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<GargantuaChargingComponent, StopThrowEvent>(OnStopThrow);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var eqe = EntityQueryEnumerator<GargantuaChargingComponent>();
        while (eqe.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextDelete)
                continue;

            if (TerminatingOrDeleted(uid))
                continue;

            RemCompDeferred(uid, comp);
        }
    }

    private void OnStartup(Entity<GargantuaChargingComponent> ent, ref ComponentStartup args)
    {
        // exists in case the component doesn't get deleted
        // high speeds will not let Land/StopThrow events trigger for some reason
        ent.Comp.NextDelete = _timing.CurTime + ent.Comp.Delete;
        Dirty(ent);
    }

    private void OnCollide(Entity<GargantuaChargingComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurEntity != ent.Owner || !args.OtherFixture.Hard)
            return;

        _effects.TryApplyEffect(args.OtherEntity, ent.Comp.Effect, user: ent.Owner);
    }

    private void OnLand(Entity<GargantuaChargingComponent> ent, ref LandEvent args)
    {
        RemCompDeferred(ent.Owner, ent.Comp);
    }

    private void OnStopThrow(Entity<GargantuaChargingComponent> ent, ref StopThrowEvent args)
    {
        RemCompDeferred(ent.Owner, ent.Comp);
    }
}
