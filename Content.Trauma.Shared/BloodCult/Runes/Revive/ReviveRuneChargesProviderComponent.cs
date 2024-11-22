// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.Runes.Revive;

[RegisterComponent, NetworkedComponent]
public sealed partial class ReviveRuneChargesProviderComponent : Component
{
    [DataField]
    public int Charges = 3;
}
