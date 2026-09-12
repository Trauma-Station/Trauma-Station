using Content.Server.GameTicking.Rules.Components;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Server.Spawners.Components;

/// <summary>
/// Spawner that selects the items to spawn based on added gamerule
/// It checks if spawner map matches gamerule loaded map (<see cref="LoadMapRuleComponent"/>) on mapinit
/// </summary>
[RegisterComponent, EntityCategory("Spawner")]
public sealed partial class GameRuleSpawnerComponent : Component
{
    /// <summary>
    /// <see cref="ConditionalSpawnRuleComponent"/>'s Key -> Entity effect that gets applied to this entity
    /// Uses entity effects for more flexibility,
    /// allowing to spawn either specific entity or random humanoid as an example
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, ProtoId<EntityEffectPrototype>> ToSpawn = new();

    /// <summary>
    /// Uses this if not null and key was not present in dictionary
    /// </summary>
    [DataField]
    public ProtoId<EntityEffectPrototype>? Fallback;
}
