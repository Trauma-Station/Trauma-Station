// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.Heretic.Systems;

public abstract class SharedHereticCombatMarkSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public virtual bool ApplyMarkEffect(EntityUid target,
        HereticCombatMarkComponent mark,
        HereticPath? path,
        EntityUid user,
        Entity<HereticComponent> heretic)
    {
        if (path == null)
            return false;

        _audio.PlayPredicted(mark.TriggerSound, target, user);
        RemCompDeferred(target, mark);
        return true;
    }
}

[ByRefEvent]
public readonly record struct UpdateCombatMarkAppearanceEvent;
