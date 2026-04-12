// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid.Prototypes;

namespace Content.Trauma.Shared.Ghost;

/// <summary>
/// Like <c>GhostRoleMobSpawner</c> but uses <see cref="GhostCharacterSystem"/>, falling back to a random character of a set species.
/// Adds components to the mob to be just as powerful as usual spawners.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GhostCharacterSpawnerComponent : Component
{
    /// <summary>
    /// The default species to use.
    /// This does not override a player's desired character slot.
    /// </summary>
    [DataField]
    public ProtoId<SpeciesPrototype> DefaultSpecies = "Human";

    /// <summary>
    /// Components to add to the spawned mob.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Whether to delete this spawner when a ghost role is taken.
    /// </summary>
    [DataField]
    public bool DeleteOnSpawn = true;
}
