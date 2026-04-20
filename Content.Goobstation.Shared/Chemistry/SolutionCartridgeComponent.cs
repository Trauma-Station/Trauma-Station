// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Chemistry;

[RegisterComponent, NetworkedComponent]
public sealed partial class SolutionCartridgeComponent : Component
{
    [DataField]
    public string SolutionName = "cartridge";
}
