// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Wraith.SaltLines;
using Content.Server.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Wraith.SaltLines;

public sealed class SaltLineSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    private static readonly ProtoId<ReagentPrototype> ReagentSalt = "TableSalt";

    private EntityQuery<SolutionContainerManagerComponent> _solutionContainerManQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SaltLineComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<SaltLineComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<SaltLinePlacerComponent, AfterInteractEvent>(OnSaltLineAfterInteract);

        SubscribeLocalEvent<ConsumeOnSaltLineComponent, AttemptSaltLineEvent>(OnAttemptSaltLine);

        _solutionContainerManQuery = GetEntityQuery<SolutionContainerManagerComponent>();
    }

    private void OnMapInit(Entity<SaltLineComponent> ent, ref MapInitEvent args) =>
        UpdateAppearance(ent);

    private void OnAnchorChanged(Entity<SaltLineComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            return;

        UpdateAppearance(ent);
        UpdateNeighbors(ent);
    }

    private void OnSaltLineAfterInteract(Entity<SaltLinePlacerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (!TryComp<MapGridComponent>(_transform.GetGrid(args.ClickLocation), out var grid))
            return;

        var gridUid = _transform.GetGrid(args.ClickLocation)!.Value;
        var snapPos = _map.TileIndicesFor((gridUid, grid), args.ClickLocation);

        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, snapPos);
        while (anchored.MoveNext(out var entity))
        {
            if (HasComp<SaltLineComponent>(entity.Value)) // dont place in same tile
                return;
        }

        var ev = new AttemptSaltLineEvent();
        ev.User = args.User;
        RaiseLocalEvent(ent.Owner, ref ev);

        if (ev.Cancelled)
            return;

        var newSaltLine = Spawn(ent.Comp.SaltLine, _map.GridTileToLocal(gridUid, grid, snapPos));
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.User)} placed {ToPrettyString(newSaltLine):saltline} at {Transform(newSaltLine).Coordinates}");
        args.Handled = true;
    }

    private void OnAttemptSaltLine(Entity<ConsumeOnSaltLineComponent> ent, ref AttemptSaltLineEvent args)
    {
        if (!_solutionContainerManQuery.TryComp(ent.Owner, out var solMan))
        {
            args.Cancelled = true;
            return;
        }

        foreach (var container in solMan.Containers)
        {
            if (!_solution.TryGetSolution(ent.Owner, container, out var solution)
                || solution is not { } sol
                || !sol.Comp.Solution.ContainsPrototype(ReagentSalt))
                continue;

            // Try remove salt from the first found solution, if there's no salt return and check next container,
            // else exit the function without cancelling it
            if (TryRemoveSalt(sol, ent, args.User))
                return;
        }

        // No reagent was consumed, therefore the event failed
        args.Cancelled = true;
    }

    #region Helpers

    /// <summary>
    ///  Removes salt from a solution
    /// </summary>
    public bool TryRemoveSalt(Entity<SolutionComponent> sol, Entity<ConsumeOnSaltLineComponent> ent, EntityUid user)
    {
        var saltAmount = sol.Comp.Solution.GetTotalPrototypeQuantity(ReagentSalt);
        if (saltAmount < ent.Comp.Amount)
            return false;

        _solution.RemoveReagent(sol, ReagentSalt, ent.Comp.Amount);
        return true;
    }


    private void UpdateAppearance(Entity<SaltLineComponent> ent)
    {
        var transform = Transform(ent.Owner);
        if (!TryComp<MapGridComponent>(transform.GridUid, out var grid) || transform.GridUid == null)
            return;

        var mask = SaltLineVisDirFlags.None;
        var tile = _map.TileIndicesFor((transform.GridUid.Value, grid), transform.Coordinates);

        var directions = new[]
        {
            (new Vector2i(0, 1), SaltLineVisDirFlags.North),
            (new Vector2i(0, -1), SaltLineVisDirFlags.South),
            (new Vector2i(1, 0), SaltLineVisDirFlags.East),
            (new Vector2i(-1, 0), SaltLineVisDirFlags.West)
        };

        foreach (var (offset, flag) in directions)
        {
            var checkTile = tile + offset;
            var anchored = _map.GetAnchoredEntitiesEnumerator(transform.GridUid.Value, grid, checkTile);

            while (anchored.MoveNext(out var entity))
            {
                if (HasComp<SaltLineComponent>(entity.Value))
                {
                    mask |= flag;
                    break;
                }
            }
        }

        _appearance.SetData(ent.Owner, SaltLineVisuals.ConnectedMask, mask);
    }

    private void UpdateNeighbors(EntityUid uid)
    {
        var transform = Transform(uid);
        if (!TryComp<MapGridComponent>(transform.GridUid, out var grid) || transform.GridUid == null)
            return;

        var tile = _map.TileIndicesFor((transform.GridUid.Value, grid), transform.Coordinates);
        var offsets = new[] { new Vector2i(0, 1), new Vector2i(0, -1), new Vector2i(1, 0), new Vector2i(-1, 0) };

        foreach (var offset in offsets)
        {
            var checkTile = tile + offset;
            var anchored = _map.GetAnchoredEntitiesEnumerator(transform.GridUid.Value, grid, checkTile);

            while (anchored.MoveNext(out var entity))
            {
                if (!TryComp<SaltLineComponent>(entity.Value, out var comp))
                    continue;

                UpdateAppearance((entity.Value, comp));
            }
        }
    }
    #endregion
}
