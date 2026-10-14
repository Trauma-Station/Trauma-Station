using Content.Shared.Damage.Events;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Timing.Components;
using Content.Shared.Timing.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Trauma.Shared.Samurai;

public sealed partial class EnergyPolearmSystem : EntitySystem
{
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private UseDelaySystem _delay = default!;

    [SubscribeLocalEvent]
    private void OnThrowHit(Entity<EnergyPolearmComponent> ent, ref ThrowDoHitEvent args)
    {
        if (ent.Comp.Mode == "staff")
        {
            _stun.KnockdownOrStun(args.Target, ent.Comp.ThrownStunTime);
        }
    }

    [SubscribeLocalEvent]
    private void OnHit(Entity<EnergyPolearmComponent> ent, ref MeleeHitEvent args)
    {
        if (ent.Comp.Mode == "staff")
        {
            if (args.Direction == null)
                return;

            foreach (var hit in args.HitEntities)
            {
                _stun.KnockdownOrStun(hit, ent.Comp.StaffWideSwingStunTime);
            }

            if (args.HitEntities.Count > 0)
            {
                var length = ent.Comp.StaffWideSwingUseDelayPerTarget * args.HitEntities.Count + TimeSpan.FromSeconds(1);
                _delay.SetLength(ent.Owner, length, ent.Comp.StaffUseDelay);
                _delay.TryResetDelay(ent.Owner, id: ent.Comp.StaffUseDelay);
            }
        }
    }
}
