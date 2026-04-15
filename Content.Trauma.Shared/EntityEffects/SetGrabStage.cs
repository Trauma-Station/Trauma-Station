// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Trauma.Common.MartialArts;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Sets the target's grab stage, does nothing if it isn't being pulled.
/// </summary>
public sealed partial class SetGrabStage : EntityEffectBase<SetGrabStage>
{
    /// <summary>
    /// Grab stage to set to.
    /// </summary>
    [DataField(required: true)]
    public GrabStage Stage;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed class SetGrabStageEffectSystem : EntityEffectSystem<PullableComponent, SetGrabStage>
{
    [Dependency] private readonly PullingSystem _pulling = default!;

    protected override void Effect(Entity<PullableComponent> ent, ref EntityEffectEvent<SetGrabStage> args)
    {
        if (ent.Comp.Puller is not {} puller || !TryComp<PullerComponent>(puller, out var pullerComp))
            return;

        _pulling.TrySetGrabStages((puller, pullerComp), ent, args.Effect.Stage);
    }
}
