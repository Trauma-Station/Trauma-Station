// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body;
using Content.Medical.Shared.Wounds;
using Content.Medical.Shared.Traumas;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Medical.Shared.EntityEffects;

/// <summary>
/// Evenly deals bone damage to each bone in the target mob.
/// The damage is split between them.
/// </summary>
public sealed partial class AdjustBoneDamage : EntityEffectBase<AdjustBoneDamage>
{
    [DataField(required: true)]
    public FixedPoint2 Amount = default!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-adjust-bone-damage", ("amount", Amount));
}

public sealed class AdjustBoneDamageEffectSystem : EntityEffectSystem<BodyComponent, AdjustBoneDamage>
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly TraumaSystem _trauma = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<AdjustBoneDamage> args)
    {
        var parts = _body.GetOrgans<WoundableComponent>(ent.AsNullable());
        if (parts.Count == 0)
            return;

        var amount = args.Effect.Amount / parts.Count;
        foreach (var part in parts)
        {
            if (_trauma.GetBone(part.AsNullable()) is not {} bone)
                continue;

            // Yeah this is less efficient when theres not as many parts damaged but who tf cares,
            // its a bone medication so it should probs be strong enough to ignore this.
            _trauma.ApplyDamageToBone(bone, amount, bone.Comp);
        }
    }
}
