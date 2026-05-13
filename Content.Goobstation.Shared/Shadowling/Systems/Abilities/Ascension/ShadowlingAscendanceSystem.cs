// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Shared.Shadowling.Components;
using Content.Goobstation.Shared.Shadowling.Components.Abilities.Ascension;
using Content.Shared.Actions;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Goobstation.Shared.Shadowling.Systems.Abilities.Ascension;

/// <summary>
/// This is the ascendance ability.
/// The ascendance ability only forms the Ascension Egg.
/// Other info about the Ascension Egg exists in its own system.
/// </summary>
public sealed class ShadowlingAscendanceSystem : EntitySystem
{
    [Dependency] private readonly AnchorableSystem _anchorable = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityQuery<MapGridComponent> _gridQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowlingAscendanceComponent, AscendanceEvent>(OnAscendance);
        SubscribeLocalEvent<ShadowlingAscendanceComponent, AscendanceDoAfterEvent>(OnAscendanceDoAfter);
        SubscribeLocalEvent<ShadowlingAscendanceComponent, MapInitEvent>(OnStartup);
        SubscribeLocalEvent<ShadowlingAscendanceComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<ShadowlingAscendanceComponent> ent, ref MapInitEvent args)
        => _actions.AddAction(ent.Owner, ref ent.Comp.ActionEnt, ent.Comp.ActionId);

    private void OnShutdown(Entity<ShadowlingAscendanceComponent> ent, ref ComponentShutdown args)
        => _actions.RemoveAction(ent.Owner, ent.Comp.ActionEnt);

    private void OnAscendance(EntityUid uid, ShadowlingAscendanceComponent component, AscendanceEvent args)
    {
        if (args.Handled)
            return;

        if (!TileFree(uid))
        {
            _popup.PopupPredicted(Loc.GetString("shadowling-ascendance-fail"), uid, uid, PopupType.MediumCaution);
            return;
        }

        var doAfter = new DoAfterArgs(
            EntityManager,
            uid,
            component.Duration,
            new AscendanceDoAfterEvent(),
            uid,
            used: args.Action)
        {
            BreakOnDamage = true,
            CancelDuplicate = true,
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
        args.Handled = true;
    }

    private void OnAscendanceDoAfter(
        EntityUid uid,
        ShadowlingAscendanceComponent component,
        AscendanceDoAfterEvent args)
    {
        if (args.Handled
            || args.Cancelled)
            return;

        var cocoon = PredictedSpawnAtPosition(component.EggProto, Transform(uid).Coordinates);
        var ascEgg = Comp<ShadowlingAscensionEggComponent>(cocoon);
        ascEgg.Creator = uid;

        args.Handled = true;
        _actions.RemoveAction(uid, args.Args.Used);
    }

    private bool TileFree(EntityUid uid)
    {
        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var grid))
            return false;

        var indices = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);

        // too lazy to look up values from prototype fixtures :)
        var layer = (int) CollisionGroup.MachineLayer;
        var mask = (int) CollisionGroup.MachineMask;
        return _anchorable.TileFree((gridUid, grid), indices, layer, mask);
    }
}
