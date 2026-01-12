// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Relays entity effects to all body parts of a given type.
/// </summary>
public sealed partial class RelayBodyParts : EntityEffectBase<RelayBodyParts>
{
    /// <summary>
    /// The body part type to run effects on.
    /// It will run on all of them if there are multiple.
    /// </summary>
    [DataField(required: true)]
    public BodyPartType PartType;

    /// <summary>
    /// Optional part symmetry to require.
    /// </summary>
    [DataField]
    public BodyPartSymmetry? PartSymmetry;

    /// <summary>
    /// Text to use for the guidebook entry for reagents.
    /// </summary>
    [DataField(required: true)]
    public LocId GuidebookText;

    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString(GuidebookText, ("chance", Probability));
}

public sealed class RelayBodyPartsEffectSystem : EntityEffectSystem<BodyComponent, RelayBodyParts>
{
    [Dependency] private readonly EffectDataSystem _data = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<RelayBodyParts> args)
    {
        var effects = args.Effect.Effects;
        var partType = args.Effect.PartType;
        var symmetry = args.Effect.PartSymmetry;
        foreach (var (part, _) in _body.GetBodyChildrenOfType(ent, partType, ent.Comp, symmetry))
        {
            _data.CopyData(ent, part);
            _effects.ApplyEffects(part, effects, args.Scale);
            _data.ClearData(part);
        }
    }
}
