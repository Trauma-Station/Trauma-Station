// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body.Prototypes;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Adds a metabolizer type to the target entity, which must be a <c>Metabolizer</c> organ.
/// </summary>
public sealed partial class AddMetabolizerType : EntityEffectBase<AddMetabolizerType>
{
    [DataField(required: true)]
    public ProtoId<MetabolizerTypePrototype> Type;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null; // idc it's not used directly in a reagent right now
}

/// <summary>
/// Removes a metabolizer type from the target entity, which must be a <c>Metabolizer</c> organ.
/// </summary>
public sealed partial class RemoveMetabolizerType : EntityEffectBase<RemoveMetabolizerType>
{
    [DataField(required: true)]
    public ProtoId<MetabolizerTypePrototype> Type;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null; // idc it's not used directly in a reagent right now
}
