// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.ClockworkCult;

[RegisterComponent]
public sealed partial class ClockworkCultAssociatedRuleComponent : Component
{
    [DataField]
    public EntityUid Rule;
}
