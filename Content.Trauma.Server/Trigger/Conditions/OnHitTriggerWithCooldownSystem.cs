// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Timing;
using Content.Shared.Trigger.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Trauma.Shared.Projectiles;

namespace Content.Trauma.Server.Trigger.Conditions;

public sealed partial class OnHitTriggerWithCooldownSystem : EntitySystem
{
    [Dependency] private TriggerSystem _trigger = default!;
    [Dependency] private IGameTiming _timing = default!;
    //[Dependency] private LightningSystem _lightning = default!;
    // No. This hould not be predicted on client.
    // there has to be a better way to do this then making 3 methods for differen types of attacks...
    [SubscribeLocalEvent]
    private void OnHitMele(Entity<OnHitTriggerWithCooldownComponent> ent, ref AttackedEvent args)
    {
        CheckForCooldown(ent, ent.Comp.LastActivated, ent.Comp.ActivationDelay);
    }

    [SubscribeLocalEvent]
    private void OnHitProjectile(Entity<OnHitTriggerWithCooldownComponent> ent, ref HitByProjectileEvent args)
    {
        CheckForCooldown(Entity<OnHitTriggerWithCooldownComponent> ent);
    }

    [SubscribeLocalEvent]
    private void OnHitscanHit(Entity<OnHitTriggerWithCooldownComponent> ent, ref AttemptHitscanRaycastFiredEvent args)
    {
        CheckForCooldown(Entity<OnHitTriggerWithCooldownComponent> ent);
    }

    private void CheckForCooldown(Entity<OnHitTriggerWithCooldownComponent> ent)
    {
        if (_timing.CurTime < ent.Comp.LastActivated + ent.Comp.ActivationDelay)
        {
            Console.WriteLine($"REACTIVE ARMOUR: only {_timing.CurTime - ent.Comp.LastActivated} seconds passed");
            return;
        }

        ent.Comp.LastActivated = _timing.CurTime;

        _trigger.Trigger(ent);
    }
}
