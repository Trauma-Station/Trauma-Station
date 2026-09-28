// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Examine;
using Content.Trauma.Shared.Nuclear;
using Robust.Shared.Map;

namespace Content.Trauma.Client.Nuclear;

public sealed partial class ClientNuclearMachineSystem : NuclearMachineSystem
{
    [SubscribeLocalEvent]
    private void OnExamined(Entity<NuclearMachineComponent> ent, ref ClientExaminedEvent args)
    {
        Spawn(ent.Comp.ArrowPrototype, new EntityCoordinates(ent, 0, 0));
    }
}
