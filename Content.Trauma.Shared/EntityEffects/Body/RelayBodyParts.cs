// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Relays entity effects to all body parts of a given type, or all parts.
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
    public BodyPartSymmetry? PartSymmetry;

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
    [Dependency] private readonly EffectDataSystem _data = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<RelayBodyParts> args)
    {
        var effect = args.Effect;
        var effects = effect.Effects;
        var symmetry = effect.PartSymmetry;
        if (effect.PartType is {} partType)
        {
            foreach (var (part, _) in _body.GetBodyChildrenOfType(ent, partType, ent.Comp, symmetry))
            {
                _data.CopyData(ent, part);
                _effects.ApplyEffects(part, effects, args.Scale);
                _data.ClearData(part);
            }
        }
        else
        {
            foreach (var (part, partComp) in _body.GetBodyChildren(ent, ent.Comp))
            {
                if (symmetry != null && partComp.Symmetry != symmetry)
                    continue;

                _data.CopyData(ent, part);
                _effects.ApplyEffects(part, effects, args.Scale);
                _data.ClearData(part);
            }
        }
    }
}
