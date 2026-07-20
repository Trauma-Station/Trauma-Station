using Robust.Shared.Prototypes;

namespace Content.Shared.Polymorph.Systems;

/// <summary>
/// Trauma - Shared polymorph system because nobody is refactoring this shit for the next 10 years.
/// Not predicted at all, everything returns null.
/// </summary>
public abstract class SharedPolymorphSystem : EntitySystem
{
    public virtual EntityUid? PolymorphEntity(EntityUid uid, ProtoId<PolymorphPrototype> protoId)
        => null;

    public virtual EntityUid? PolymorphEntity(EntityUid uid, PolymorphConfiguration configuration)
        => null;

    public virtual EntityUid? RevertPolymorph(EntityUid uid)
        => null;
}
