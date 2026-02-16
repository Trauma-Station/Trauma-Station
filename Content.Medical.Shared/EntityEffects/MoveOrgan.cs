// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.EntityEffects;

/// <summary>
/// Moves an organ from one body part to another.
/// The parent organ will be changed.
/// The target entity must be the body.
/// </summary>
public sealed partial class MoveOrgan : EntityEffectBase<MoveOrgan>
{
    /// <summary>
    /// The slot of the organ to be moved.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Organ;

    /// <summary>
    /// The part to move the organ into.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Dest;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-move-organ", ("chance", Probability), ("organ", prototype.Index(Organ).Name), ("dest", prototype.Index(Dest).Name));
}

public sealed class MoveOrganEffectSystem : EntityEffectSystem<BodyComponent, MoveOrgan>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BodyCacheSystem _cache = default!;
    [Dependency] private readonly BodyPartSystem _part = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<MoveOrgan> args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var effect = args.Effect;
        var slot = effect.Organ;
        var body = ent.AsNullable();
        if (_body.GetOrgan(body, slot) is not {} organ ||
            _body.GetOrgan(body, effect.Dest) is not {} dest ||
            !_part.CanInsertOrgan(dest, slot) || // don't remove the original if it couldn't be inserted after
            !_body.RemoveOrgan(body, organ)) // the organ refused to be removed...
            return;

        // the child organ will refuse to be inserted without this, so set it to the new parent
        _cache.SetParentCategory(organ, effect.Dest);
        if (!_part.InsertOrgan(dest, organ)) // shouldn't fail...
            Log.Error($"Failed to move organ {ToPrettyString(organ)} from {ToPrettyString(body)} to {ToPrettyString(dest)} in slot {slot}!");
    }
}
