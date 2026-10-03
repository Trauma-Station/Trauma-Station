// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Shared.Map;
using System.Linq;

namespace Content.Trauma.Shared.Holosign;

public sealed partial class ChargeHolosignSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private HashSet<Entity<IComponent>> _signs = new();

    [SubscribeLocalEvent]
    private void OnInit(Entity<ChargeHolosignProjectorComponent> ent, ref ComponentInit args)
    {
        // its required, funny test is still funny
        if (string.IsNullOrEmpty(ent.Comp.SignComponentName))
            return;

        ent.Comp.SignComponent = Factory.GetRegistration(ent.Comp.SignComponentName).Type;
    }

    [SubscribeLocalEvent]
    private void OnBeforeInteract(Entity<ChargeHolosignProjectorComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || !args.CanReach ||
            HasComp<StorageComponent>(args.Target) || // if it's a storage component like a bag, we ignore usage so it can be stored
            !TryComp<LimitedChargesComponent>(ent, out var charges))
            return;

        // first check if there's any existing holofans to clear
        var coords = args.ClickLocation.SnapToGrid(EntityManager);
        var mapCoords = _transform.ToMapCoordinates(coords);
        _signs.Clear();
        _lookup.GetEntitiesInRange(ent.Comp.SignComponent, mapCoords, 0.25f, _signs);
        if (_signs.Count == 0)
            TryPlaceSign((ent, ent, charges), coords, args.User);
        else
            TryRemoveSign((ent, ent, charges), _signs.First(), args.User);

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<ChargeHolosignProjectorComponent> ent, ref UseInHandEvent args)
    {
        if (!TryComp<LimitedChargesComponent>(ent, out var charges))
            return;

        // recall all holosigns
        var added = 0;
        foreach (var signUid in ent.Comp.Signs)
        {
            if (TerminatingOrDeleted(signUid))
                continue;

            PredictedQueueDel(signUid);
            added++;
        }

        ent.Comp.Signs.Clear();
        Dirty(ent);

        // refill charges
        _charges.AddCharges((ent, charges), added);
    }

    public bool TryPlaceSign(Entity<ChargeHolosignProjectorComponent, LimitedChargesComponent> ent, EntityCoordinates coords, EntityUid user)
    {
        if (!_charges.TryUseCharge((ent, ent.Comp2)))
        {
            _popup.PopupEntity(Loc.GetString("charge-holoprojector-no-charges", ("item", ent)), ent, user);
            return false;
        }

        var placed = PredictedSpawnAtPosition(ent.Comp1.SignProto, coords);
        ent.Comp1.Signs.Add(placed);
        Dirty(ent, ent.Comp1);
        return true;
    }

    public bool TryRemoveSign(Entity<ChargeHolosignProjectorComponent, LimitedChargesComponent> ent, EntityUid sign, EntityUid user)
    {
        // don't overfill
        if (_charges.GetCurrentCharges((ent, ent.Comp2)) >= ent.Comp2.MaxCharges)
        {
            _popup.PopupEntity(Loc.GetString("charge-holoprojector-charges-full", ("item", ent)), sign, user);
            return false;
        }

        PredictedQueueDel(sign);
        ent.Comp1.Signs.Remove(sign);
        Dirty(ent, ent.Comp1);

        _charges.AddCharges((ent, ent.Comp2), 1);

        var msg = Loc.GetString("charge-holoprojector-reclaim-others", ("sign", sign), ("user", Identity.Name(user, EntityManager)));
        _popup.PopupEntity(
            Loc.GetString("charge-holoprojector-reclaim", ("sign", sign)),
            msg,
            ent,
            user);
        return true;
    }
}
