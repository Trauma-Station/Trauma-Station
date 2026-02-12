// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Adds a marking to the target mob, on a specific organ.
/// </summary>
public sealed partial class AddMarking : EntityEffectBase<AddMarking>
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Organ;

    [DataField(required: true)]
    public ProtoId<MarkingPrototype> Marking;

    [DataField]
    public bool Forced;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-add-marking", ("chance", Probability), ("marking", prototype.Index(Marking).Name));
}

public sealed class AddMarkingEffectSystem : EntityEffectSystem<BodyComponent, AddMarking>
{
    [Dependency] private readonly BodySystem _body = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<AddMarking> args)
    {
        var e = args.Effect;
        _body.AddOrganMarking(ent.AsNullable(), e.Organ, e.Marking, e.Forced);
    }
}
