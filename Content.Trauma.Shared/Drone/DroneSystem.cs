// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Emoting;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.UserInterface;
using Content.Shared.Whitelist;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Drone;

public abstract partial class DroneSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private HashSet<Entity<MindContainerComponent>> _mobs = new();

    [SubscribeLocalEvent]
    private void OnUseAttempt(Entity<DroneComponent> ent, ref UseAttemptEvent args)
    {
        var blacklisted = _whitelist.IsWhitelistPass(ent.Comp.Blacklist, args.Used);
        if (FindNonDrone(ent) is not { } viewer)
        {
            if (blacklisted)
            {
                TryPopup(ent, "drone-cant-use", null);
                args.Cancel();
            }
            return;
        }

        if (blacklisted) // blacklist. this one *does* prevent actions. it would probably be best if this read from the component or something.
        {
            args.Cancel();
            TryPopup(ent, "drone-cant-use-nearby", viewer);
        }
        else if (_whitelist.IsWhitelistPass(ent.Comp.Whitelist, args.Used)) /// whitelist. sends proximity warning popup if the item isn't whitelisted. Doesn't prevent actions.
		{
            TryPopup(ent, "drone-too-close", viewer);
        }
    }

    [SubscribeLocalEvent]
    private void OnMindAdded(EntityUid uid, DroneComponent drone, MindAddedMessage args)
    {
        UpdateDroneAppearance(uid, true);
        _popup.PopupEntity(Loc.GetString("drone-activated"), uid, PopupType.Large);
    }

    [SubscribeLocalEvent]
    private void OnActivateUIAttempt(EntityUid uid, DroneComponent component, UserOpenActivatableUIAttemptEvent args)
    {
        if (_whitelist.IsWhitelistPass(component.Blacklist, args.Target))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnExamined(EntityUid uid, DroneComponent component, ExaminedEvent args)
    {
        if (TryComp<MindContainerComponent>(uid, out var mind) && mind.HasMind)
        {
            args.PushMarkup(Loc.GetString("drone-active"));
        }
        else
        {
            args.PushMarkup(Loc.GetString("drone-dormant"));
        }
    }

    [SubscribeLocalEvent]
    private void OnEmoteAttempt(Entity<DroneComponent> ent, ref EmoteAttemptEvent args)
    {
        // No.
        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnThrowAttempt(Entity<DroneComponent> ent, ref ThrowAttemptEvent args)
    {
        args.Cancel();
    }

    protected void UpdateDroneAppearance(EntityUid uid, bool on)
    {
        _appearance.SetData(uid, DroneVisuals.Status, on);
    }

    private EntityUid? FindNonDrone(Entity<DroneComponent> ent)
    {
        var coords = Transform(ent).Coordinates;
        _mobs.Clear();
        _lookup.GetEntitiesInRange(coords, ent.Comp.InteractionBlockRange, _mobs);
        foreach (var entity in _mobs)
        {
            // Require the entity to be controlled by a player and not a drone or ghost.
            if (!entity.Comp.HasMind || HasComp<DroneComponent>(entity) || HasComp<GhostComponent>(entity))
                continue;

            // filter out all dead entities.
            if (_mob.IsDead(entity.Owner))
                continue;

            // !
            return entity;
        }

        return null;
    }

    private void TryPopup(Entity<DroneComponent> ent, LocId loc, EntityUid? nearest)
    {
        var now = _timing.CurTime;
        if (now < ent.Comp.NextProximityAlert)
            return;

        ent.Comp.NextProximityAlert = now + ent.Comp.ProximityDelay;
        DirtyField(ent, ent.Comp, nameof(DroneComponent.NextProximityAlert));
        _popup.PopupEntity(Loc.GetString(loc, ("being", nearest ?? ent.Owner)), ent, ent);
    }
}

[Serializable, NetSerializable]
public enum DroneVisuals : byte
{
    Status
}
