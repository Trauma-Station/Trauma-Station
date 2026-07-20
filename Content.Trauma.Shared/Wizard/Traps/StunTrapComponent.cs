// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Wizard.Traps;

[RegisterComponent]
public sealed partial class StunTrapComponent : Component
{
    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(10);

    [DataField]
    public int Damage = 30;
}
