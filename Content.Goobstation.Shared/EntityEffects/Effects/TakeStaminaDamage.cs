// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

public sealed partial class TakeStaminaDamage : EntityEffectBase<TakeStaminaDamage>
{
    /// <summary>
    /// How much stamina damage to take.
    /// </summary>
    [DataField]
    public int Amount = 10;

    /// <summary>
    /// Whether stamina damage should be applied immediately
    /// </summary>
    [DataField]
    public bool Immediate;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-deal-stamina-damage",
            ("immediate", Immediate),
            ("amount", MathF.Abs(Amount)),
            ("chance", Probability),
            ("deltasign", MathF.Sign(Amount)));
}

public sealed class TakeStaminaDamageSystem : EntityEffectSystem<StaminaComponent, TakeStaminaDamage>
{
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    protected override void Effect(Entity<StaminaComponent> ent, ref EntityEffectEvent<TakeStaminaDamage> args)
    {
        // TODO: wtf is this shitcode, investigate
        if (args.Scale != 1f)
            return;

        var amount = args.Effect.Amount;
        var immediate = args.Effect.Immediate;
        _stamina.TakeStaminaDamage(ent, amount, ent.Comp, visual: false, immediate: immediate);
    }
}
