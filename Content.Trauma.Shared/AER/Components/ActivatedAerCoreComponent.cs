namespace Content.Trauma.Shared.AER;

/// <summary>
/// component identifying aer cores used by sci to spawn aers
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActivatedAerCoreComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool ActiveCore;

}