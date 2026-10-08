// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
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
    [Dependency] private DamageableSystem _damage = default!;
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

    private const float PylonLookupRange = 10;

    private HashSet<Entity<BloodCultistComponent>> _targets = new();
    private HashSet<Entity<ActivePylonComponent>> _pylons = new();

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
            _damage.ChangeDamage(user, pylon.Comp.DamageOnInteract, increaseOnly: true, targetPart: TargetBodyPart.Hands, canMiss: false);
            return;
        }

        ToggleActive(pylon, user);
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

    [SubscribeLocalEvent]
    private void OnAnchorStateChanged(Entity<ActivePylonComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored && !_timing.ApplyingState)
            RemCompDeferred(ent, ent.Comp);
    }

    private bool ToggleActive(Entity<PylonComponent> pylon, EntityUid user)
    {
        // if it already existed, we are removing it, so invert the state
        var enabling = !TryComp<ActivePylonComponent>(pylon, out var active);

        if (enabling)
        {
            var coords = Transform(pylon).Coordinates;
            _pylons.Clear();
            _lookup.GetEntitiesInRange(coords, PylonLookupRange, _pylons);
            if (_pylons.Count > 0)
            {
                _popup.PopupEntity($"There can't be other pylons within {PylonLookupRange} meters!", pylon, user, PopupType.MediumCaution);
                return false;
            }
        }

        if (enabling)
            EnsureComp<ActivePylonComponent>(pylon);
        else
            RemComp(pylon, active!);

        _appearance.SetData(pylon.Owner, PylonVisuals.Activated, enabling);
        _pointLight.SetEnabled(pylon.Owner, enabling);

        var suffix = enabling ? "on" : "off";
        var msg = Loc.GetString($"pylon-toggle-{suffix}");
        _popup.PopupEntity(msg, pylon, user);
        return true;
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
            // TODO: make a spawn effect message
            if (_net.IsClient)
                Spawn(pylon.Comp.TileCorruptEffect, tilePos);
            return; // only replace the first found tile, not all of them!
        }
    }

    private void HealInRange(Entity<PylonComponent> pylon)
    {
        // this will only heal humanoid cultists, not constructs.
        // due to how BloodCultistComponent is networked, it also means
        // the client only predicts healing itself with no extra checks :)
        var pos = Transform(pylon).Coordinates;
        _targets.Clear();
        _lookup.GetEntitiesInRange(pos, pylon.Comp.HealingAuraRange, _targets);
        foreach (var target in _targets)
        {
            if (!_mobState.IsDead(target.Owner))
                _damage.ChangeDamage(target.Owner, pylon.Comp.Healing, true, targetPart: TargetBodyPart.All, canMiss: false, splitDamage: SplitDamageBehavior.None);
        }
    }
}
