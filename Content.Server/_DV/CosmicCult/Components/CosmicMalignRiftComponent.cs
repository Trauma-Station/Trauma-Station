using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._DV.CosmicCult.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CosmicMalignRiftComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan DangerTimer = default!;

    [DataField]
    public TimeSpan? DangerWait = null;

    [DataField]
    public float DangerRange = 6;

    [DataField]
    public int EntropyGranted = 10;

    [DataField]
    public TimeSpan MarkTime = TimeSpan.FromMinutes(2);

    [DataField]
    public EntProtoId PurgeVFX = "CleanseEffectVFX";

    [DataField]
    public SoundSpecifier PurgeSound = new SoundPathSpecifier("/Audio/_DV/CosmicCult/cleanse_deconversion.ogg");

    [DataField]
    public TimeSpan BibleTime = TimeSpan.FromSeconds(35);

    [DataField]
    public TimeSpan ChaplainTime = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan AbsorbTime = TimeSpan.FromSeconds(25);
}
