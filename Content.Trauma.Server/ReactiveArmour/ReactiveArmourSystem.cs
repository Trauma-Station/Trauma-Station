// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Timing;
using Content.Shared.Inventory;
using Content.Shared.EntityEffects;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Projectiles;
using Content.Trauma.Shared.Projectiles;


namespace Content.Trauma.Server.ReactiveArmour;

public sealed partial class ReactiveArmourSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    //[Dependency] private LightningSystem _lightning = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ReactiveArmourComponent, InventoryRelayedEvent<AttackedEvent>>(OnHitMele);
        SubscribeLocalEvent<ReactiveArmourComponent, InventoryRelayedEvent<GotHitByProjectileEvent>>(OnHitProjectile);
    }

    // No. This hould not be predicted on client.
    // there has to be a better way to do this then making 3 methods for differen types of attacks...
    private void OnHitMele(EntityUid uid, ReactiveArmourComponent comp, InventoryRelayedEvent<AttackedEvent> args)
    {
        CheckForCooldown(args.Owner, comp);
    }

    private void OnHitProjectile(EntityUid uid, ReactiveArmourComponent comp, InventoryRelayedEvent<GotHitByProjectileEvent> args)
    {
        Console.WriteLine($"REACTIVE ARMOUR: uid {uid}");
        Console.WriteLine($"REACTIVE ARMOUR: args.Owner {args.Owner}");
        Console.WriteLine($"REACTIVE ARMOUR: args {args}");
        CheckForCooldown(args.Owner, comp);
    }

    // do we even have hitscan weapons?
    // private void OnHitscanHit(EntityUid uid, ref AttemptHitscanRaycastFiredEvent args)
    // {
    // }

    private void CheckForCooldown(EntityUid target, ReactiveArmourComponent comp)
    {
        if (_timing.CurTime < comp.LastActivated + comp.ActivationDelay)
            return;

        comp.LastActivated = _timing.CurTime;

        _effects.ApplyEffects(target, comp.Effects);
    }
}
