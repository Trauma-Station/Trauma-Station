// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Timing;
using Content.Trauma.Server.Wizard.Components;
using Content.Trauma.Shared.Effects;
using Content.Trauma.Shared.TelescopicBaton;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Server.Wizard.Systems;

public sealed partial class UseDelayBlockKnockdownSystem : EntitySystem
{
    [Dependency] private UseDelaySystem _delay = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SparksSystem _sparks = default!;

    [SubscribeLocalEvent]
    private void OnSuccess(Entity<UseDelayBlockKnockdownComponent> ent, ref KnockdownOnHitSuccessEvent args)
    {
        var (uid, comp) = ent;

        if (comp.ResetDelayOnSuccess)
            _delay.TryResetDelay(uid, id: comp.Delay);

        _audio.PlayPvs(comp.KnockdownSound, Transform(uid).Coordinates);

        if (!comp.DoSparks)
            return;

        foreach (var knocked in args.KnockedDown)
        {
            var coords = Transform(knocked).Coordinates;
            if (comp.DoCustom)
                Spawn(comp.CustomEffect, coords);
            else
                _sparks.DoSparks(coords, playSound: false, source: knocked);
        }
    }

    [SubscribeLocalEvent]
    private void OnAttempt(Entity<UseDelayBlockKnockdownComponent> ent, ref KnockdownOnHitAttemptEvent args)
    {
        var (uid, comp) = ent;

        if (args.Cancelled || !TryComp(uid, out UseDelayComponent? delay))
            return;

        if (_delay.IsDelayed((uid, delay), comp.Delay))
            args.Cancelled = true;
    }
}
