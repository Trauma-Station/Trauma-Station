namespace Content.Trauma.Shared.Vampires.Dantalion;

/// <summary>
/// Component that marks an entity as thralled by a vampire
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class VampireThrallComponent : Component
{
    /// <summary>
    /// The vampire that owns us.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Vampire;
}
