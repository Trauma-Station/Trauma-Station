// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Breaks the target entity's pull, if it's being pulled.
/// </summary>
public sealed partial class StopPull : EntityEffectBase<StopPull>
{
    /// <summary>
    /// Whether to ignore grab stages.
    /// </summary>
    [DataField]
    public bool IgnoreGrab = true;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class StopPullEffectSystem : EntityEffectSystem<PullableComponent, StopPull>
{
    [Dependency] private PullingSystem _pulling = default!;

    protected override void Effect(Entity<PullableComponent> ent, ref EntityEffectEvent<StopPull> args)
    {
        _pulling.TryStopPull(ent, ent.Comp, user: args.User, ignoreGrab: args.Effect.IgnoreGrab);
    }
}
