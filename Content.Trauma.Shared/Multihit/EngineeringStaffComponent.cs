// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Multihit;

[RegisterComponent, NetworkedComponent]
public sealed partial class EngineeringStaffComponent : Component
{
    public override bool SessionSpecific => true;
}
