// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Xenomorphs.Larva;

[RegisterComponent]
public sealed partial class XenomorphLarvaComponent : Component
{
    [DataField]
    public TimeSpan BurstDelay = TimeSpan.FromSeconds(5);

    [ViewVariables]
    public EntityUid? Victim;
}
