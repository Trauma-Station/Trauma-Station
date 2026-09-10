// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Weapons.DelayedKnockdown;

[RegisterComponent, NetworkedComponent]
public sealed partial class DelayedKnockdownOnHitComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(4);

    [DataField]
    public bool Refresh = true;

    [DataField]
    public bool ApplyOnHeavyAttack;

    [DataField]
    public string UseDelay = "default";
}
