// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.EntityEffects;
using Content.Shared.EntityEffects;
using Content.Shared.Metabolism;
using Robust.Shared.Prototypes;

namespace Content.Medical.Server.EntityEffects;

/// <summary>
/// Adds a metabolizer type to the target organ entity.
/// </summary>
public sealed partial class AddMetabolizerType : EntityEffectBase<AddMetabolizerType>
{
    [DataField(required: true)]
    public ProtoId<MetabolizerTypePrototype> Type;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null; // idc
}

public sealed class AddMetabolizerTypeEffectSystem : EntityEffectSystem<MetabolizerComponent, AddMetabolizerType>
{
    protected override void Effect(Entity<MetabolizerComponent> ent, ref EntityEffectEvent<AddMetabolizerType> args)
    {
        ent.Comp.MetabolizerTypes ??= new();
        ent.Comp.MetabolizerTypes.Add(args.Effect.Type);
    }
}
