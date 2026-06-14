namespace Content.Server.Spawners.Components;

public sealed partial class EntityTableSpawnerComponent
{
    /// <summary>
    /// If true, spawn attached to the entity's grid or map instead of the spawner entity.
    /// </summary>
    [DataField]
    public bool Absolute;
}
