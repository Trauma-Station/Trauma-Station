// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Trauma.Shared.Heretic.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems;

public abstract partial class SharedHereticCombatMarkSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private EntityLookupSystem _look = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;

    [Dependency] private EntityQuery<MobStateComponent> _mobQuery = new();

    private readonly HashSet<Entity<HumanoidProfileComponent>> _lookupHumanoid = new();

    public void ApplyMarkEffect(EntityUid target, HereticCombatMarkComponent mark, EntityUid user)
    {
        var protoId = $"HereticMark{mark.Path.ToString()}";
        if (ProtoMan.HasIndex<EntityEffectPrototype>(protoId))
            _effects.TryApplyEffect(target, protoId, 1f, user);

        _audio.PlayPredicted(mark.TriggerSound, target, user);
        RemCompDeferred(target, mark);

        var repetitions = mark.Repetitions - 1;
        if (repetitions <= 0)
            return;

        _lookupHumanoid.Clear();

        var coords = Transform(target).Coordinates;
        _look.GetEntitiesInRange(coords, 5f, _lookupHumanoid, LookupFlags.Dynamic);
        var look = _lookupHumanoid.Where(x => x.Owner != target && !_heretic.IsHereticOrGhoul(x))
            .OrderBy(x => (byte?) (_mobQuery.CompOrNull(x)?.CurrentState) ?? byte.MaxValue) // Prioritize living mobs
            .ThenBy(x => coords.TryDistance(EntityManager, Transform(x).Coordinates, out var dist) ? dist : float.MaxValue) // Prioritize mobs nearby
            .ToArray();
        if (look.Length == 0)
            return;

        var lookent = look[0];
        var markComp = EnsureComp<HereticCombatMarkComponent>(lookent);
        markComp.DisappearTime = markComp.MaxDisappearTime;
        markComp.Path = mark.Path;
        markComp.Repetitions = repetitions;
        Dirty(lookent, markComp);
    }
}

[ByRefEvent]
public readonly record struct UpdateCombatMarkAppearanceEvent;
