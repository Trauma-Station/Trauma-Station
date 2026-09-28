// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Heretic.Components;

[RegisterComponent]
public sealed partial class HereticRuleComponent : Component
{
    [DataField]
    public int RealityShiftPerHeretic = 1;

    [DataField]
    public bool HasAHereticAscended;

    [DataField]
    public EntProtoId ERTEvent = "SpawnERTSecurityDelayed";

    [DataField]
    public List<EntityUid> Minds = new();

    [DataField]
    public TimeSpan NextPassivePointUpdate;

    [DataField]
    public TimeSpan PassivePointCooldown = TimeSpan.FromMinutes(20);
}
