using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Shared.Vehicles;

[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class ForkliftComponent : Component
{
    [DataField]
    public EntityUid? LiftAction;

    [DataField]
    public EntityUid? UnliftAction;

    [DataField]
    public int ForkliftCapacity = 4;

    [DataField]
    public SoundSpecifier LiftSound;

    [DataField]
    public EntityUid? LiftSoundUid;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan? LiftSoundEndTime;
}
