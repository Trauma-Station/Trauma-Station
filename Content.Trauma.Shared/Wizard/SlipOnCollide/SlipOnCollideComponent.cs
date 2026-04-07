// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Wizard.SlipOnCollide;

[RegisterComponent, NetworkedComponent]
public sealed partial class SlipOnCollideComponent : Component
{
    [DataField]
    public bool Force = true;
}
