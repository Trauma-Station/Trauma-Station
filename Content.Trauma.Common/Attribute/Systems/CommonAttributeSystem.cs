using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.Attribute.Systems;

public abstract partial class CommonAttributeSystem : EntitySystem
{
    /// <summary>
    /// Gets a attribute unit based on its entity prototype ID.
    /// </summary>
    public abstract Entity<KnowledgeComponent>? GetAttribute(EntityUid target, [ForbidLiteral] EntProtoId knowledgeUnit);

    /// <summary>
    /// Clears attributes from the target entity.
    /// </summary>
    public abstract void ClearAttribute(EntityUid target, bool deleteAll);
}
