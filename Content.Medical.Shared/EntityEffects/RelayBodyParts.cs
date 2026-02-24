// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.EntityEffects;

/// <summary>
/// Relays entity effects to all body parts of a given type, or all parts.
/// Target must be a body.
/// </summary>
public sealed partial class RelayBodyParts : EntityEffectBase<RelayBodyParts>
{
    /// <summary>
    /// The body part type to run effects on.
    /// It will run on all of them if there are multiple.
    /// If this is null it will run on all body parts.
    /// </summary>
    [DataField]
    public BodyPartType? PartType;

    /// <summary>
    /// Optional part symmetry to require.
    /// </summary>
    [DataField]
    public BodyPartSymmetry? Symmetry;

    /// <summary>
    /// Text to use for the guidebook entry for reagents.
    /// </summary>
    [DataField]
    public LocId? GuidebookText;

    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => GuidebookText is {} key ? Loc.GetString(key, ("chance", Probability)) : null;
}

public sealed class RelayBodyPartsEffectSystem : EntityEffectSystem<BodyComponent, RelayBodyParts>
{
    [Dependency] private readonly BodyPartSystem _part = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<RelayBodyParts> args)
    {
        var effect = args.Effect;
        var effects = effect.Effects;
        var partType = effect.PartType;
        var symmetry = effect.Symmetry;
        foreach (var part in _part.GetBodyParts(ent.AsNullable(), partType, symmetry))
        {
            _effects.ApplyEffects(part, effects, args.Scale, args.User);
        }
    }
}
