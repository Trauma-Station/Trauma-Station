using Robust.Shared.Prototypes;

namespace Content.Shared.StatusEffectNew;

public sealed partial class StatusEffectsSystem
{
    /// <summary>
    /// Add a permanent status effect to an entity
    /// </summary>
    public void AddEffect(EntityUid target, [ForbidLiteral] EntProtoId id)
    {
        TryAddStatusEffect(target, id, out _);
    }

    /// <summary>
    /// Add a list of permanent status effects to an entity
    /// </summary>
    public void AddEffects(EntityUid target, IReadOnlyList<EntProtoId> effects)
    {
        foreach (var id in effects)
        {
            AddEffect(target, id);
        }
    }

    public void RemoveEffects(EntityUid target, IReadOnlyList<EntProtoId> effects)
    {
        foreach (var id in effects)
        {
            TryRemoveStatusEffect(target, id);
        }
    }
}
