// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Megafauna.Arena;

/// <summary>
/// Component for use in grids where you want to TP in and then TP back.
/// It creates a dummy map with the grid, and links it to this entity.
///
/// You must map entities with <see cref="TeleportFromArenaComponent"/> component on the map,
/// so players can use those entities to TP back.
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

    /// <summary>
    /// The delay to teleport to the arena.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Effects to run on the user entering the arena
    /// </summary>
    [DataField]
    public EntityEffect[]? EffectsOnEnter;

    /// <summary>
    /// Effects to run on the user exiting the arena
    /// </summary>
    [DataField]
    public EntityEffect[]? EffectsOnExit;
}

[Serializable, NetSerializable]
public sealed partial class TeleportToArenaDoAfterEvent : SimpleDoAfterEvent;
