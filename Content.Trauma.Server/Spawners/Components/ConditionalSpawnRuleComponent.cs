namespace Content.Trauma.Server.Spawners.Components;

/// <summary>
/// Component added to gamerules that load maps. Used to determine which entities to spawn on
/// <see cref="GameRuleSpawnerComponent"/> spawners based on Key
/// </summary>
[RegisterComponent]
public sealed partial class ConditionalSpawnRuleComponent : Component
{
    [DataField(required: true)]
    public string Key;
}
