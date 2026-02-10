// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Adds a marking to the target entity.
/// </summary>
public sealed partial class AddMarking : EntityEffectBase<AddMarking>
{
    [DataField(required: true)]
    public ProtoId<MarkingPrototype> Marking;

    [DataField]
    public Color? Color;

    [DataField]
    public bool Forced;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-add-marking", ("chance", Probability), ("marking", prototype.Index(Marking).Name));
}

public sealed class AddMarkingEffectSystem : EntityEffectSystem<HumanoidAppearanceComponent, AddMarking>
{
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;

    protected override void Effect(Entity<HumanoidAppearanceComponent> ent, ref EntityEffectEvent<AddMarking> args)
    {
        var effect = args.Effect;
        _humanoid.AddMarking(ent, effect.Marking, effect.Color, forced: effect.Forced, humanoid: ent.Comp);
    }
}
