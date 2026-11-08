namespace Content.Trauma.Shared.Megafauna.Arena;

/// <summary>
/// Component used on an entity in "arena" (megafauna) grids.
/// Teleports you back to the entity that teleported you to the arena.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class TeleportFromArenaComponent : Component
{
    /// <summary>
    /// The <see cref="TeleportToArenaComponent"/> entity we are linked to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Linked;
}
