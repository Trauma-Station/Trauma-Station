// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Popups;
using Content.Shared._EinsteinEngines.Silicon.EmitBuzzWhileDamaged;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Mobs.Components;

namespace Content.Server._EinsteinEngines.Silicon.EmitBuzzOnCrit;

/// <summary>
/// This handles the buzzing popup and sound of a silicon based race when it is pretty damaged.
/// </summary>
public sealed class EmitBuzzWhileDamagedSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EmitBuzzWhileDamagedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_mob.IsDead(uid) ||
                !_threshold.TryGetThresholdForState(uid, MobState.Critical, out var threshold) ||
                _damageable.GetTotalDamage(uid) < threshold / 2)
                continue;

            comp.AccumulatedFrametime += frameTime;

            if (comp.AccumulatedFrametime < comp.CycleDelay)
                continue;

            comp.AccumulatedFrametime -= comp.CycleDelay;

            if (_timing.CurTime <= comp.LastBuzzPopupTime + comp.BuzzPopupCooldown)
                continue;

            // Start buzzing
            comp.LastBuzzPopupTime = _timing.CurTime;
            _popup.PopupEntity(Loc.GetString("silicon-behavior-buzz"), uid);
            Spawn("EffectSparks", Transform(uid).Coordinates);
            _audio.PlayPvs(comp.Sound, uid, AudioParams.Default.WithVariation(0.05f));
        }
    }

}
