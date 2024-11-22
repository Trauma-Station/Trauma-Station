namespace Content.Trauma.Shared.Roles;

/// <summary>
/// Makes a mind role provide actions to its player.
/// The actions are contained by the mind entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RoleActionsComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> Actions = new();

    [DataField, AutoNetworkedField]
    public List<EntityUid> ActionEntities = new();
}
