// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Heals a body part.
/// </summary>
public sealed partial class HealBodyPart : EntityEffectBase<HealBodyPart>
{
    public FixedPoint2 HealAmount = 10f;
    public ProtoId<DamageGroupPrototype> DamageGroup = "Brute";
}

public sealed class HealBodyPartEffectSystem : EntityEffectSystem<OrganComponent, HealBodyPart>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override void Effect(Entity<OrganComponent> ent, ref EntityEffectEvent<HealBodyPart> args)
    {
        var damage = new DamageSpecifier(_proto.Index(args.Effect.DamageGroup), -args.Effect.HealAmount * args.Scale);
        _damageable.TryChangeDamage(ent.Owner, damage);
    }
}
