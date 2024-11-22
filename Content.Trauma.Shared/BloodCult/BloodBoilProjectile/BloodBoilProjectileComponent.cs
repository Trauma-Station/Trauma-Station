// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.BloodBoilProjectile;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class BloodBoilProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Target;
}
