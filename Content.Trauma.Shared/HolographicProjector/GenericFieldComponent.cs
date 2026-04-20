using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.HolographicProjector;

[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class GenericFieldComponent : Component
{
    /// <summary>
    /// What made this entity?
    /// </summary>
    [ViewVariables]
    public Entity<GenericFieldGeneratorComponent>? SourceGen;

    /// <summary>
    /// was a temporary tile made with this entity?
    /// </summary>
    [ViewVariables]
    public bool TempTile = false;

    /// <summary>
    /// how much damage to heal per second
    /// </summary>
    [ViewVariables]
    public float RegenRate = -5f;

    /// <summary>
    /// Used to check if it's healed damage recently.
    /// </summary>
    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan RegenTimer;

    /// <summary>
    /// How many seconds should the field wait to regenerate?
    /// </summary>
    [DataField]
    public TimeSpan RegenTime = TimeSpan.FromSeconds(1);
}