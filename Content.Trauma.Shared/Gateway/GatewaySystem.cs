// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.UserInterface;
using Content.Shared.Access.Systems;
using Content.Shared.Popups;
using Content.Shared.Station.Systems;
using Content.Shared.Tag;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Gateway;

public sealed partial class GatewaySystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private LinkedEntitySystem _linkedEntity = default!;
    [Dependency] private MetaDataSystem _metadata = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StationSystem _stations = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private EntityQuery<GatewayComponent> _query = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<GatewayComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Enabled)
            UpdateAllGateways();
        UpdateAppearance(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<GatewayComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Enabled)
            UpdateAllGateways();
    }

    [SubscribeLocalEvent]
    private void OnUIOpenAttempt(Entity<GatewayComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!ent.Comp.Enabled || !ent.Comp.Interactable)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnUIOpened(Entity<GatewayComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUI(ent);
    }

    public void SetEnabled(Entity<GatewayComponent> ent, bool value)
    {
        if (ent.Comp.Enabled == value)
            return;

        ent.Comp.Enabled = value;
        Dirty(ent);
        UpdateAllGateways();
    }

    public void UpdateAllGateways()
    {
        var query = EntityQueryEnumerator<GatewayComponent>();
        foreach (var ent in query)
        {
            UpdateUI(ent);
        }
    }

    private void UpdateUI(Entity<GatewayComponent> ent)
    {
        // client cant predict destinations outside pvs
        if (_net.IsClient)
            return;

        var destinations = new List<GatewayDestinationData>();
        var query = AllEntityQuery<GatewayComponent, TransformComponent>();

        while (query.MoveNext(out var destUid, out var dest, out var destXform))
        {
            if (!dest.Enabled
                || destUid == ent.Owner
                || (ent.Comp.TagRestriction != null && !_tag.HasTag(destUid, ent.Comp.TagRestriction.Value)) // if we have a tag restriction and destination doesn't have it, abort
                || (dest.TagRestriction != null && !_tag.HasTag(ent, dest.TagRestriction.Value))) // if destination has a tag restriction but we don't have the tag, abort
                continue;

            destinations.Add(new GatewayDestinationData()
            {
                Entity = GetNetEntity(destUid),
                // Fallback to grid's ID if applicable.
                Name = dest.Name.IsEmpty && destXform.GridUid is { } grid ? FormattedMessage.FromUnformatted(Name(grid)) : dest.Name ,
                Portal = IsActive(destUid)
            });
        }

        _linkedEntity.GetLink(ent.Owner, out var current);

        var state = new GatewayBoundUserInterfaceState(
            destinations,
            GetNetEntity(current)
        );

        _ui.SetUiState(ent.Owner, GatewayUiKey.Key, state);
    }

    private void UpdateAppearance(EntityUid uid)
    {
        _appearance.SetData(uid, GatewayVisuals.Active, IsActive(uid));
    }

    [SubscribeLocalEvent]
    private void OnOpenPortal(Entity<GatewayComponent> ent, ref GatewayOpenPortalMessage args)
    {
        if (!ent.Comp.Interactable)
            return;

        var user = args.Actor;
        if (_timing.CurTime < ent.Comp.NextReady)
        {
            _popup.PopupCursor("This gateway isn't ready yet", user);
            return;
        }

        var dest = GetEntity(args.Destination);
        if (!ent.Comp.Enabled ||
            dest == ent.Owner ||
            !_query.TryComp(dest, out var destComp) ||
            IsActive(dest) ||
            !destComp.Enabled)
        {
            _popup.PopupCursor("The destination gateway is busy!", user, PopupType.SmallCaution);
            return;
        }

        // if the gateway has an access reader check it before allowing opening
        if (CheckAccess(user, ent.AsNullable()))
            return;

        // TODO: admin log???
        ClosePortal(ent.AsNullable(), user);
        OpenPortal(ent, (dest, destComp), user);
    }

    private void OpenPortal(Entity<GatewayComponent> ent, Entity<GatewayComponent> dest, EntityUid user)
    {
        _linkedEntity.TryLink(ent.Owner, dest.Owner);

        var sourcePortal = EnsureComp<PortalComponent>(ent);
        var targetPortal = EnsureComp<PortalComponent>(dest);

        sourcePortal.CanTeleportToOtherMaps = true;
        targetPortal.CanTeleportToOtherMaps = true;

        sourcePortal.RandomTeleport = false;
        targetPortal.RandomTeleport = false;

        // for ui
        ent.Comp.NextReady = _timing.CurTime + ent.Comp.Cooldown;
        Dirty(ent);

        _audio.PlayPredicted(ent.Comp.OpenSound, ent, user);
        _audio.PlayPredicted(dest.Comp.OpenSound, dest, user);

        UpdateUI(ent);
        UpdateAppearance(ent);
        UpdateAppearance(dest);
    }

    private void ClosePortal(Entity<GatewayComponent?> ent, EntityUid user)
    {
        if (!_query.Resolve(ent, ref ent.Comp))
            return;

        RemComp<PortalComponent>(ent);
        if (!_linkedEntity.GetLink(ent.Owner, out var dest))
            return;

        if (_query.TryComp(dest, out var destComp))
        {
            // portals closed, put it on cooldown and let it eventually be opened again
            destComp.NextReady = _timing.CurTime + destComp.Cooldown;
            Dirty(dest.Value, destComp);
        }

        _audio.PlayPredicted(ent.Comp.CloseSound, ent, user);
        _audio.PlayPredicted(ent.Comp.CloseSound, dest.Value, user);

        _linkedEntity.TryUnlink(ent.Owner, dest.Value);
        RemComp<PortalComponent>(dest.Value);

        UpdateUI((ent, ent.Comp));
        UpdateAppearance(ent);
        UpdateAppearance(dest.Value);
    }

    private void TryClose(EntityUid uid, EntityUid user)
    {
        // portal already closed so cant close it
        if (!_linkedEntity.GetLink(uid, out var source))
            return;

        // not allowed to close it
        if (CheckAccess(user, source.Value))
            return;

        ClosePortal(source.Value, user);
    }

    /// <summary>
    /// Checks the user's access. Makes popup and plays sound if missing access.
    /// Returns whether access was missing.
    /// </summary>
    private bool CheckAccess(EntityUid user, Entity<GatewayComponent?> ent)
    {
        if (!_query.Resolve(ent, ref ent.Comp))
            return false;

        if (_access.IsAllowed(user, ent.Owner))
            return false;

        _popup.PopupEntity(Loc.GetString("gateway-access-denied"), user, user);
        _audio.PlayPredicted(ent.Comp.AccessDeniedSound, ent, user);
        return true;
    }

    private bool IsActive(EntityUid uid)
        => _linkedEntity.GetLink(uid, out _);
}
