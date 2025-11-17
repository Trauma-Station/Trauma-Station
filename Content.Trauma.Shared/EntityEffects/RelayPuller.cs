using Content.Shared.EntityEffects;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Applies an effect to the entity pulling this one.
/// </summary>
public sealed partial class RelayPuller : EntityEffectBase<RelayPuller>
{
    /// <summary>
    /// Effect to apply to the Puller entity.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect Effect = default!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-relay-puller", ("chance", Probability), ("effect", Effect.EntityEffectGuidebookText(prototype, entSys) ?? string.Empty));
}

public sealed class RelayPullerEffectSystem : EntityEffectSystem<PullableComponent, RelayPuller>
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<PullableComponent> ent, ref EntityEffectEvent<RelayPuller> args)
    {
        if (ent.Comp.Puller is {} puller)
            _effects.TryApplyEffect(puller, args.Effect.Effect, args.Scale);
    }
}
