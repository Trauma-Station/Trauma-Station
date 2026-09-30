// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.EntityEffects.Effects;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Robust.Shared.GameObjects;

namespace Content.Trauma.Server.EntityEffects.Effects;

/// <summary>
/// Sets on fire all entitys in set range.
/// </summary>
public sealed partial class SetOnFireInRangeSystem : EntityEffectSystem<FlammableComponent, SetOnFireInRange>
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<FlammableComponent> ent, ref EntityEffectEvent<SetOnFireInRange> args)
    {
        var entPos = _transform.GetMapCoordinates(ent.Owner);
        foreach (var (uid, comp) in _lookup.GetEntitiesInRange<FlammableComponent>(entPos, args.Effect.SeachRange)){
            _flammable.AdjustFireStacks(uid, args.Effect.FireStacks, comp);
            _flammable.Ignite(uid, ent.Owner, comp, ent.Owner);
        }
    }
}
