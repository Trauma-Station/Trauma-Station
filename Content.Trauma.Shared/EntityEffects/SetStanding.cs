// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Shared.Standing;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Tries to make the target stand, or downs the target.
/// </summary>
public sealed partial class SetStanding : EntityEffectBase<SetStanding>
{
    /// <summary>
    /// Whether to stand or down the target.
    /// </summary>
    [DataField]
    public bool Standing = true;

    /// <summary>
    /// Force the target to stand/be downed.
    /// </summary>
    [DataField]
    public bool Force = false;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-set-standing", ("chance", Probability), ("standing", Standing));
}

public sealed class SetStandingEffectSystem : EntityEffectSystem<StandingStateComponent, SetStanding>
{
    [Dependency] private readonly StandingStateSystem _standing = default!;

    protected override void Effect(Entity<StandingStateComponent> ent, ref EntityEffectEvent<SetStanding> args)
    {
        var force = args.Effect.Force;
        if (args.Effect.Standing)
            _standing.Stand(ent, force: force, standingState: ent.Comp);
        else
            _standing.Down(ent, force: force, standingState: ent.Comp);
    }
}
