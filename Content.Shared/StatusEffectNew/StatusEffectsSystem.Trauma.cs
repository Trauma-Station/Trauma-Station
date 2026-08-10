using Robust.Shared.Prototypes;

namespace Content.Shared.StatusEffectNew;

public sealed partial class StatusEffectsSystem
{
    public void AddEffects(EntityUid target, IReadOnlyList<EntProtoId> effects)
    {
        foreach (var id in effects)
        {
            TryAddStatusEffect(target, id, out _);
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
