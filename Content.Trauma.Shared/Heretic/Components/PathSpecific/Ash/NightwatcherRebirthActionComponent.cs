// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Ash;

[RegisterComponent]
public sealed partial class NightwatcherRebirthActionComponent : Component
{
    [DataField]
    public TimeSpan CooldownReductionPerVictim = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan MinCooldown = TimeSpan.FromSeconds(10);

    [DataField]
    public int LastTargets;
}
