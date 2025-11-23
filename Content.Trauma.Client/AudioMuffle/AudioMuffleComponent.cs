using Content.Trauma.Shared.AudioMuffle;

namespace Content.Trauma.Client.AudioMuffle;

[RegisterComponent]
public sealed partial class AudioMuffleComponent : Component
{
    [ViewVariables]
    public float? OriginalVolume;

    [ViewVariables]
    public HashSet<Entity<SoundBlockerComponent>> RayBlockers = new();

    [ViewVariables]
    public Vector2i? Indices;
}
