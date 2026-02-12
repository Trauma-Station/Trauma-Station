// <Trauma>
// TODO: move this shit out of here it has literally no reason to be here
using Content.Shared.Cuffs;
using Content.Shared.Heretic;
using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
// </Trauma>
using Content.Shared.Atmos;
using Content.Shared.Camera;
using Content.Shared.Hands.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<HandsComponent, GetEyeOffsetRelayedEvent>(RelayEvent);
        SubscribeLocalEvent<HandsComponent, GetEyePvsScaleRelayedEvent>(RelayEvent);
        SubscribeLocalEvent<HandsComponent, RefreshMovementSpeedModifiersEvent>(RelayEvent);
        SubscribeLocalEvent<HandsComponent, CheckMagicItemEvent>(RelayEvent); // goob edit - heretics

        // By-ref events.
        SubscribeLocalEvent<HandsComponent, ExtinguishEvent>(RefRelayEvent);
        SubscribeLocalEvent<HandsComponent, ProjectileReflectAttemptEvent>(RefRelayEvent);
        SubscribeLocalEvent<HandsComponent, HitScanReflectAttemptEvent>(RefRelayEvent);
        SubscribeLocalEvent<HandsComponent, WieldAttemptEvent>(RefRelayEvent);
        SubscribeLocalEvent<HandsComponent, UnwieldAttemptEvent>(RefRelayEvent);
        SubscribeLocalEvent<HandsComponent, TargetHandcuffedEvent>(RefRelayEvent);

        SubscribeLocalEvent<HandsComponent, RefreshEquipmentHudEvent<ShowHealthBarsComponent>>(RefRelayEvent); // goob edit - heretics
        SubscribeLocalEvent<HandsComponent, RefreshEquipmentHudEvent<ShowHealthIconsComponent>>(RefRelayEvent); // goob edit - heretics
    }

    public void RelayEvent<T>(Entity<HandsComponent> entity, ref T args) where T : EntityEventArgs // Trauma - made public
    {
        CoreRelayEvent(entity, ref args);
    }

    public void RefRelayEvent<T>(Entity<HandsComponent> entity, ref T args) // Trauma - made public
    {
        var ev = CoreRelayEvent(entity, ref args);
        args = ev.Args;
    }

    private HeldRelayedEvent<T> CoreRelayEvent<T>(Entity<HandsComponent> entity, ref T args)
    {
        var ev = new HeldRelayedEvent<T>(args);

        foreach (var held in EnumerateHeld(entity.AsNullable()))
        {
            RaiseLocalEvent(held, ref ev);
        }

        return ev;
    }
}
