// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Shared.Megafauna.Components;
using Content.Lavaland.Shared.Megafauna.Events;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Lavaland.Shared.Megafauna.Systems;

public sealed partial class MegafaunaSystem
{
    public void StartupMegafauna(Entity<MegafaunaAiComponent> ent)
    {
        var ev = new MegafaunaStartupEvent();
        RaiseLocalEvent(ent, ref ev);
        ent.Comp.Active = true;
    }

    public void ShutdownMegafauna(Entity<MegafaunaAiComponent> ent)
    {
        var ev = new MegafaunaShutdownEvent();
        RaiseLocalEvent(ent, ref ev);
        ent.Comp.Active = false;
    }

    public void KillMegafauna(Entity<MegafaunaAiComponent> ent)
    {
        var ev = new MegafaunaKilledEvent();
        RaiseLocalEvent(ent, ref ev);
        ent.Comp.Active = false;
    }

    /// <summary>
    /// Helper method that constructs new <see cref="RequestPerformActionEvent"/> for megafauna AI to use an action.
    /// </summary>
    public RequestPerformActionEvent GetPerformEvent(EntityUid boss, EntityUid action)
    {
        var targetingComp = CompOrNull<MegafaunaAiTargetingComponent>(boss);

        var netAction = GetNetEntity(action);
        var netTarget = HasComp<EntityTargetActionComponent>(action) ? GetNetEntity(targetingComp?.TargetEnt) : null;
        var netCoords = HasComp<WorldTargetActionComponent>(action) ? GetNetCoordinates(targetingComp?.TargetCoords) : null;

        return new RequestPerformActionEvent(netAction, netTarget, netCoords);
    }

    public void PickRandomPosition(MegafaunaCalculationBaseArgs args, float radius)
    {
        // TODO add an option to not pick any obstructed coordinates

        var uid = args.Entity;
        var mapId = Transform(uid).MapID;

        var position = _xform.GetWorldPosition(uid) + args.Random.NextVector2(radius);
        var newMapCoords = new MapCoordinates(position, mapId);
        var coords = _xform.ToCoordinates(newMapCoords);

        var comp = EnsureComp<MegafaunaAiTargetingComponent>(args.Entity);
        comp.TargetEnt = null;
        comp.TargetCoords = coords;
    }
}
