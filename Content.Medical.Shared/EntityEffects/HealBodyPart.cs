// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

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
    [Dependency] private readonly WoundSystem _wounds = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override void Effect(Entity<OrganComponent> ent, ref EntityEffectEvent<HealBodyPart> args)
    {
        _wounds.TryHealWoundsOnWoundable(ent.Owner, args.Effect.HealAmount * args.Scale, out _, damageGroup: _proto.Index(args.Effect.DamageGroup));
    }
}
