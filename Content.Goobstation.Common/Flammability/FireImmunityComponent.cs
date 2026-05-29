// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Common.Flammability;

[RegisterComponent, NetworkedComponent]
public sealed partial class FireImmunityComponent : Component
{
    public override bool SessionSpecific => true;
}
