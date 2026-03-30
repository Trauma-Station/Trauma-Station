// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction.Events;
using Content.Shared.Mech.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Mech;

public sealed class RotationCooldownSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RotationCooldownComponent, ChangeDirectionAttemptEvent>(OnChangeDirectionAttempt);
    }

    private void OnChangeDirectionAttempt(Entity<RotationCooldownComponent> ent, ref ChangeDirectionAttemptEvent args)
    {
        var now = _timing.CurTime;
        if (now < ent.Comp.NextRotate)
        {
            args.Cancel();
            return;
        }

        // other players wont hear it but the shitcode doesnt pass a user so idc
        if (_timing.IsFirstTimePredicted)
            _audio.PlayLocal(ent.Comp.Sound, ent, null);

        // technically this should use a separate event but literally nothing else uses this action blocker so...
        ent.Comp.NextRotate = now + ent.Comp.Delay;
        Dirty(ent);
    }
}
