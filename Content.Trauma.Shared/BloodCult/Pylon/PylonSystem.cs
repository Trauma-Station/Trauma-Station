// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Random.Helpers;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Trauma.Shared.BloodCult.Pylon;

public sealed partial class PylonSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private ITileDefinitionManager _tileMan = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPointLightSystem _pointLight = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private TileSystem _tile = default!;
    [Dependency] private TurfSystem _turfs = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    private HashSet<Entity<BloodCultistComponent>> _targets = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ActivePylonComponent, PylonComponent>();
        while (query.MoveNext(out var uid, out var active, out var comp))
        {
            if (now >= active.NextCorrupt)
            {
                active.NextCorrupt = now + comp.CorruptCooldown;
                Dirty(uid, active);
                CorruptRandomTile((uid, comp));
            }

            if (now >= active.NextHeal)
            {
                active.NextHeal = now + comp.HealCooldown;
                Dirty(uid, active);
                HealInRange((uid, comp));
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnInteract(Entity<PylonComponent> pylon, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;
        if (!_cult.IsCultist(user))
        {
            _audio.PlayPredicted(pylon.Comp.BurnHandSound, pylon, user);
            _popup.PopupEntity(Loc.GetString("powered-light-component-burn-hand"), pylon, user);
            _damageable.ChangeDamage(user, pylon.Comp.DamageOnInteract, increaseOnly: true);
            return;
        }

        var active = ToggleActive(pylon) ? "on" : "off";
        var msg = Loc.GetString($"pylon-toggle-{active}");
        _popup.PopupEntity(msg, pylon, user);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<ActivePylonComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<PylonComponent>(ent, out var pylon))
            return;

        var now = _timing.CurTime;
        ent.Comp.NextCorrupt = now + pylon.CorruptCooldown;
        ent.Comp.NextHeal = now + pylon.HealCooldown;
        Dirty(ent);
    }

    private bool ToggleActive(Entity<PylonComponent> pylon)
    {
        // if it already existed, we are removing it, so invert the return value
        var state = !EnsureComp<ActivePylonComponent>(pylon, out var active);
        if (!state)
            RemComp(pylon, active);

        _appearance.SetData(pylon.Owner, PylonVisuals.Activated, state);
        _pointLight.SetEnabled(pylon.Owner, state);
        return state;
    }

    private void CorruptRandomTile(Entity<PylonComponent> pylon)
    {
        var xform = Transform(pylon);
        if (xform.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var grid))
            return;

        var radius = pylon.Comp.CorruptionRadius;
        var center = xform.Coordinates.Position;
        var tiles = _map.GetLocalTilesIntersecting(
            gridUid, grid,
            // TODO: better box centered thing
            new Box2(center + new Vector2(-radius, -radius),
                center + new Vector2(radius, radius)))
            .ToList();

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(pylon));
        rand.Shuffle(tiles);

        var cultTile = (ContentTileDefinition) _tileMan[pylon.Comp.CultTile];
        var tileId = cultTile.TileId;
        foreach (var tile in tiles)
        {
            if (tile.Tile.TypeId == tileId)
                continue; // ignore already converted tiles

            var tilePos = _turfs.GetTileCenter(tile);
            // all clients in pvs range of the pylon predict the sound
            _audio.PlayPredicted(pylon.Comp.CorruptTileSound, tilePos, null, AudioParams.Default.WithVolume(-5));
            _tile.ReplaceTile(tile, cultTile);
            // also means this effect can be purely clientside
            if (_net.IsClient)
                Spawn(pylon.Comp.TileCorruptEffect, tilePos);
            return; // only replace the first found tile, not all of them!
        }
    }

    private void HealInRange(Entity<PylonComponent> pylon)
    {
        // this will only heal humanoid cultists, not constructs.
        // due to how BloodCultistComponent is networked,  it also means
        // the client only predicts healing itself with no extra checks :)
        var pos = Transform(pylon).Coordinates;
        _targets.Clear();
        _lookup.GetEntitiesInRange(pos, pylon.Comp.HealingAuraRange, _targets);
        foreach (var target in _targets)
        {
            if (!_mobState.IsDead(target.Owner))
                _damageable.ChangeDamage(target.Owner, pylon.Comp.Healing, true);
        }
    }
}
