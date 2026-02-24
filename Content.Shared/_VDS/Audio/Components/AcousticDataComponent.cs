namespace Content.Shared._VDS.Audio.Components;

/// <summary>
/// Data that alters audio if the client has acoustics enabled.
/// </summary>
[RegisterComponent]
public sealed partial class AcousticDataComponent : Component
{
    /// <summary>
    /// How much audio should be absorbed when an acoustic ray reaches this entity.
    /// Ranges from 0% to 100% are ideal.
    /// Farther away things suffer falloff.
    /// </summary>
    [DataField]
    public float Absorption = 0f;

    /// <summary>
    /// If the acoustic ray will pass through this entity, or bounce off of it (like a wall) instead.
    /// </summary>
    [DataField]
    public bool ReflectRay = false;
}
