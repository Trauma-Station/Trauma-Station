// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Doors;
using Content.Shared.Prying.Components;
using Content.Trauma.Shared.Repulse;

namespace Content.Trauma.Shared.BloodCult.RunedDoor;

public sealed partial class RunedDoorSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;

    [SubscribeLocalEvent]
    private void OnBeforeDoorOpened(Entity<RunedDoorComponent> door, ref BeforeDoorOpenedEvent args)
    {
        if (args.User is not { } user)
            return;

        if (!_cult.IsCultist(user))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnBeforeDoorClosed(Entity<RunedDoorComponent> door, ref BeforeDoorClosedEvent args)
    {
        if (args.User is not { } user)
            return;

        if (!_cult.IsCultist(user))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnBeforePry(Entity<RunedDoorComponent> door, ref BeforePryEvent args)
    {
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnRepulseAttempt(Entity<RunedDoorComponent> door, ref RepulseAttemptEvent args)
    {
        if (_cult.IsCultist(args.Target))
            args.Cancelled = true;
    }
}
