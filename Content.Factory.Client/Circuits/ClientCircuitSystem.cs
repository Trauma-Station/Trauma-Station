// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Factory.Shared.Circuits;

namespace Content.Factory.Client.Circuits;

public sealed partial class ClientCircuitSystem : CircuitSystem
{
    [SubscribeLocalEvent]
    private void OnInit(Entity<CircuitComponent> ent, ref ComponentInit args)
    {
        // so client has all 0s LinkedOutputs etc instead of an empty list
        ent.Comp.ValidatePortsCount();
    }
}
