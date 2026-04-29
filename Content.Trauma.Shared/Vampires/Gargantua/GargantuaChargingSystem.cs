// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Events;

namespace Content.Trauma.Shared.Vampires.Gargantua;

public sealed class GargantuaChargingSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GargantuaChargingComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<GargantuaChargingComponent, LandEvent>(OnLand);
    }

    private void OnCollide(Entity<GargantuaChargingComponent> ent, ref StartCollideEvent args)
    {
        _effects.ApplyEffects(args.OtherEntity, ent.Comp.ImpactEffects);
    }

    private void OnLand(Entity<GargantuaChargingComponent> ent, ref LandEvent args)
    {
        RemCompDeferred(ent.Owner, ent.Comp);
    }
}
