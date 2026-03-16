using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._DV.CosmicCult.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedMonumentSystem))]
[AutoGenerateComponentPause]
public sealed partial class MonumentComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool CanActivate;
    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField, AutoNetworkedField]
    public int Stage = 1;

    [DataField]
    public TimeSpan InteractionTime = TimeSpan.FromSeconds(14);

    [DataField]
    public TimeSpan TransformTime = TimeSpan.FromSeconds(2.8);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? MusicTimer;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? BufferTimer;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? FinaleTimer;

    [DataField, AutoNetworkedField]
    public TimeSpan BufferTime = TimeSpan.FromSeconds(480); // 8 minutes should be plenty of time to figure out what's going on (and it can be sped up anyway)

    [DataField, AutoNetworkedField]
    public TimeSpan BufferSacrificeSpeedup = TimeSpan.FromSeconds(30);

    [DataField, AutoNetworkedField]
    public TimeSpan FinaleTime = TimeSpan.FromSeconds(126);

    [DataField]
    public SoundSpecifier BufferMusic = new SoundPathSpecifier("/Audio/_DV/CosmicCult/premonition.ogg")
    {
        Params = AudioParams.Default.WithVolume(-4f)
    };

    [DataField]
    public SoundSpecifier BufferMusicLoop = new SoundPathSpecifier("/Audio/_DV/CosmicCult/premonition_loop.ogg")
    {
        Params = AudioParams.Default.WithVolume(-4f).WithLoop(true)
    };

    [DataField]
    public SoundSpecifier FinaleMusic = new SoundPathSpecifier("/Audio/_DV/CosmicCult/a_new_dawn.ogg")
    {
        Params = AudioParams.Default.WithVolume(-4f)
    };

    [DataField]
    public EntProtoId CosmicGod = "MobCosmicGodSpawn";

    [DataField]
    public EntProtoId SacrificeVessel = "MobCosmicShatteredForm";

    [DataField]
    public EntProtoId SacrificeVfx = "CosmicBlankAbilityVFX";

    [DataField]
    public EntProtoId DespawnVfx = "MonumentCosmicCultDestruction";

    [DataField]
    public SoundSpecifier SacrificeSfx = new SoundPathSpecifier("/Audio/_DV/CosmicCult/ability_blank.ogg");
}

[Serializable, NetSerializable]
public enum MonumentVisuals : byte
{
    Monument,
    Transforming,
    FinaleReached,
}

[Serializable, NetSerializable]
public enum MonumentVisualLayers : byte
{
    MonumentLayer,
    TransformLayer,
    FinaleLayer,
}
