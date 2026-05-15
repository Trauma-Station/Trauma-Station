// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Wraith.Events;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Maps;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Goobstation.Shared.Wraith.Revenant;

public sealed partial class RevenantShockwaveSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private TileSystem _tile = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private HashSet<Entity<DamageableComponent>> _targets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantShockwaveComponent, RevenantShockwaveEvent>(OnShockwave);
    }

    private void OnShockwave(Entity<RevenantShockwaveComponent> ent, ref RevenantShockwaveEvent args)
    {
        PryAnyTiles(ent);

        if (ent.Comp.StructureDamage is not {} damage)
            return;

        var coords = Transform(ent).Coordinates;
        _targets.Clear();
        _lookup.GetEntitiesInRange(coords, ent.Comp.SearchRange, _targets);
        foreach (var entity in _targets)
        {
            if (_tag.HasTag(entity, ent.Comp.WallTag) || _tag.HasTag(entity, ent.Comp.WindowTag))
            {
                _damageable.ChangeDamage(entity.AsNullable(), damage, true, origin: ent.Owner);
                continue;
            }

            _stun.KnockdownOrStun(entity.Owner, ent.Comp.KnockdownDuration);
        }

        _audio.PlayPredicted(ent.Comp.ShockSound, ent.Owner, null);

        args.Handled = true;
    }

    //TO DO: Add some sort of effect that telegraphs the use of the shockwave.
    private void PryAnyTiles(Entity<RevenantShockwaveComponent> ent)
    {
        if (_net.IsClient)
            return;

        var grid = _transform.GetGrid(ent.Owner);
        if (!TryComp<MapGridComponent>(grid, out var map))
            return;

        var tiles = _mapSystem.GetTilesIntersecting(
                grid.Value,
                map,
                Box2.CenteredAround(_transform.GetWorldPosition(ent.Owner),
                    new Vector2(ent.Comp.SearchRange * 2, ent.Comp.SearchRange)))
            .ToArray();

        _random.Shuffle(tiles);

        for (var i = 0; i < ent.Comp.TilesToPry; i++)
        {
            if (!tiles.TryGetValue(i, out var value))
                continue;
            _tile.PryTile(value);
        }
    }
}
