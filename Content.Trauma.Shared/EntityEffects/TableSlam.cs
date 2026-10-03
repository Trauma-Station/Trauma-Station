// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Climbing.Components;
using Content.Shared.Coordinates;
using Content.Shared.EntityEffects;
using Content.Shared.Movement.Pulling.Components;
using Content.Goobstation.Shared.TableSlam;

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
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TableSlamSystem _slam = default!;

    private readonly HashSet<Entity<BonkableComponent>> _tablesNearby = new();

    protected override void Effect(Entity<PullableComponent> ent, ref EntityEffectEvent<TableSlam> args)
    {
        var entPos = _transform.GetMapCoordinates(ent.Owner);

        _tablesNearby.Clear();
        _lookup.GetEntitiesInRange<BonkableComponent>(entPos, args.Effect.SeachRange, _tablesNearby);
        foreach (var (uid, comp) in _tablesNearby){
            _slam.TableSlam(ent, uid);
            break;
        }
    }
}
