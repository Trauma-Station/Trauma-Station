// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Attribute.Systems;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Grants minimum skill levels to the target mob.
/// </summary>
public sealed partial class GrantAttributes : EntityEffectBase<GrantAttributes>
{
    /// <summary>
    /// Each skill and the minimum level to ensure the target has.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Attributes = new();

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed class GrantAttributesEffectSystem : EntityEffectSystem<KnowledgeHolderComponent, GrantAttributes>
{
    [Dependency] private readonly SharedAttributeSystem _attribute = default!;

    protected override void Effect(Entity<KnowledgeHolderComponent> ent, ref EntityEffectEvent<GrantAttributes> args)
    {
        _attribute.AddAttributeUnits(ent, args.Effect.Attributes);
    }
}
