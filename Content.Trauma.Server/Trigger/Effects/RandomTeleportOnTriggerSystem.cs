// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio.Systems;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Systems;
using Content.Trauma.Shared.Teleportation.Systems;
using Content.Goobstation.Common.Effects;


namespace Content.Trauma.Server.Trigger.Effects;

public sealed partial class RandomTeleportOnTriggerSystem : XOnTriggerSystem<RandomTeleportOnTriggerComponent>
{
    [Dependency] private RandomTeleportSystem _teleport = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private CommonSparksSystem _sparks = default!;

    protected override void OnTrigger(Entity<RandomTeleportOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        // No it cannot be an OnTriggerEvent because trigger happens only once and sound with sparks should be playd 2 times
        // Also i think BS crystal teleportation might be revorked to use events only
        _audio.PlayPredicted(ent.Comp.DepartureSound, ent, null);
        _sparks.DoSparks(ent);

        var newCoords = _teleport.RandomTeleport(ent, ent.Comp.TeleportationRadius);

        _audio.PlayPredicted(ent.Comp.ArrivalSound, ent, null);
        _sparks.DoSparks(ent);

        args.Handled = true;
    }
}
