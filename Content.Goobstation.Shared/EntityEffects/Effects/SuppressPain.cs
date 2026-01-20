using Content.Shared.Database;
using Content.Shared._Shitmed.Medical.Surgery.Consciousness.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Pain.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

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
    public BodyPartType PartType = BodyPartType.Head;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-suppress-pain");
}

public sealed class SuppressPainEffectSystem : EntityEffectSystem<BodyComponent, SuppressPain>
{
    [Dependency] private readonly ConsciousnessSystem _consciousness = default!;
    [Dependency] private readonly PainSystem _pain = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<SuppressPain> args)
    {
        var scale = FixedPoint2.New(args.Scale);

        if (!_consciousness.TryGetNerveSystem(ent, out var nerveSys))
            return;

        if (_body.GetBodyChildrenOfType(ent, args.Effect.PartType).FirstOrNull() is not {} part)
            return;

        var nerves = nerveSys.Value;
        var ident = args.Effect.ModifierIdentifier;
        var amount = args.Effect.Amount * scale;
        var time = args.Effect.Time;
        if (_pain.TryGetPainModifier(nerves, part.Id, ident, out var modifier))
            _pain.TryChangePainModifier(nerves, part.Id, ident, modifier.Value.Change - amount, time: time);
        else
            _pain.TryAddPainModifier(nerves, part.Id, ident, -amount, time: time);
    }
}
