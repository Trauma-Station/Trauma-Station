namespace Content.Trauma.Shared.Megafauna.Arena;

/// <summary>
/// Component used on entities that teleport you to a grid,
/// and link the grid's entity with this entity, so you can teleport back.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class TeleportToArenaComponent : Component
{
    /// <summary>
    /// The path to the grid to load
    /// </summary>
    [DataField(required: true)]
    public ResPath ArenaPath;

    /// <summary>
    /// The grid entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Arena;
}
