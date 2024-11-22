// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult;

/// <summary>
/// Given to all blood cultists and constructs.
/// Added/removed with the antag role.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCultMemberComponent : Component
{
    public override bool SessionSpecific => true;
}
