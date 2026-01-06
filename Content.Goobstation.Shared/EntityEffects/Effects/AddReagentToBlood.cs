using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

/// <summary>
/// Trauma - Rewrote this shitcode and put it here instead of core files
/// </summary>
public sealed partial class AddReagentToBlood : EntityEffectBase<AddReagentToBlood>
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField(required: true)]
    public FixedPoint2 Amount = default!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var proto = prototype.Index(Reagent);
        return Loc.GetString("entity-effect-guidebook-add-to-chemicals",
            ("chance", Probability),
            ("deltasign", MathF.Sign(Amount.Float())),
            ("reagent", proto.LocalizedName),
            ("amount", MathF.Abs(Amount.Float())));
    }
}

public sealed class AddReagentToBloodEffectSystem : EntityEffectSystem<BloodstreamComponent, AddReagentToBlood>
{
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    protected override void Effect(Entity<BloodstreamComponent> ent, ref EntityEffectEvent<AddReagentToBlood> args)
    {
        var solution = new Solution();
        solution.AddReagent(args.Effect.Reagent, args.Effect.Amount);
        _bloodstream.TryAddToBloodstream(ent.AsNullable(), solution);
    }
}
