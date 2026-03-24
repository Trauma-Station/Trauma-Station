using Content.Goobstation.Shared.Teleportation.Systems;
using Content.Medical.Common.Wounds;
using Content.Medical.Shared.Surgery.Steps.Parts;
using Content.Medical.Shared.Wounds;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared._Goobstation.Wizard.Projectiles;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Teleportation;
using Content.Shared.Throwing;
using Content.Trauma.Shared.Heretic.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed class HereticBladeSystem : SharedHereticBladeSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly SharedRandomTeleportSystem _teleport = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _sol = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly WoundSystem _wound = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void ApplyLockBladeEffect(EntityUid target, EntityUid targetPart, float probability)
    {
        base.ApplyLockBladeEffect(target, targetPart, probability);

        if (!_random.Prob(probability))
            return;

        if (!_wound.TryInduceWound(targetPart, "WeepingAvulsion", 25f, out _, damageGroup: "Brute"))
            return;

        var effectAmount = _random.Next(3, 6);

        // Open ribcage for easier ascension if chest is mangled
        if (TryComp(targetPart, out WoundableComponent? woundable) && woundable.RootWoundable == targetPart &&
            woundable.WoundableSeverity >= WoundableSeverity.Mangled &&
            (!EnsureComp<SkinRetractedComponent>(targetPart, out _) |
             !EnsureComp<IncisionOpenComponent>(targetPart, out _) |
             !EnsureComp<BonesSawedComponent>(targetPart, out _) | !EnsureComp<BonesOpenComponent>(targetPart, out _)))
        {
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Goobstation/Heretic/goresplat.ogg"),
                target,
                AudioParams.Default.WithVolume(10f));
            effectAmount *= 2;
        }
        else
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Goobstation/Heretic/blood3.ogg"), target);

        if (!TryComp(target, out BloodstreamComponent? bloodStream))
            return;

        var coords = _transform.GetMapCoordinates(target);
        var color = bloodStream.BloodReferenceSolution.GetColor(_proto);

        for (var i = 0; i < effectAmount; i++)
        {
            var dir = _random.NextAngle().ToVec();
            var chunk = Spawn("BloodChunkEffect", coords);
            // TODO: blood splatter color

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

    protected override void ApplyAshBladeEffect(EntityUid target)
    {
        base.ApplyAshBladeEffect(target);

        _flammable.AdjustFireStacks(target, 2.5f, null, true, 0.35f);
    }

    protected override void ApplyFleshBladeEffect(EntityUid target)
    {
        base.ApplyFleshBladeEffect(target);

        if (!TryComp(target, out BloodstreamComponent? bloodStream))
            return;

        _blood.TryModifyBleedAmount((target, bloodStream), 2f);

        if (!_sol.ResolveSolution(target,
                bloodStream.BloodSolutionName,
                ref bloodStream.BloodSolution,
                out var bloodSolution))
            return;

        _puddle.TrySpillAt(target, bloodSolution.SplitSolution(10), out _);
    }

    protected override void RandomTeleport(EntityUid user, EntityUid blade, RandomTeleportComponent comp)
    {
        base.RandomTeleport(user, blade, comp);

        _teleport.RandomTeleport(user, comp, false);
        QueueDel(blade);
    }
}
