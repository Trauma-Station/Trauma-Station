// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Consciousness;
using Content.Medical.Shared.Pain;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Medical.Shared.EntityEffects;

public sealed partial class AdjustPainFeels : EntityEffectBase<AdjustPainFeels>
{
    [DataField(required: true)]
    public FixedPoint2 Amount = default!;

    [DataField]
    public string ModifierIdentifier = "PainSuppressant";

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-suppress-pain", ("chance", Probability));
}

public sealed class AdjustPainFeelsEffectSystem : EntityEffectSystem<BodyComponent, AdjustPainFeels>
{
    [Dependency] private readonly ConsciousnessSystem _consciousness = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PainSystem _pain = default!;
    [Dependency] private readonly BodyPartSystem _part = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<AdjustPainFeels> args)
    {
        var scale = FixedPoint2.New(args.Scale);
        if (!_consciousness.TryGetNerveSystem(ent, out var nerveSys))
            return;

        var nerves = nerveSys.Value;
        var ident = args.Effect.ModifierIdentifier;
        var amount = args.Effect.Amount * scale;
        foreach (var bodyPart in _part.GetBodyParts(ent.AsNullable()))
        {
            // TODO SHITMED: predicted rng
            var add = _random.Prob(0.3f) ? amount : -amount;
            // TODO SHITMED: modifier isnt used bruh make it better
            if (_pain.TryGetPainFeelsModifier(bodyPart, nerves, ident, out var modifier))
                _pain.TryChangePainFeelsModifier(nerves, ident, bodyPart, add);
            else
                _pain.TryAddPainFeelsModifier(nerves, ident, bodyPart, add);
        }
    }
}
