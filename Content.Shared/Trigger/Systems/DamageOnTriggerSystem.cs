// <Trauma>
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
// </Trauma>
using Content.Shared.Damage;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed partial class DamageOnTriggerSystem : XOnTriggerSystem<DamageOnTriggerComponent>
{
    // <Trauma>
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    // </Trauma>
    [Dependency] private Damage.Systems.DamageableSystem _damageableSystem = default!;

    protected override void OnTrigger(Entity<DamageOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        // <Trauma>
        if (ent.Comp.TargetUser)
        {
            if (args.User is { } user)
                target = user;

            else if (_containers.TryGetContainingContainer(ent.Owner, out var container))
                target = container.Owner;

            else
                return;
        }

        if (!_whitelist.CheckBoth(target, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;
        // </Trauma>

        var damage = new DamageSpecifier(ent.Comp.Damage);
        var ev = new BeforeDamageOnTriggerEvent(damage, target);
        RaiseLocalEvent(ent.Owner, ref ev);

        var canMiss = ent.Comp.TargetPart == null; // Trauma

        args.Handled |= _damageableSystem.TryChangeDamage(target, ev.Damage, ent.Comp.IgnoreResistances, origin: ent.Owner, targetPart: ent.Comp.TargetPart, canMiss: canMiss); // Trauma - added targetPart and canMiss
    }
}

/// <summary>
/// Raised on an entity before it deals damage using DamageOnTriggerComponent.
/// Used to modify the damage that will be dealt.
/// </summary>
[ByRefEvent]
public record struct BeforeDamageOnTriggerEvent(DamageSpecifier Damage, EntityUid Tripper);
