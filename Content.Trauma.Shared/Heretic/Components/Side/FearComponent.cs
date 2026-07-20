// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class FearComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, float> FearData = new();

    [DataField, AutoNetworkedField]
    public float TotalFear;

    [DataField, AutoNetworkedField]
    public float TargetFear;

    [DataField]
    public float Modifier = 1f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan NextUpdate;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan NextReduction;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextScream;

    [DataField]
    public TimeSpan ScreamDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(0.2);

    [DataField]
    public TimeSpan ReductionDelay = TimeSpan.FromSeconds(1.5);

    [DataField]
    public float HorrorThreshold = 30f;

    [DataField]
    public float MaxFear = 50f;

    [DataField]
    public float MinRadius = 0.4f;

    [DataField]
    public SoundSpecifier FearSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/fear.ogg");

    [DataField]
    public SoundSpecifier HorrorSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/doom.ogg");

    [DataField]
    public List<Vector2> FearVolumeCurve = new()
    {
        new(2f, -10f),
        new(10f, 0f),
        new(30f, 0f),
        new(40f, -10f),
    };

    [DataField]
    public List<Vector2> HorrorVolumeCurve = new()
    {
        new(25f, -8f),
        new(30f, 2f),
        new(60f, 2f),
    };

    [ViewVariables]
    public Entity<AudioComponent>? FearAudio;

    [ViewVariables]
    public Entity<AudioComponent>? HorrorAudio;
}
