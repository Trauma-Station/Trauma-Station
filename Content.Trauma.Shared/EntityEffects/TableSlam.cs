// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Coordinates;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Climbing.Components;
using Content.Goobstation.Shared.TableSlam;
using Robust.Shared.GameObjects;

namespace Content.Trauma.Shared.EntityEffects.Effects;

/// <summary>
/// Checks if any tables are in range and slams the ent on it.
/// </summary>
public sealed partial class TableSlam : EntityEffectBase<TableSlam>
{
    [DataField]
    public float SeachRange = 1.0f;
}

public sealed partial class TableSlamEffectSystem : EntityEffectSystem<PullableComponent, TableSlam>
{
    [Dependency] private TableSlamSystem _slam = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<PullableComponent> ent, ref EntityEffectEvent<TableSlam> args)
    {
        var entPos = _transform.GetMapCoordinates(ent.Owner);
        foreach (var (uid, comp) in _lookup.GetEntitiesInRange<BonkableComponent>(entPos, args.Effect.SeachRange)){
            _slam.TableSlam(ent, uid);
            break;
        }
    }
}