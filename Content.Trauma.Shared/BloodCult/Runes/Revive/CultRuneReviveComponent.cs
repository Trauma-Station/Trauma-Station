// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.Runes.Revive;

[RegisterComponent, NetworkedComponent]
public sealed partial class CultRuneReviveComponent : Component
{
    [DataField]
    public float ReviveRange = 0.5f;

    /// <summary>
    /// Number of sacrifices needed for 1 revival.
    /// </summary>
    [DataField]
    public int ChargesUsed = 3;
}
