// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Random.Helpers;
using Content.Trauma.Shared.Heretic.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems;

public abstract class SharedHereticCombatMarkSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly EntityLookupSystem _look = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;

    private readonly HashSet<Entity<HumanoidProfileComponent>> _lookupHumanoid = new();

    public void ApplyMarkEffect(EntityUid target, HereticCombatMarkComponent mark, EntityUid user)
    {
        var protoId = $"HereticMark{mark.Path.ToString()}";
        if (_proto.HasIndex<EntityEffectPrototype>(protoId))
            _effects.TryApplyEffect(target, protoId, mark.Repetitions, user);

        _audio.PlayPredicted(mark.TriggerSound, target, user);
        RemCompDeferred(target, mark);

        var repetitions = mark.Repetitions - 1;
        if (repetitions <= 0)
            return;

        _lookupHumanoid.Clear();

        // transfers the mark to the next nearby person
        _look.GetEntitiesInRange(Transform(target).Coordinates, 5f, _lookupHumanoid, LookupFlags.Dynamic);
        var look = _lookupHumanoid.Where(x => x.Owner != target && !_heretic.IsHereticOrGhoul(x)).ToArray();
        if (look.Length == 0)
            return;

        var random = SharedRandomExtensions.PredictedRandom(Timing, GetNetEntity(target));

        var lookent = random.Pick(look);
        var markComp = EnsureComp<HereticCombatMarkComponent>(lookent);
        markComp.DisappearTime = markComp.MaxDisappearTime;
        markComp.Path = mark.Path;
        markComp.Repetitions = repetitions;
        Dirty(lookent, markComp);
    }
}

[ByRefEvent]
public readonly record struct UpdateCombatMarkAppearanceEvent;
