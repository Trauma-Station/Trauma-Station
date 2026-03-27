using Content.Medical.Common.Targeting;
using Content.Medical.Common.Wounds;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Surgery.Steps.Parts;
using Content.Medical.Shared.Wounds;
using Content.Shared._Goobstation.Wizard.Projectiles;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Throwing;
using Content.Trauma.Shared.BloodSplatter;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Heretic.EntityEffects;

public sealed partial class LockBladeEffect : EntityEffectBase<LockBladeEffect>
{
    [DataField]
    public SoundSpecifier WoundSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/blood3.ogg");

    [DataField]
    public SoundSpecifier OpeningSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/goresplat.ogg");
}

public sealed class LockBladeEffectSystem : EntityEffectSystem<BodyComponent, LockBladeEffect>
{
    [Dependency] private readonly WoundSystem _wound = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BodyPartSystem _part = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override void Effect(Entity<BodyComponent> target, ref EntityEffectEvent<LockBladeEffect> args)
    {
        var targeting = CompOrNull<TargetingComponent>(args.User);

        var (type, symmetry) = _body.ConvertTargetBodyPart(targeting?.Target ?? TargetBodyPart.Chest);
        if (_part.GetBodyParts(target, type, symmetry: symmetry).FirstOrNull() is not { } targetPart)
            return;

        if (!_wound.TryInduceWound(targetPart, "WeepingAvulsion", 25f, out _, damageGroup: "Brute"))
            return;

        var effectAmount = 1f;

        // Open ribcage for easier ascension if chest is mangled
        if (TryComp(targetPart, out WoundableComponent? woundable) && woundable.RootWoundable == targetPart &&
            woundable.WoundableSeverity >= WoundableSeverity.Mangled &&
            (!EnsureComp<SkinRetractedComponent>(targetPart, out _) |
             !EnsureComp<IncisionOpenComponent>(targetPart, out _) |
             !EnsureComp<BonesSawedComponent>(targetPart, out _) | !EnsureComp<BonesOpenComponent>(targetPart, out _)))
        {
            _audio.PlayPredicted(args.Effect.OpeningSound, target, args.User, AudioParams.Default.WithVolume(10f));
            effectAmount = 2;
        }
        else
            _audio.PlayPredicted(args.Effect.WoundSound, target, args.User);

        if (_net.IsClient || !TryComp(target, out BloodstreamComponent? bloodStream))
            return;

        effectAmount *= _random.Next(3, 6);

        var coords = _transform.GetMapCoordinates(target);
        var color = bloodStream.BloodReferenceSolution.GetColor(_proto);

        for (var i = 0; i < effectAmount; i++)
        {
            var dir = _random.NextAngle().ToVec();
            var chunk = Spawn("BloodChunkEffect", coords);
            var comp = EnsureComp<BloodSplatterOnLandComponent>(chunk);
            comp.Color = color;
            Dirty(chunk, comp);

            if (TryComp(chunk, out TrailComponent? trail))
            {
                trail.Color = color;
                Dirty(chunk, trail);
            }

            _throw.TryThrow(chunk,
                direction: dir * _random.NextVector2(1f, 3f),
                baseThrowSpeed: _random.NextFloat(1f, 2.5f),
                pushbackRatio: 0f,
                friction: 2f,
                recoil: false,
                playSound: false);
        }
    }
}
