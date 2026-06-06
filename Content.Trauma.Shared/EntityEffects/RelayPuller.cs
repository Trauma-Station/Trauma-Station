// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Movement.Pulling.Components;

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

public sealed partial class RelayPullerEffectSystem : EntityEffectSystem<PullableComponent, RelayPuller>
{
    [Dependency] private EffectDataSystem _data = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<PullableComponent> ent, ref EntityEffectEvent<RelayPuller> args)
    {
        if (ent.Comp.Puller is not {} puller)
            return;

        _data.CopyData(ent, puller);
        _effects.TryApplyEffect(puller, args.Effect.Effect, args.Scale, args.User);
        _data.ClearData(puller);
    }
}
