// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Moves an organ from one body part to another.
/// The target entity must be the body.
/// </summary>
public sealed partial class MoveOrgan : EntityEffectBase<MoveOrgan>
{
    [DataField(required: true)]
    public string Slot = string.Empty;

    [DataField(required: true)]
    public BodyPartType Src;

    /// <summary>
    /// The part type to move the organ into.
    /// </summary>
    [DataField(required: true)]
    public BodyPartType Dest;

    /// <summary>
    /// Optional symmetry to use for both parts.
    /// </summary>
    [DataField]
    public BodyPartSymmetry? Symmetry;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-move-organ", ("chance", Probability), ("slot", Slot), ("src", Src), ("dest", Dest));
}

public sealed class MoveOrganEffectSystem : EntityEffectSystem<BodyComponent, MoveOrgan>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<MoveOrgan> args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var effect = args.Effect;
        var symmetry = effect.Symmetry;
        var slot = effect.Slot;
        if (_body.FindPart(ent, effect.Src, symmetry) is not {} src ||
            _body.FindPart(ent, effect.Dest, symmetry) is not {} dest ||
            _body.FindPartOrgan(src, slot) is not {} organ)
            return;

        _body.RemoveOrgan(organ, organ.Comp);
        if (!_body.InsertOrgan(dest, organ, slot, dest.Comp, organ.Comp))
            Log.Error($"Failed to move organ {ToPrettyString(organ)} from {ToPrettyString(src)} to {ToPrettyString(dest)} in slot {slot}!");
    }
}
