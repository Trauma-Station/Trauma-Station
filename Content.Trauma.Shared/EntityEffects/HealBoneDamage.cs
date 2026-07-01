// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Used in yaml to set how much bone damage a reagent or chemical heals.
/// </summary>
public sealed partial class HealBoneDamage : EntityEffectBase<HealBoneDamage>
{
    [DataField]
    public FixedPoint2 Amount = 1;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-heal-bone-damage", ("chance", Probability), ("amount", Amount.Float()));
}

/// <summary>
/// Heals bone damage on every body part with a bone.
/// </summary>
public sealed partial class HealBoneDamageEffectSystem : EntityEffectSystem<BodyComponent, HealBoneDamage>
{
    [Dependency] private TraumaSystem _trauma = default!;
    [Dependency] private BodySystem _body = default!;

    protected override void Effect(Entity<BodyComponent> entity, ref EntityEffectEvent<HealBoneDamage> args)
    {
        var amount = args.Effect.Amount * args.Scale;

        foreach (var woundable in _body.GetOrgans<WoundableComponent>(entity.Owner))
            _trauma.HealBone(woundable, amount);
    }
}
