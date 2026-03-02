using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Station;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared._DV.CosmicCult;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Trauma.Common.RoundEnd;
using Robust.Shared.Map.Components;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Numerics;
using System.Linq;

namespace Content.Server._DV.CosmicCult.Abilities;

// TODO: why the fuck is this not in MonumentSystem
public sealed class CosmicMonumentSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly CosmicCultRuleSystem _cultRule = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private HashSet<Entity<MonumentSpawnMarkComponent>> _nearbyMarks = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, EventCosmicPlaceMonument>(OnCosmicPlaceMonument);
        SubscribeLocalEvent<MonumentSpawnMarkComponent, InteractHandEvent>(OnActivate);
        SubscribeLocalEvent<MonumentOnDespawnComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<EmergencyShuttleDockedEvent>(OnEvacDocked);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var spawnQuery = EntityQueryEnumerator<MonumentOnDespawnComponent>();
        while (spawnQuery.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.SpawnTimer)
                continue;

            var monument = Spawn(comp.Prototype, Transform(uid).Coordinates);
            var evt = new CosmicCultAssociateRuleEvent(uid, monument);
            RaiseLocalEvent(ref evt);
            RemComp<MonumentOnDespawnComponent>(uid);
        }
    }

    private void OnStartup(Entity<MonumentOnDespawnComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.SpawnTimer = _timing.CurTime + ent.Comp.SpawnTime;
    }

    private void OnCosmicPlaceMonument(Entity<CosmicCultComponent> ent, ref EventCosmicPlaceMonument args)
    {
        if (!TryComp<MonumentPlacementActionComponent>(args.Action, out var monuPlacement)
        || !TryComp<CosmicCultComponent>(args.Performer, out var cultComp)
        || _cultRule.AssociatedGamerule(ent) is not { } cult
        || args.Handled)
            return;

        args.Handled = true;

        if (monuPlacement.MarkUid is { } mark) // If you already placed a mark, using the action again removes it
        {
            QueueDel(mark);
            monuPlacement.MarkUid = null;
            _popup.PopupEntity(Loc.GetString("cosmiccult-monument-mark-removed"), ent, ent);
            return;
        }
        if (!VerifyPlacement(ent, out var targetPos))
            return;

        _nearbyMarks.Clear();
        _lookup.GetEntitiesInRange(targetPos, range: 0.1f, _nearbyMarks); // If you use the action on top of an existing mark, you un-/approve it instead
        if (_nearbyMarks.Count > 0)
        {
            ToggleMarkApproval(_nearbyMarks.First(), (args.Performer, cultComp));
            return;
        }

        var newMark = Spawn(monuPlacement.MarkProto, targetPos); // If all else failed, just create a new mark
        monuPlacement.MarkUid = newMark;
        _cultRule.TransferCultAssociation(ent, newMark);
        EnsureComp<MonumentSpawnMarkComponent>(newMark, out var markComp);
        markComp.ApprovalsRequired = (int) Math.Ceiling(cult.Comp.TotalCult / 2f);

        ToggleMarkApproval((newMark, markComp), (args.Performer, cultComp)); // Automatically approve your own mark
    }


    /// <summary>
    /// If a given cultist hasn't approved a given mark, set their approval, otherwise revoke it.
    /// If enough approvals are granted, spawn the actual monument
    /// </summary>
    private void ToggleMarkApproval(Entity<MonumentSpawnMarkComponent> monument, Entity<CosmicCultComponent> cultist)
    {
        if (_cultRule.AssociatedGamerule(monument) is not { } cult) return;
        if (cultist.Comp.CurrentLevel < cultist.Comp.MaxLevel)
        {
            _popup.PopupEntity(Loc.GetString("cosmiccult-monument-approval-lowlevel"), monument, cultist);
            return;
        }
        if (monument.Comp.ApprovingCultists.Remove(cultist.Owner))
        {
            _popup.PopupEntity(Loc.GetString("cosmiccult-monument-approval-removed"), monument, cultist);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("cosmiccult-monument-approval-added"), monument, cultist);
            monument.Comp.ApprovingCultists.Add(cultist.Owner);
        }

        monument.Comp.ApprovalsRequired = (int) Math.Ceiling(cult.Comp.TotalCult / 2f);
        if (monument.Comp.ApprovalsRequired > monument.Comp.ApprovingCultists.Count) return; // Not enough approvals yet

        var newMonument = Spawn(monument.Comp.MonumentSpawnIn, Transform(monument).Coordinates);
        var evt = new CosmicCultAssociateRuleEvent(monument, newMonument);
        RaiseLocalEvent(ref evt);
        RemoveAllMonumentMarks();

        Dirty(monument, monument.Comp);
    }

    /// <summary>
    /// Removes all already placed monument marks, and all "Place Monument" actions from all cultists
    /// </summary>
    private void RemoveAllMonumentMarks()
    {
        var cultQuery = EntityQueryEnumerator<CosmicCultComponent>();
        while (cultQuery.MoveNext(out _, out var comp))
            _actions.RemoveAction(comp.MonumentActionEntity);

        var markQuery = EntityQueryEnumerator<MonumentSpawnMarkComponent>();
        while (markQuery.MoveNext(out var mark, out _))
            QueueDel(mark);
    }

    private void OnActivate(Entity<MonumentSpawnMarkComponent> ent, ref InteractHandEvent args)
    {
        if (!TryComp<CosmicCultComponent>(args.User, out var cultComp)) return;
        ToggleMarkApproval(ent, (args.User, cultComp));
    }

    /// <summary>
    /// Makes it impossible to place or activate a monument if evac docks to the station.
    /// </summary>
    private void OnEvacDocked(ref EmergencyShuttleDockedEvent args)
    {
        RemoveAllMonumentMarks();

        var query = EntityQueryEnumerator<MonumentComponent>(); // REmove any existing monuments
        while (query.MoveNext(out var uid, out var comp))
        {
            Spawn(comp.DespawnVfx, Transform(uid).Coordinates);
            QueueDel(uid);
        }
    }

    //todo this can probably be mostly moved to shared but my brain isn't cooperating w/ that rn
    // ... and neither do I care enough to do this
    private bool VerifyPlacement(Entity<CosmicCultComponent> uid, out EntityCoordinates outPos)
    {
        //MAKE SURE WE'RE STANDING ON A GRID
        var xform = Transform(uid);
        outPos = new EntityCoordinates();

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-monument-spawn-error-grid"), uid, uid);
            return false;
        }

        var localTile = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);
        var targetIndices = localTile.GridIndices + new Vector2i(0, 1);
        var pos = _map.ToCenterCoordinates(xform.GridUid.Value, targetIndices, grid);
        outPos = pos;
        var box = new Box2(pos.Position + new Vector2(-1.4f, -0.4f), pos.Position + new Vector2(1.4f, 0.4f));

        //CHECK IF IT'S BEING PLACED CHEESILY CLOSE TO SPACE
        var spaceDistance = 3;
        var worldPos = _transform.GetWorldPosition(xform);
        foreach (var tile in _map.GetTilesIntersecting(xform.GridUid.Value, grid, new Circle(worldPos, spaceDistance)))
        {
            if (_turf.IsSpace(tile))
            {
                _popup.PopupEntity(Loc.GetString("cosmicability-monument-spawn-error-space", ("DISTANCE", spaceDistance)), uid, uid);
                return false;
            }
        }

        //CHECK IF WE'RE ON THE STATION OR IF SOMEONE'S TRYING TO SNEAK THIS ONTO SOMETHING SMOL
        if (_station.GetStationInMap(xform.MapID) is not { } station)
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-monument-spawn-error-station"), uid, uid);
            return false;
        }

        var stationGrid = _station.GetLargestGrid(station);

        if (stationGrid != null && stationGrid != xform.GridUid)
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-monument-spawn-error-station"), uid, uid);
            return false;
        }

        //CHECK FOR ENTITY AND ENVIRONMENTAL INTERSECTIONS
        if (_lookup.AnyLocalEntitiesIntersecting(xform.GridUid.Value, box, LookupFlags.Dynamic | LookupFlags.Static, uid))
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-monument-spawn-error-intersection"), uid, uid);
            return false;
        }

        return true;
    }
}
