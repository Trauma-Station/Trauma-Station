// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Targeting;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class TargetedDamage : EntityEffectBase<TargetedDamage>
{
    /// <summary>
    /// The amount of damage to deal.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null; // idc
}

public sealed class TargetedDamageEffectSystem : EntityEffectSystem<TransformComponent, TargetedDamage>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<TargetedDamage> args)
    {
        var damage = new DamageSpecifier(args.Effect.Damage);

        damage *= args.Scale;

        TargetBodyPart targetPart = TargetBodyPart.Chest;
        if (args.User is { } user && TryComp<TargetingComponent>(user, out var comp))
        {
            targetPart = comp.Target;
        }

        _damageable.TryChangeDamage(ent.Owner, damage, true, targetPart: targetPart);
    }
}
