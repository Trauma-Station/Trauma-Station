// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Temperature;

/// <summary>
/// Prevents heating up past body temperature of 37C.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpecialHighTempImmunityComponent : Component
{
    public override bool SessionSpecific => true;
}
