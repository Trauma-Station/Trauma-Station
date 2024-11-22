// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Pinpointer;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Warps;
using Content.Trauma.Common.Wizard;
using Content.Trauma.Server.Wizard.Systems;
using Content.Trauma.Shared.Teleportation;
using Content.Trauma.Shared.Wizard.FadingTimedDespawn;
using Content.Trauma.Shared.Wizard.Teleport;
using Robust.Shared.Audio;
using Robust.Shared.Physics;

namespace Content.Trauma.Server.Wizard.Teleport;

public sealed partial class WizardTeleportSystem : SharedWizardTeleportSystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SpellsSystem _spells = default!;
    [Dependency] private TeleportSystem _teleport = default!;
    [Dependency] private WizardRuleSystem _wizard = default!;
    [Dependency] private FadingTimedDespawnSystem _fadeDespawn = default!;

    private static readonly EntProtoId SmokeProto = "AdminInstantEffectSmoke10";

    private static readonly SoundSpecifier TeleportSound =
        new SoundPathSpecifier("/Audio/_Goobstation/Wizard/teleport_diss.ogg");

    private static readonly SoundSpecifier PostTeleportSound =
        new SoundPathSpecifier("/Audio/_Goobstation/Wizard/teleport_app.ogg");

    [SubscribeLocalEvent]
    private void OnLocationSelected(Entity<TeleportScrollComponent> ent, ref WizardTeleportLocationSelectedMessage args)
    {
        if (TryComp<ActionComponent>(ent, out var action) && !_actions.ValidAction((ent, action)))
            return;

        if (TryComp<LimitedChargesComponent>(ent, out var charges) && _charges.IsEmpty((ent, charges)))
            return;

        var user = args.Actor;
        var location = GetEntity(args.Location);

        if (!TryComp<WizardTeleportLocationComponent>(location, out var comp))
            return;

        if (!Teleport(user, location))
            return;

        _spells.SpeakSpell(user,
            user,
            Loc.GetString("action-speech-spell-teleport", ("location", comp.Location ?? Name(location))),
            MagicSchool.Translocation);

        if (action != null)
            _actions.StartUseDelay((ent, action));
        if (charges != null && !_charges.TryUseCharge((ent, charges)))
        {
            _popup.PopupEntity(Loc.GetString("teleport-scroll-no-charges"), user, user, PopupType.Medium);
            _ui.CloseUis(ent.Owner);

            // Don't Queuedel right away so that client doesn't throw debug assert exception
            _fadeDespawn.FadeDespawnEntity(ent, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        }
    }

    private bool Teleport(EntityUid user, EntityUid location)
    {
        var oldCoords = Transform(user).Coordinates;
        var coords = Transform(location).Coordinates;
        var soundOut = TeleportSound;
        var soundIn = PostTeleportSound;
        if (!_teleport.Teleport(user, coords, soundIn, soundOut, user: user, predicted: false))
            return false;

        Spawn(SmokeProto, oldCoords);
        Spawn(SmokeProto, coords);
        return true;
    }

    public override void OnTeleportSpell(EntityUid performer, EntityUid action)
    {
        var key = WizardTeleportUiKey.Key;
        if (!_ui.TryToggleUi(action, key, performer))
            return;

        var state = new WizardTeleportState(GetWizardTeleportLocations());
        _ui.SetUiState(action, key, state);
    }

    [SubscribeLocalEvent]
    private void OnAfterUIOpen(Entity<TeleportScrollComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        var key = WizardTeleportUiKey.Key;
        if (!_ui.HasUi(ent, key))
            return;

        var state = new WizardTeleportState(GetWizardTeleportLocations());
        _ui.SetUiState(ent.Owner, key, state);
    }

    [SubscribeLocalEvent(after: [typeof(NavMapSystem)])]
    private void OnTeleportWarpMapInit(Entity<WizardTeleportWarpPointComponent> ent, ref MapInitEvent args)
    {
        var uid = ent.Owner;

        if (!TryComp(uid, out WarpPointComponent? warp))
            return;

        if (!TryComp(uid, out TransformComponent? xform))
            return;

        if (_wizard.GetWizardTargetStationGrids().Where(x => x != null).All(x => xform.ParentUid != x))
            return;

        if (!CanTeleportTo(xform))
            return;

        var teleportLocation = Spawn(null, _transform.GetMapCoordinates(uid, xform));
        EnsureComp<WizardTeleportLocationComponent>(teleportLocation).Location = warp.Location;
        _transform.AttachToGridOrMap(teleportLocation);
    }

    private List<WizardWarp> GetWizardTeleportLocations()
    {
        var list = new List<WizardWarp>();
        var allQuery = AllEntityQuery<WizardTeleportLocationComponent, TransformComponent>();
        while (allQuery.MoveNext(out var uid, out var location, out var xform))
        {
            if (CanTeleportTo(xform))
                list.Add(new(GetNetEntity(uid), location.Location ?? Name(uid)));
        }

        return list;
    }

    private bool CanTeleportTo(TransformComponent xform)
    {
        foreach (var (_, fix) in _lookup.GetEntitiesInRange<FixturesComponent>(xform.Coordinates,
                     0.1f,
                     LookupFlags.Static))
        {
            if (fix.Fixtures.Any(x => x.Value.Hard && (x.Value.CollisionLayer & (int) CollisionGroup.Impassable) != 0))
                return false;
        }

        return true;
    }
}
