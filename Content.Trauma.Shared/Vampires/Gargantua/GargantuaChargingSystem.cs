// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Events;

namespace Content.Trauma.Shared.Vampires.Gargantua;

public sealed class GargantuaChargingSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GargantuaChargingComponent, StartCollideEvent>(OnCollide);

        SubscribeLocalEvent<GargantuaChargingComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<GargantuaChargingComponent, StopThrowEvent>(OnStopThrow);
    }

    private void OnCollide(Entity<GargantuaChargingComponent> ent, ref StartCollideEvent args)
    {
        // So we don't null reference errors
        if (_net.IsClient)
            return;

        _effects.ApplyEffects(args.OtherEntity, ent.Comp.ImpactEffects);
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
