using Content.Shared.Whitelist; // Trauma
using Content.Shared.Damage;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed partial class DamageOnTriggerSystem : XOnTriggerSystem<DamageOnTriggerComponent>
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!; // Trauma
    [Dependency] private Damage.Systems.DamageableSystem _damageableSystem = default!;

    protected override void OnTrigger(Entity<DamageOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        // <Trauma>
        if (!_whitelist.CheckBoth(target, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;
        // </Trauma>

        var damage = new DamageSpecifier(ent.Comp.Damage);
        var ev = new BeforeDamageOnTriggerEvent(damage, target);
        RaiseLocalEvent(ent.Owner, ref ev);

        // Trauma - added targetPart
        args.Handled |= _damageableSystem.TryChangeDamage(target, ev.Damage, ent.Comp.IgnoreResistances, origin: ent.Owner, targetPart: ent.Comp.TargetPart);
    }
}

/// <summary>
/// Raised on an entity before it deals damage using DamageOnTriggerComponent.
/// Used to modify the damage that will be dealt.
/// </summary>
[ByRefEvent]
public record struct BeforeDamageOnTriggerEvent(DamageSpecifier Damage, EntityUid Tripper);
