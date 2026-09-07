// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Wounds;
using Content.Medical.Shared.Traumas;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;

namespace Content.Medical.Shared.EntityEffects;

/// <summary>
/// Evenly deals bone damage to each bone in the target mob.
/// The damage is split between them.
/// </summary>
public sealed partial class AdjustBoneDamage : EntityEffectBase<AdjustBoneDamage>
{
    [DataField(required: true)]
    public FixedPoint2 Amount;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-adjust-bone-damage", ("amount", Amount));
}

public sealed partial class AdjustBoneDamageEffectSystem : EntityEffectSystem<BodyComponent, AdjustBoneDamage>
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private TraumaSystem _trauma = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<AdjustBoneDamage> args)
    {
        var bones = _body.GetOrgans<BoneComponent>(ent.AsNullable());
        if (bones.Count == 0)
            return;

        var amount = args.Effect.Amount / bones.Count;
        foreach (var bone in bones)
        {
            // Yeah this is less efficient when theres not as many parts damaged but who tf cares,
            // its a bone medication so it should probs be strong enough to ignore this.
            _trauma.DamageBone(bone.AsNullable(), amount);
        }
    }
}
