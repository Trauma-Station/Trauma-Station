// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Wizard.Projectiles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class HomingProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField, AutoNetworkedField]
    public float? HomingSpeed = 720f;

    [DataField]
    public Angle Tolerance = Angle.FromDegrees(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan HomingTime = TimeSpan.FromMilliseconds(100);
}
