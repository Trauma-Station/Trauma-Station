using Content.Shared.EntityEffects;
using Content.Shared.Implants.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// For an implant, applies an effect to the entity it's implanted in.
/// </summary>
public sealed partial class RelayImplanted : EntityEffectBase<RelayImplanted>
{
    /// <summary>
    /// Effect to apply to the implanted entity.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect Effect = default!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-relay-implanted", ("chance", Probability), ("effect", Effect.EntityEffectGuidebookText(prototype, entSys) ?? string.Empty));
}

public sealed class RelayImplantedEffectSystem : EntityEffectSystem<SubdermalImplantComponent, RelayImplanted>
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<SubdermalImplantComponent> ent, ref EntityEffectEvent<RelayImplanted> args)
    {
        if (ent.Comp.ImplantedEntity is {} user)
            _effects.TryApplyEffect(user, args.Effect.Effect, args.Scale);
    }
}
