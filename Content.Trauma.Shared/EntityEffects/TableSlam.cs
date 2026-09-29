// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Throwing;
using Content.Shared.Coordinates;
using Content.Shared.Climbing.Components;
using Robust.Shared.GameObjects;

namespace Content.Trauma.Shared.EntityEffects.Effects;

/// <summary>
/// Checks if any tables are in range and slams the ent on it.
/// </summary>
public sealed partial class TableSlam : EntityEffectBase<TableSlam>
{
    [DataField]
    public float seachRange = 1.0f;
}

public sealed partial class TableSlamSystem : EntityEffectSystem<TransformComponent, TableSlam>
{
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<TableSlam> args)
    {
        var entPos = _transform.GetMapCoordinates(ent.Owner);

        foreach (var (uid, comp) in _lookup.GetEntitiesInRange<BonkableComponent>(entPos, args.Effect.seachRange)){
            _throwing.TryThrow(ent.Owner, uid.ToCoordinates(), animated: false, doSpin: false);
            break;
        }
    }
}