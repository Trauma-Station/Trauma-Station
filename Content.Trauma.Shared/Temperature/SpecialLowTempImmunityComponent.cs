// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Temperature;

/// <summary>
/// Prevetns cooling down below body temp of 37C.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpecialLowTempImmunityComponent : Component
{
    public override bool SessionSpecific => true;
}
