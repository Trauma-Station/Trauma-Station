// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.EntityEffects;

/// <summary>
/// Relays entity effects to a single random body part picked from allowed types.
/// </summary>
public sealed partial class RelayRandomPart : EntityEffectBase<RelayRandomPart>
{
    /// <summary>
    /// The body part types to pick from.
    /// </summary>
    [DataField(required: true)]
    public BodyPartType[] Types = default!;

    /// <summary>
    /// Optional part symmetry to require.
    /// </summary>
    [DataField]
    public BodyPartSymmetry? PartSymmetry;

    /// <summary>
    /// Effect to apply to a random part.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect Effect = default!;

    /// <summary>
    /// Effect to apply to the target body if no valid bodyparts were found.
    /// </summary>
    [DataField]
    public EntityEffect? FailEffect;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-relay-random-part", ("effect", Effect.EntityEffectGuidebookText(prototype, entSys)!));
}

public sealed class RelayRandomPartEffectSystem : EntityEffectSystem<BodyComponent, RelayRandomPart>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly BodyPartSystem _part = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    private List<EntityUid> _parts = new();

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<RelayRandomPart> args)
    {
        var effect = args.Effect;
        var symmetry = effect.PartSymmetry;
        _parts.Clear();
        foreach (var partType in effect.Types)
        {
            foreach (var part in _part.GetBodyParts(ent, partType, symmetry))
            {
                _parts.Add(part);
            }
        }

        if (_parts.Count == 0) // no parts found
        {
            if (effect.FailEffect is {} fail)
                _effects.TryApplyEffect(ent, fail, args.Scale, args.User);
            return;
        }

        // TODO: PredictedRandom when it's real
        var seed = SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new Random(seed);
        var picked = rand.Pick(_parts);
        _effects.TryApplyEffect(picked, effect.Effect, args.Scale, args.User);
    }
}
