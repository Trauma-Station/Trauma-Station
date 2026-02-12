// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body;
using Content.Shared.Database;
using Content.Medical.Shared.Consciousness;
using Content.Medical.Shared.Pain;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Medical.Shared.EntityEffects;

public sealed partial class SuppressPain : EntityEffectBase<SuppressPain>
{
    [DataField(required: true)]
    public FixedPoint2 Amount = default!;

    [DataField(required: true)]
    public TimeSpan Time = default!;

    [DataField]
    public string ModifierIdentifier = "PainSuppressant";

    /// <summary>
    /// The body part to change the pain for.
    /// </summary>
    [DataField]
    public ProtoId<OrganCategoryPrototype> OrganCategory = "Torso";

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-suppress-pain");
}

public sealed class SuppressPainEffectSystem : EntityEffectSystem<BodyComponent, SuppressPain>
{
    [Dependency] private readonly ConsciousnessSystem _consciousness = default!;
    [Dependency] private readonly PainSystem _pain = default!;
    [Dependency] private readonly BodySystem _body = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<SuppressPain> args)
    {
        var scale = FixedPoint2.New(args.Scale);

        if (!_consciousness.TryGetNerveSystem(ent, out var nerveSys))
            return;

        var effect = args.Effect;
        if (_body.GetOrgan(ent.AsNullable(), effect.OrganCategory) is not {} organ)
            return;

        var nerves = nerveSys.Value;
        var ident = effect.ModifierIdentifier;
        var amount = effect.Amount * scale;
        var time = effect.Time;
        if (_pain.TryGetPainModifier(nerves, organ, ident, out var modifier))
            _pain.TryChangePainModifier(nerves, organ, ident, modifier.Value.Change - amount, time: time);
        else
            _pain.TryAddPainModifier(nerves, organ, ident, -amount, time: time);
    }
}
