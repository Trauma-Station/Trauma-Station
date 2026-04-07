// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Trauma.Shared.Xenomorphs.Acid.Components;

[RegisterComponent]
public sealed partial class XenomorphAcidComponent : Component
{
    [DataField]
    public EntProtoId AcidId = "XenomorphAcid";

    [DataField]
    public TimeSpan AcidLifeTime = TimeSpan.FromSeconds(100);

    [DataField]
    public DamageSpecifier DamagePerSecond;
}
