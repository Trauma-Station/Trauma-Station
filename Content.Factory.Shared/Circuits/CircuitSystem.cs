// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Factory.Shared.Circuits;

public abstract partial class CircuitSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnInit(Entity<CircuitComponent> ent, ref ComponentInit args)
    {
        // so client has all 0s LinkedOutputs etc instead of an empty list
        ent.Comp.ValidatePortsCount();
    }
}
