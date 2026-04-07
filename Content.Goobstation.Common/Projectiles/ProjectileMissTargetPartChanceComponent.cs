// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Common.Projectiles;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ProjectileMissTargetPartChanceComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> PerfectHitEntities = new();
}
