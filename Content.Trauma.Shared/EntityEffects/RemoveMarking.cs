// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Removes a marking from the target entity.
/// </summary>
public sealed partial class RemoveMarking : EntityEffectBase<RemoveMarking>
{
    [DataField(required: true)]
    public ProtoId<MarkingPrototype> Marking;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-remove-marking", ("chance", Probability), ("marking", prototype.Index(Marking).Name));
}

public sealed class RemoveMarkingEffectSystem : EntityEffectSystem<HumanoidAppearanceComponent, RemoveMarking>
{
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;

    protected override void Effect(Entity<HumanoidAppearanceComponent> ent, ref EntityEffectEvent<RemoveMarking> args)
    {
        _humanoid.RemoveMarking(ent, args.Effect.Marking, humanoid: ent.Comp);
    }
}
