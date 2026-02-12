// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Removes a marking from the target entity.
/// </summary>
public sealed partial class RemoveMarking : EntityEffectBase<RemoveMarking>
{
    /// <summary>
    /// The organ to look for the marking.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Organ;

    [DataField(required: true)]
    public ProtoId<MarkingPrototype> Marking;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-remove-marking", ("chance", Probability), ("marking", prototype.Index(Marking).Name));
}

public sealed class RemoveMarkingEffectSystem : EntityEffectSystem<BodyComponent, RemoveMarking>
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<RemoveMarking> args)
    {
        // TODO NUBODY: make better if an actual api is made
        if (_body.GetOrgan(ent.AsNullable(), args.Effect.Organ) is not {} organ ||
            !TryComp<VisualOrganMarkingsComponent>(organ, out var comp))
            return;

        var markings = comp.Markings;
        var marking = _proto.Index(args.Effect.Marking);
        if (!markings.TryGetValue(marking.BodyPart, out var list))
            return;

        list.RemoveAll(data => data.MarkingId == marking.ID);
        Dirty(organ, comp); // no fucking idea if this works :))))))
    }
}
