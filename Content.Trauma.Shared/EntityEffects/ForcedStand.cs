using Content.Shared.EntityEffects;
using Content.Shared.Standing;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;
public sealed partial class ForcedStand : EntityEffectBase<ForcedStand>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null; // idc
}

public sealed class ForcedStandEffectSystem : EntityEffectSystem<TransformComponent, ForcedStand>
{
    [Dependency] private readonly StandingStateSystem _standing = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<ForcedStand> args)
    {
        _standing.Stand(ent.Owner);
    }
}
