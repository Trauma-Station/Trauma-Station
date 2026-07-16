// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Shared.Blob;
using Content.Goobstation.Shared.Blob.Components;
using Content.Goobstation.Shared.Blob.Events;
using Content.Shared.EntityEffects;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Random.Helpers;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Blob;

public sealed partial class BlobTileSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBlobCoreSystem _core = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private NpcFactionSystem _faction = default!;
    [Dependency] private EntityQuery<BlobCoreComponent> _coreQuery = default!;
    [Dependency] private EntityQuery<BlobObserverComponent> _observerQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    private static readonly ProtoId<NpcFactionPrototype> BlobFaction = "Blob";

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<BlobTileComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        if (!_observerQuery.TryComp(user, out var observer))
            return;

        if (ent.Comp.Core == null || observer.Core is not { } core)
            return;

        if (Transform(ent).Anchored)
            return;

        var current = ProtoMan.Index(ent.Comp.Tile);
        if (current.Upgrade is not { } nextId)
            return;

        var next = ProtoMan.Index(nextId);
        var verbName = $"Upgrade to {next.Name}";
        args.Verbs.Add(new()
        {
            Act = () => TryUpgrade(ent, core, user),
            Text = verbName
        });
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<BlobTileComponent> ent, ref MapInitEvent args)
    {
        var faction = EnsureComp<NpcFactionMemberComponent>(ent);
        var factionEnt = (ent, faction);
        _faction.ClearFactions(factionEnt, false);
        _faction.AddFaction(factionEnt, BlobFaction, true);

        // let npcs target it...?
        EnsureComp<MobStateComponent>(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<BlobTileComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Core is not { } core ||
            !_coreQuery.TryComp(core, out var coreComp))
            return;

        coreComp.BlobTiles.Remove(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnDestruction(Entity<BlobTileComponent> ent, ref DestructionEventArgs args)
    {
        if (ent.Comp.Core is not { } core ||
            !_coreQuery.TryComp(core, out var coreComp))
            return;

        var chem = ProtoMan.Index(coreComp.CurrentChem);
        if (chem.DestructionEffects is not { } effects)
            return;

        _effects.ApplyEffects(ent, effects, predicted: false); // destruction prediction when
    }

    [SubscribeLocalEvent]
    private void OnNodePulse(Entity<BlobTileComponent> ent, ref BlobNodePulseEvent args)
    {
        args.Handled |= NodePulse(ent, args.Core, args.Chem, args.Handled);
    }

    /// <summary>
    /// Logic for when a blob tile is pulsed by a blob node.
    /// Returns true if an entity was attacked, preventing further spread/attack attempts.
    /// </summary>
    private bool NodePulse(Entity<BlobTileComponent> ent, Entity<BlobCoreComponent> core, BlobChemPrototype chem, bool lazy)
    {
        var healing = ent.Comp.HealthOfPulse;
        if (chem.HealingScale != 1)
            healing *= chem.HealingScale;
        _damage.ChangeDamage(ent.Owner, healing);

        if (lazy)
            return false;

        var xform = Transform(ent);
        if (xform.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var grid))
            return false;

        if (_core.GetNearNode(xform.Coordinates, core.Comp.TilesRadiusLimit) is not { } node)
            return false;

        var mobTile = _map.GetTileRef(gridUid, grid, xform.Coordinates);

        var center = mobTile.GridIndices;
        var mobAdjacentTiles = new[]
        {
            center.Offset(Direction.East),
            center.Offset(Direction.West),
            center.Offset(Direction.North),
            center.Offset(Direction.South),
        };

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));

        var localPos = xform.Coordinates.Position;

        var radius = 1.0f;

        var innerTiles = _map.GetLocalTilesIntersecting(gridUid, grid,
            new Box2(localPos + new Vector2(-radius, -radius), localPos + new Vector2(radius, radius)))
            .ToList();
        rand.Shuffle(innerTiles);

        foreach (var innerTile in innerTiles)
        {
            if (!mobAdjacentTiles.Contains(innerTile.GridIndices))
                continue;

            var spawn = true;
            foreach (var uid in _map.GetAnchoredEntities(gridUid, grid, innerTile.GridIndices))
            {
                if (HasComp<BlobTileComponent>(uid))
                    spawn = false;

                if (!HasComp<DestructibleComponent>(uid))
                    continue;

                DoLunge(ent, uid);
                _damage.TryChangeDamage(uid, chem.Damage);
                if (_net.IsClient && _timing.IsFirstTimePredicted) // all clients will predict it
                    _audio.PlayPvs(core.Comp.AttackSound, uid);
                return true;
            }

            if (!spawn)
                continue;

            // spawn a new blob tile there
            var coords = _map.ToCoordinates(gridUid, innerTile.GridIndices, grid);
            if (_core.TransformBlobTile(null, core.AsNullable(), node, ent.Comp.SpreadTile, coords))
                break;
        }

        return false;
    }

    private void TryUpgrade(Entity<BlobTileComponent> target, EntityUid core, EntityUid observer)
    {
        var coords = Transform(target).Coordinates;
        var current = ProtoMan.Index(target.Comp.Tile);
        if (current.Upgrade is not { } nextId ||
            !TryComp<BlobCoreComponent>(core, out var coreComp) ||
            _core.GetNearNode(coords, coreComp.TilesRadiusLimit) is not { } node)
            return;

        _core.TransformBlobTile(target.AsNullable(), (core, coreComp), node, nextId, coords);
    }

    public bool IsEmptySpecial(Entity<BlobNodeComponent> node, ProtoId<BlobTilePrototype> tile)
        // no real good way about this
        => tile.Id switch
        {
            "Factory" => TerminatingOrDeleted(node.Comp.BlobFactory),
            "Resource" => TerminatingOrDeleted(node.Comp.BlobResource),
            _ => false
        };

    public void DoLunge(EntityUid from, EntityUid target, EntityUid? user = null)
    {
        var userXform = Transform(from);
        var targetPos = _transform.GetWorldPosition(target);
        var localPos = Vector2.Transform(targetPos, _transform.GetInvWorldMatrix(userXform));
        localPos = userXform.LocalRotation.RotateVec(localPos);

        var ev = new BlobAttackEvent(GetNetEntity(from), GetNetEntity(target), localPos);
        if (_net.IsClient)
        {
            // client predicts this lunge either via interaction or node update loop
            RaiseLocalEvent(ev);
        }
        else if (user != null)
        {
            // server tells clients about another player's interaction
            var filter = Filter.Pvs(from);
            filter.RemovePlayerByAttachedEntity(user.Value);
            RaiseNetworkEvent(ev, filter);
        }
    }
}
