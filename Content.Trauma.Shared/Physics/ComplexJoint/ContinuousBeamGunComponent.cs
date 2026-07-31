// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Physics.ComplexJoint;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ContinuousBeamGunComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Endpoint;

    [DataField, AutoNetworkedField]
    public MapCoordinates? CursorPosition;

    [DataField]
    public EntityUid? BeamSoundEnt;

    [DataField, AutoNetworkedField]
    public EntityUid? Shooter;

    [DataField(required: true)]
    public ComplexJointVisualsData Data = default!;

    [DataField]
    public bool UserCanFire = true;

    [DataField]
    public bool AltFire = true;

    [DataField]
    public float BeamScale = 1f;

    [DataField]
    public Vector2 MinMaxLaserRange = new(1f, 8f);

    [DataField]
    public float LaserSpeed = 1f;

    [DataField]
    public float LaserThickness = 0.05f;

    [DataField]
    public float? MaxRangeOverride;

    [DataField]
    public float BeamTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan BeamTimer;

    [DataField]
    public SoundSpecifier? BeamSound;

    [DataField]
    public TimeSpan DamageTime = TimeSpan.FromMilliseconds(200);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan DamageTimer;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan UpdateTimer;

    [DataField]
    public TimeSpan UpdateTime = TimeSpan.FromMilliseconds(20);

    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField]
    public EntityEffect[]? Effects;
}
