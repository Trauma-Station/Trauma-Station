// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.UserInterface;

namespace Content.Trauma.Shared.Cargo;

public sealed class CargoOrderDestinationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CargoOrderConsoleComponent, BeforeActivatableUIOpenEvent>(OnUIOpen);
        SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleSetDestinationMessage>(OnSetDestination);
    }

    private void OnUIOpen(Entity<CargoOrderConsoleComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        if (ent.Comp.Destination != null || IsPaused(ent)) // no shitting up map yml
            return;

        // when opening a console for the first time, link it to a telepad or the trade station
        ent.Comp.Destination = GetNetEntity(FindDefaultDest(ent));
        Dirty(ent);
    }

    private void OnSetDestination(Entity<CargoOrderConsoleComponent> ent, ref CargoConsoleSetDestinationMessage args)
    {
        if (args.Destination == ent.Comp.Destination ||
            // no storing bad entities, currently only telepads can be used
            GetEntity(args.Destination) is {} dest && !ValidDest(dest))
            return;

        ent.Comp.Destination = args.Destination;
        Dirty(ent);
    }

    private EntityUid? FindDefaultDest(EntityUid console)
    {
        var map = Transform(console).MapID;
        var telepadQuery = EntityQueryEnumerator<CargoTelepadComponent, TransformComponent>();
        while (telepadQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID == map)
                return uid;
        }

        var atsQuery = EntityQueryEnumerator<TradeStationComponent, TransformComponent>();
        while (atsQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID == map)
                return uid;
        }

        // better luck next time :(
        return null;
    }

    private bool ValidDest(EntityUid uid)
        => HasComp<CargoTelepadComponent>(uid) || HasComp<TradeStationComponent>(uid);
}
