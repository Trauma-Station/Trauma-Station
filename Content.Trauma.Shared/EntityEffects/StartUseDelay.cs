// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Shared.Timing;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that starts a use delay on the target entity.
/// </summary>
public sealed partial class StartUseDelay : EntityEffectBase<StartUseDelay>
{
    /// <summary>
    /// The specific use delay to check.
    /// </summary>
    [DataField]
    public string DelayId = UseDelaySystem.DefaultId;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-start-use-delay", ("chance", Probability), ("id", DelayId));
}

public sealed class StartUseDelayEffectSystem : EntityEffectSystem<UseDelayComponent, StartUseDelay>
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    protected override void Effect(Entity<UseDelayComponent> ent, ref EntityEffectEvent<StartUseDelay> args)
    {
        var id = args.Effect.DelayId;
        _useDelay.TryResetDelay((ent, ent.Comp), id: id);
    }
}
