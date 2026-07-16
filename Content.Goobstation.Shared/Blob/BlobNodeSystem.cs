// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Blob.Components;
using Content.Shared.Destructible;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Blob;

public sealed partial class BlobNodeSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedBlobCoreSystem _core = default!;
    [Dependency] private SharedBlobMobSystem _blobMob = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private EntityQuery<BlobCoreComponent> _coreQuery = default!;
    [Dependency] private EntityQuery<BlobTileComponent> _tileQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    public static readonly ProtoId<BlobTilePrototype> NodeTile = "Node";

    private HashSet<Entity<BlobMobComponent>> _mobs = new();
    private HashSet<Entity<BlobTileComponent>> _tiles = new();

    public void PulseNode(Entity<BlobNodeComponent> ent, Entity<BlobCoreComponent> core, BlobChemPrototype chem)
    {
        var xform = Transform(ent);

        var ev = new BlobSpecialPulseEvent(core, chem);
        if (ent.Comp.BlobFactory is { } factory)
            RaiseLocalEvent(factory, ref ev);
        if (ent.Comp.BlobResource is { } resource)
            RaiseLocalEvent(resource, ref ev);

        _mobs.Clear();
        _lookup.GetEntitiesInRange(xform.Coordinates, ent.Comp.PulseRadius, _mobs);
        foreach (var mob in _mobs)
        {
            if (_mob.IsDead(mob))
                continue;

            _blobMob.NodePulse(mob);
        }
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<BlobNodeComponent> ent, ref ComponentShutdown args)
    {
        if (!_tileQuery.TryComp(ent, out var tile) ||
            tile.Tile != NodeTile) // ignore core it will delete stuff itself
            return;

        PredictedQueueDel(ent.Comp.BlobFactory);
        PredictedQueueDel(ent.Comp.BlobResource);
    }

    private void Pulse(Entity<BlobNodeComponent> ent, Entity<BlobCoreComponent> core)
    {
        var chem = ProtoMan.Index(core.Comp.CurrentChem);
        var coords = Transform(ent).Coordinates;
        _tiles.Clear();
        _lookup.GetEntitiesInRange(coords, ent.Comp.PulseRadius, _tiles);
        var ev = new BlobNodePulseEvent(core, chem);
        foreach (var tile in _tiles)
        {
            RaiseLocalEvent(tile, ref ev);
        }

        PulseNode(ent, core, chem);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BlobNodeComponent>();
        foreach (var ent in query)
        {
            if (now < ent.Comp.NextPulse)
                continue;

            ent.Comp.NextPulse = now + ent.Comp.PulseFrequency;
            Dirty(ent);

            if (_tileQuery.CompOrNull(ent)?.Core is not { } core ||
                !_coreQuery.TryComp(core, out var coreComp))
            {
                PredictedQueueDel(ent);
                continue;
            }

            Pulse(ent, (core, coreComp));
        }
    }

    public void SwapSpecials(Entity<BlobNodeComponent> a, Entity<BlobNodeComponent> b)
    {
        (a.Comp.BlobFactory, b.Comp.BlobFactory) = (b.Comp.BlobFactory, a.Comp.BlobFactory);
        (a.Comp.BlobResource, b.Comp.BlobResource) = (b.Comp.BlobResource, a.Comp.BlobResource);
        Dirty(a);
        Dirty(b);
    }
}
