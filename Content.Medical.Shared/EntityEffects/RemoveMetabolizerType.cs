// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.EntityEffects;
using Content.Shared.EntityEffects;
using Content.Shared.Metabolism;
using Robust.Shared.Prototypes;

namespace Content.Medical.Server.EntityEffects;

/// <summary>
/// Removes a metabolizer type from the target organ entity.
/// </summary>
public sealed partial class RemoveMetabolizerType : EntityEffectBase<RemoveMetabolizerType>
{
    [DataField(required: true)]
    public ProtoId<MetabolizerTypePrototype> Type;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null; // idc
}

public sealed class RemoveMetabolizerTypeEffectSystem : EntityEffectSystem<MetabolizerComponent, RemoveMetabolizerType>
{
    protected override void Effect(Entity<MetabolizerComponent> ent, ref EntityEffectEvent<RemoveMetabolizerType> args)
    {
        var type = args.Effect.Type;
        if (ent.Comp.MetabolizerTypes is {} types && types.Contains(type))
            types.Remove(type);
    }
}
