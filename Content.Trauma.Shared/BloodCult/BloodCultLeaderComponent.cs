// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult;

/// <summary>
/// Given to the blood cult leader for icons.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCultLeaderComponent : Component
{
    public override bool SessionSpecific => true;
}
