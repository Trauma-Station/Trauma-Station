using Content.Goobstation.Shared.Disease.Chemistry;
using Content.Goobstation.Shared.Disease.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

/// <summary>
/// Modifies the entity's immunity's strength, with accumulation.
/// </summary>
public sealed partial class ImmunityModifier : EntityEffectBase<ImmunityModifier>
{
    /// <summary>
    /// How much to add to the immunity's gain rate.
    /// </summary>
    [DataField]
    public float GainRateModifier = 0.002f;

    /// <summary>
    /// How much to add to the immunity's strength.
    /// </summary>
    [DataField]
    public float StrengthModifier = 0.02f;

    /// <summary>
    /// How long the modifier applies.
    /// Is scaled by reagent amount if used with an EntityEffectReagentArgs.
    /// </summary>
    [DataField]
    public TimeSpan StatusLifetime = TimeSpan.FromSeconds(2);

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-immunity-modifier",
            ("chance", Probability),
            ("gainrate", GainRateModifier),
            ("strength", StrengthModifier),
            ("time", StatusLifetime.TotalSeconds));
}

public sealed class ImmunityModifierEffectSystem : EntityEffectSystem<DiseaseCarrierComponent, ImmunityModifier>
{
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Remove reagent at set rate, changes the immunity modifiers and adds a ImmunityModifierMetabolismComponent if not already there.
    /// </summary>
    protected override void Effect(Entity<DiseaseCarrierComponent> ent, ref EntityEffectEvent<ImmunityModifier> args)
    {
        var status = EnsureComp<ImmunityModifierMetabolismComponent>(ent);

        status.GainRateModifier = args.Effect.GainRateModifier;
        status.StrengthModifier = args.Effect.StrengthModifier;

        // only going to scale application time
        var statusLifetime = args.Effect.StatusLifetime * args.Scale;

        var now = _timing.CurTime;
        if (status.ModifierTimer < now)
            status.ModifierTimer = now;
        status.ModifierTimer += statusLifetime;
        Dirty(ent, status);
    }
}
